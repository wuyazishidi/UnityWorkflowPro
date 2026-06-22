# -*- coding: utf-8 -*-
"""
Figma -> UGUI 同步核心（被 Config/figma-pull.ps1 调用，也可直接 python 运行）。

给一个 Figma node-id，确定性地完成「拉取 -> 导出资源 -> 生成 UISpec 草稿 -> 落地版式报告 + 合成图」。
这一步把所有易错/重复/有坑的外部活儿做完；剩下的 spec 微调与 build/render 交给 AI + ui-build-render.ps1。

坑都已内置处理：
  - token 需 file_content:read（非 current_user:read）；链接分享密码 ≠ API token。
  - 走系统代理用 curl 子进程（本机 Figma API 不走代理打不通）。
  - 整卡背景大图必须降采样到 <=MaxBg，否则落进 Icons/ 被强制 Uncompressed 卡死 Unity 主线程。
  - 光栅背景按卡片 cornerRadius 用 PIL 打圆角 alpha。
  - UGUI 无描边：有 fill+stroke 的输入框 -> 「外层描边色 + 内层填充内缩2px」叠边环；半透 fill 自动提不透明度避免边环糊进内部。

用法: python scripts/figma_sync.py <node-id> [Panel] [--file KEY] [--token TOK] [--maxbg 1280] [--iconscale 3]
"""
import sys, os, io, json, base64, subprocess, argparse, re

FILE_KEY_DEFAULT = "wGp5DXqAjtpwuPS4qMWkxP"
API = "https://api.figma.com/v1"


def curl(url, token=None, out=None, timeout=90):
    """用 curl 子进程发请求（继承系统代理）。out 给路径则下载到文件，否则返回 bytes。"""
    cmd = ["curl", "-s", "-m", str(timeout)]
    if token:
        cmd += ["-H", f"X-Figma-Token: {token}"]
    if out:
        cmd += ["-o", out, "-w", "%{http_code}"]
        r = subprocess.run(cmd + [url], capture_output=True, text=True)
        return r.stdout.strip()
    r = subprocess.run(cmd + [url], capture_output=True)
    return r.stdout


def get_json(url, token, tries=4):
    """带重试：Figma API 偶发空响应/限流，重试几次再解析（避免一次空响应直接崩）。"""
    last = ""
    for i in range(tries):
        raw = curl(url, token)
        try:
            last = raw.decode("utf-8", "replace")
        except Exception:
            last = str(raw)
        s = last.strip()
        if s:
            try:
                return json.loads(s)
            except Exception:
                pass  # 非 JSON（限流页/空）→ 重试
        import time
        time.sleep(1.5 * (i + 1))
    raise SystemExit(f"ERROR: Figma API 多次返回非 JSON（限流或网络）。url={url}\n最后响应前 200 字: {last[:200]!r}")


def hex_of(color, opacity=1.0):
    if not color:
        return None
    a = color.get("a", 1.0) * opacity
    s = "#%02X%02X%02X" % (round(color["r"] * 255), round(color["g"] * 255), round(color["b"] * 255))
    return s if a >= 0.999 else s + "%02X" % round(a * 255)


def first_solid_fill(n):
    for f in n.get("fills", []):
        if f.get("visible", True) and f["type"] == "SOLID":
            return hex_of(f["color"], f.get("opacity", 1.0))
    return None


def has_image_fill(n):
    return any(f.get("visible", True) and f["type"] == "IMAGE" for f in n.get("fills", []))


def first_stroke(n):
    for s in n.get("strokes", []):
        if s.get("type") == "SOLID":
            return hex_of(s["color"], s.get("opacity", 1.0)), n.get("strokeWeight", 1.0)
    return None, 0


def first_gradient(n):
    """提取线性渐变填充 → {type, angle, stops}（spec 004 Phase 2）。无则 None。"""
    import math
    for f in n.get("fills", []):
        if not f.get("visible", True):
            continue
        if f["type"] != "GRADIENT_LINEAR":
            continue
        stops = [{"color": hex_of(s["color"], s["color"].get("a", 1.0)), "pos": round(s.get("position", 0.0), 4)}
                 for s in f.get("gradientStops", [])]
        if len(stops) < 2:
            continue
        # 由两个手柄点(归一化、y 向下)算角度；UIVertexGradient: angle=atan2(dy,dx)
        hp = f.get("gradientHandlePositions") or []
        ang = 0.0
        if len(hp) >= 2:
            dx = hp[1]["x"] - hp[0]["x"]; dy = hp[1]["y"] - hp[0]["y"]
            ang = round(math.degrees(math.atan2(dy, dx)), 1)
        return {"type": "Linear", "angle": ang, "stops": stops}
    return None


def node_opacity(n):
    o = n.get("opacity")
    return o if (o is not None and o < 0.999) else None


def _grad_max_alpha(g):
    """渐变 stops 的最大不透明度(0-1)。判断渐变是否"可见"——全透明渐变是装饰性叠加(微光等)，不能拿来盖底色。"""
    mx = 0.0
    for s in (g or {}).get("stops", []):
        c = s.get("color", "") or ""
        a = (int(c[7:9], 16) / 255.0) if len(c) == 9 else 1.0
        if a > mx:
            mx = a
    return mx


def _line_spacing(st):
    """由 Figma 行高换算 TMP lineSpacing(≈百分比差，负=收紧)。AUTO 行高或差异很小时返回 None(用 TMP 默认)。"""
    unit = st.get("lineHeightUnit")
    if unit not in ("PIXELS", "FONT_SIZE_%", "PERCENT"):
        return None  # INTRINSIC_%(自动行高) → 不干预
    pct = st.get("lineHeightPercentFontSize")
    if pct is None:
        lh, fs = st.get("lineHeightPx"), st.get("fontSize")
        if lh and fs:
            pct = lh / fs * 100.0
    if pct is None:
        return None
    # TMP 的行高 = 字体默认行高 + lineSpacing(1/100 em)。MiSans SDF 默认行高 =
    # m_LineHeight/m_PointSize = 119.34/90 ≈ 132.6%(非 100%)。要还原 Figma 总行高 pct%，
    # 须以字体默认行高为基准相减，否则每行多算 ~0.33em，多行文本(如 content_Text)累积溢出。
    sp = round(pct - 132.6, 1)
    return sp if abs(sp) >= 8 else None  # 只在与默认行高明显偏差时干预


def _char_spacing(st):
    """Figma letterSpacing(px) → TMP characterSpacing(≈1/100 em，与字号无关、随渲染字号缩放，兼容字号归一化)。
    0/缺失/极小返回 None（不写该字段，保持 spec 精简）。"""
    ls, fs = st.get("letterSpacing"), st.get("fontSize")
    if not ls or not fs:
        return None
    cs = round(ls / fs * 100.0, 1)
    return cs if abs(cs) >= 0.5 else None


def all_vector_leaves(n):
    """节点的所有叶子是否都是 VECTOR（线稿/图标插画）。空 children 返回 False。"""
    kids = n.get("children", [])
    if not kids:
        return False
    ok = False
    for c in kids:
        t = c["type"]
        if t == "VECTOR":
            ok = True
        elif t in ("FRAME", "GROUP", "INSTANCE", "COMPONENT"):
            if not all_vector_leaves(c):
                return False
        elif t in ("BOOLEAN_OPERATION",):
            ok = True
        else:
            return False
    return ok


import hashlib

_COMMON = "Assets/UI/Common"
_CORNER_META_TMPL = None

# 字重→真实字体：按 Figma fontWeight 选对应字重的 MiSans SDF，而非 faux bold 合成一档。
# Figma 全工程用 400/500/600/700 → Regular/Medium/Semibold/Bold 四档；阈值取相邻字重中点。
# builder ResolveFont 找不到对应 SDF 时回退 TMP 默认字体(不报错)。
_WEIGHT_FONTS = [
    (450, "Assets/Fonts/MiSans Regular SDF.asset"),    # <450  → Regular(400 正文)
    (550, "Assets/Fonts/MiSans Medium SDF.asset"),     # 450-550 → Medium(500)
    (650, "Assets/Fonts/MiSans Semibold SDF.asset"),   # 550-650 → Semibold(600)
]
_WEIGHT_FONT_BOLD = "Assets/Fonts/MiSans-Bold SDF.asset"  # >=650 → Bold(700)


_FONT_UNIFORM = "Assets/Fonts/MiSans Medium SDF.asset"  # 统一字体：全部文本用 Medium SDF


def weight_font(w):
    """Figma fontWeight → SDF 资源路径。
    当前策略：统一用 MiSans Medium SDF（不按字重分档）。
    要恢复按字重四档(Regular/Medium/Semibold/Bold)，删掉下面一行的统一返回即可。"""
    return _FONT_UNIFORM
    w = w or 400
    for thr, path in _WEIGHT_FONTS:
        if w < thr:
            return path
    return _WEIGHT_FONT_BOLD



def _corner_guid(name):
    """确定性 GUID(32 hex)：同名圆角 sprite 跨同步/跨工程恒定，prefab 引用不断裂。"""
    return hashlib.md5(("uiwf-corner-" + name).encode()).hexdigest()


def _write_corner_meta(name, border):
    """按 round12.png.meta 模板写 .meta，仅替换 guid 与 spriteBorder(9-slice 边=圆角半径)。"""
    global _CORNER_META_TMPL
    if _CORNER_META_TMPL is None:
        with io.open(_COMMON + "/round12.png.meta", encoding="utf-8") as f:
            _CORNER_META_TMPL = f.read()
    m = re.sub(r"guid: [0-9a-f]+", "guid: " + _corner_guid(name), _CORNER_META_TMPL, count=1)
    m = re.sub(r"spriteBorder: \{[^}]+\}",
               "spriteBorder: {x: %d, y: %d, z: %d, w: %d}" % (border, border, border, border), m)
    with io.open("%s/%s.png.meta" % (_COMMON, name), "w", encoding="utf-8") as f:
        f.write(m)


def ensure_corner_sprite(r, kind="round"):
    """按 Figma 实际圆角 r 程序化生成精确的 9-slice 圆角 sprite(若缺)，返回(路径, border=r)。
    取代旧的"r<=12 一律 round12"档位——那会把 r=3.5 的标签圆角放大到 12(差 3.4 倍)，是通用偏差。
    9-slice 角=r 像素、spritePixelsToUnits=100 → 渲染圆角=r 设计单位，精确还原 cornerRadius。"""
    from PIL import Image, ImageDraw, ImageChops
    # 钳到 [2,48]：Figma 的 pill(完全圆角)用超大 cornerRadius(几百~几千)，原样生成会 size 爆内存；
    # 48 已是足够大的圆角档，UGUI 9-slice 在小元素上会自动按比例缩 border → 渲染成 pill 效果。
    r = max(2, min(48, int(round(r))))
    name = "%s%d" % (kind, r)
    path = "%s/%s.png" % (_COMMON, name)
    if not os.path.exists(path):
        pad, ss = 4, 4                        # 中间可拉伸区半边 / 超采样抗锯齿
        size = 2 * r + 2 * pad
        S, R = size * ss, r * ss
        white = Image.new("RGBA", (S, S), (255, 255, 255, 255))
        out = Image.new("RGBA", (S, S), (255, 255, 255, 0))
        outer = Image.new("L", (S, S), 0)
        ImageDraw.Draw(outer).rounded_rectangle([0, 0, S - 1, S - 1], radius=R, fill=255)
        if kind == "ring":
            sw = 2 * ss                       # 描边 2px(匹配现有 ring12)
            inner = Image.new("L", (S, S), 0)
            ImageDraw.Draw(inner).rounded_rectangle([sw, sw, S - 1 - sw, S - 1 - sw], radius=max(0, R - sw), fill=255)
            mask = ImageChops.subtract(outer, inner)
        else:
            mask = outer
        out = Image.composite(white, out, mask)
        out.resize((size, size), Image.LANCZOS).save(path)
        _write_corner_meta(name, r)
        print("[corner] gen %s.png (%dx%d border=%d)" % (name, size, size, r))
    return path, r


def round_sprite(r):
    return ensure_corner_sprite(r if r else 12, "round")


def ring_sprite(r):
    """镂空描边环精灵(中间透明)，用于半透面板描边，避免实心底色把半透填充染色。"""
    return ensure_corner_sprite(r if r else 12, "ring")


def eff_corner_radius(n, default=12):
    """有效圆角(设计 px)：永不超过节点短边的一半。
    Figma 的「完全圆角」pill 用哨兵超大 cornerRadius(几百万)，原先一律钳到 48 会把
    小 pill(如 62x20 的状态徽标)严重过圆——胶囊变大圆角。这里按 min(w,h)/2 = pill 真实
    半径封顶，对 cornerRadius 本就合理的节点(r < 短边/2)无影响。"""
    cr = n.get("cornerRadius")
    cr = int(cr) if cr else default
    bb = n.get("absoluteBoundingBox") or {}
    w, h = bb.get("width"), bb.get("height")
    if w and h:
        cr = min(cr, int(min(w, h) // 2))
    return cr


def text_align(n):
    st = n.get("style", {})
    h = (st.get("textAlignHorizontal") or "LEFT").upper()
    return {"LEFT": "MidlineLeft", "RIGHT": "MidlineRight", "CENTER": "Center", "JUSTIFIED": "MidlineLeft"}.get(h, "MidlineLeft")


def save_source_snapshot(panel, file_key, node, panel_dir, data):
    """把原始 Figma 节点树 + 来源元数据写进仓库内 figma/（committed），用于离线恢复。
    - figma/<Panel>.nodes.json : 该 node 的完整子树（设计备份，Figma 清掉也还在）
    - figma/<Panel>.meta.json  : 来源 = {fileKey, node, folder, lastModified, 用到的接口}
    figma/RECOVERY.md 为人读总索引（手工维护），机器索引 = figma/*.meta.json 之并集。"""
    snap_dir = "figma"
    os.makedirs(snap_dir, exist_ok=True)
    meta = {
        "panel": panel,
        "fileKey": file_key,
        "node": node,
        "folder": panel_dir,
        "spec": f"{panel_dir}/{panel}.json",
        "lastModified": data.get("lastModified"),
        "api": {
            "nodes": f"{API}/files/{file_key}/nodes?ids={node}",
            "images": f"{API}/images/{file_key}?ids=<NODE_ID>&format=png&scale=2",
            "auth": "header X-Figma-Token: <token>（scope=file_content:read）",
        },
        "resync": f"figma-sync.ps1 -Node {node} -Panel {panel} -FileKey {file_key} -Verify",
    }
    with io.open(f"{snap_dir}/{panel}.meta.json", "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=2)
    with io.open(f"{snap_dir}/{panel}.nodes.json", "w", encoding="utf-8") as f:
        json.dump(data["nodes"][node], f, ensure_ascii=False, indent=2)
    print(f"snapshot -> {snap_dir}/{panel}.nodes.json + {panel}.meta.json (committed, 离线可恢复)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("node")
    ap.add_argument("panel", nargs="?", default="Login")
    ap.add_argument("--file", default=FILE_KEY_DEFAULT)
    ap.add_argument("--token", default=os.environ.get("FIGMA_TOKEN", ""))
    ap.add_argument("--maxbg", type=int, default=1280)
    ap.add_argument("--iconscale", type=int, default=3)
    ap.add_argument("--outroot", default="Assets/UI")
    a = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    if not a.token:
        print("ERROR: no token (pass --token or set FIGMA_TOKEN / .figma-token)"); sys.exit(2)
    node = a.node.replace("-", ":")
    panel_dir = f"{a.outroot}/{a.panel}"
    icons_dir = f"{panel_dir}/Icons"
    meta_dir = f"{panel_dir}/.figma"
    os.makedirs(icons_dir, exist_ok=True)
    os.makedirs(meta_dir, exist_ok=True)

    # 1) 拉节点树
    data = get_json(f"{API}/files/{a.file}/nodes?ids={node}", a.token)
    if data.get("nodes", {}).get(node) is None:
        print(f"ERROR: node {node} not found. lastModified={data.get('lastModified')}"); sys.exit(2)
    doc = data["nodes"][node]["document"]

    # 1.5) 快照（仓库内、committed）：把本次拉取的原始 Figma 节点树 + 来源元数据存进 figma/，
    #      这样即便以后 Figma 里清掉/改了这个 node，也无需重新扫描——离线即可恢复设计与来源。
    #      （注意：Assets/UI/*/.figma/ 被 gitignore，是易变中间产物；figma/ 顶层目录入库。）
    save_source_snapshot(a.panel, a.file, node, panel_dir, data)
    root_bb = doc["absoluteBoundingBox"]
    OX, OY = root_bb["x"], root_bb["y"]
    FW, FH = round(root_bb["width"]), round(root_bb["height"])

    def rect(n):
        bb = n["absoluteBoundingBox"]
        return {"x": round(bb["x"] - OX), "y": round(bb["y"] - OY), "w": round(bb["width"]), "h": round(bb["height"])}

    # 2) 找卡片（有圆角+实色、尺寸接近整帧）与背景大图
    card = {"node": None, "r": 16}
    def find_card(n):
        if n["type"] == "FRAME" and n.get("cornerRadius") and first_solid_fill(n):
            bb = n["absoluteBoundingBox"]
            if bb["width"] >= FW * 0.8 and bb["height"] >= FH * 0.8 and card["node"] is None:
                card["node"] = n; card["r"] = int(n["cornerRadius"])
        for c in n.get("children", []):
            find_card(c)
    find_card(doc)
    card_id = card["node"]["id"] if card["node"] else None
    card_bb = card["node"]["absoluteBoundingBox"] if card["node"] else root_bb

    # 参考系收敛到“卡片”而非外层画板：面板根 = 卡片(1106x778)，去掉画板四周的空白；
    # 这样渲染与卡片真值同框，-Verify 的 MAE 才有意义。card 在画板原点时坐标不变。
    if card["node"]:
        OX, OY = card_bb["x"], card_bb["y"]
        FW, FH = round(card_bb["width"]), round(card_bb["height"])

    # 3) 收集需导出的节点：背景大图 / 其它 IMAGE / 纯描边小框(角标) / 全线稿(图标·装饰)
    exports = {}  # id -> role ('bg'|'image'|'corner'|'art')
    def collect(n):
        bb = n.get("absoluteBoundingBox")
        if bb:
            is_bg = has_image_fill(n) and bb["width"] >= card_bb["width"] * 0.9 and bb["height"] >= card_bb["height"] * 0.9
            if is_bg:
                exports[n["id"]] = "bg"
            elif has_image_fill(n):
                exports[n["id"]] = "image"
            elif all_vector_leaves(n):
                exports[n["id"]] = "art"
            elif not n.get("children") and first_stroke(n)[0] and not first_solid_fill(n):
                exports[n["id"]] = "corner"
        # 全线稿/图标节点不再深入其 children
        if exports.get(n["id"]) in ("art", "corner", "bg", "image"):
            return
        for c in n.get("children", []):
            collect(c)
    collect(doc)

    # 资源文件名：按角色稳定命名（bg.png / corner1.png / icon1.png / art1.png），避免 node-id 丑名与跨次重复
    role_short = {"bg": "bg", "image": "icon", "art": "art", "corner": "corner"}
    _names, _cnt = {}, {}
    for nid in exports:  # dict 保持遍历顺序 → 命名稳定
        role = exports[nid]
        if role == "bg":
            _names[nid] = "bg.png"
        else:
            _cnt[role] = _cnt.get(role, 0) + 1
            _names[nid] = f"{role_short.get(role, role)}{_cnt[role]}.png"

    _canon = {}  # nid -> 去重后规范文件名（同像素内容的多份只留一张，其余重定向到它）

    def asset_path(nid):
        if nid in _canon:
            return _canon[nid]
        return _names.get(nid, "n" + nid.replace(":", "-") + ".png")

    # 4) 调 images API 拿渲染 URL（背景按帧宽，图标按 iconscale）
    bg_ids = [i for i, r in exports.items() if r == "bg"]
    icon_ids = [i for i, r in exports.items() if r != "bg"]
    urls = {}
    if bg_ids:
        j = get_json(f"{API}/images/{a.file}?ids={','.join(bg_ids)}&format=png&scale=1", a.token)
        urls.update(j.get("images", {}))
    if icon_ids:
        j = get_json(f"{API}/images/{a.file}?ids={','.join(icon_ids)}&format=png&scale={a.iconscale}", a.token)
        urls.update(j.get("images", {}))

    from PIL import Image, ImageDraw
    exported = []
    for nid, url in urls.items():
        if not url:
            continue
        out = f"{icons_dir}/{asset_path(nid)}"
        code = curl(url, out=out)
        if exports.get(nid) == "bg":
            im = Image.open(out).convert("RGBA")
            w, h = im.size
            if w > a.maxbg:
                nh = round(h * a.maxbg / w); im = im.resize((a.maxbg, nh), Image.LANCZOS); w, h = im.size
            rad = round(card["r"] * w / card_bb["width"])
            m = Image.new("L", (w, h), 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, w - 1, h - 1], radius=rad, fill=255)
            im.putalpha(m); im.save(out)
            exported.append(f"{asset_path(nid)} (bg {w}x{h} r{rad})")
        else:
            exported.append(f"{asset_path(nid)} ({exports.get(nid)})")

    # 4.5) 图标去重：Figma 把重复实例(同款 pill 边框/圆点等)各导一张 → 同像素内容只留一张，
    #      其余 nid 的引用重定向到保留图(_canon)，并删掉冗余 PNG(+旧 .meta)。降包体与图集占用。
    import hashlib
    _hash_keep, _dups = {}, 0
    for nid in list(urls.keys()):
        if not urls.get(nid) or exports.get(nid) == "bg":  # 背景唯一，不参与去重
            continue
        fn = asset_path(nid)
        p = f"{icons_dir}/{fn}"
        if not os.path.exists(p):
            continue
        with open(p, "rb") as fh:
            h = hashlib.md5(fh.read()).hexdigest()
        if h in _hash_keep:
            _canon[nid] = _hash_keep[h]            # 重定向到已保留的同内容图
            for rm in (p, p + ".meta"):
                try: os.remove(rm)
                except OSError: pass
            _dups += 1
        else:
            _hash_keep[h] = fn
    if _dups:
        print(f"dedupe -> 合并 {_dups} 张重复图标(同像素内容)，Icons/ 唯一图 {len(_hash_keep)} 张")

    # 5) 合成图（真值参照）：导卡片节点（与渲染同框，便于 -Verify 做可靠 MAE），无卡片则退回整帧
    truth_id = card_id or node
    j = get_json(f"{API}/images/{a.file}?ids={truth_id}&format=png&scale=2", a.token)
    tu = j.get("images", {}).get(truth_id)
    if tu:
        curl(tu, out=f"{meta_dir}/truth.png")

    # 6) 生成 UISpec 草稿（扁平化：全部直接挂 root，rect 用整帧绝对像素）
    out_nodes = []

    def is_button(n):
        # 接受 FRAME/COMPONENT/INSTANCE（按钮组件实例）。填充不强制实色（渐变/透明由 _apply_v2 处理，透明按钮仍可点）。
        if n.get("type") not in ("FRAME", "COMPONENT", "INSTANCE"):
            return False
        nm = n.get("name", "") or ""
        # 确定性命名约定：名字以 _Btn 结尾 → 一律 Button（无论填充/文字对齐，列表项整行可点也算）。
        if nm.lower().endswith("_btn"):
            return True
        # 兜底（未加后缀的旧设计）：含 button/btn/按钮 + 内有居中文字。
        low = nm.lower()
        if "button" in low or "btn" in low or "按钮" in nm:
            return _find_centered_text(n) is not None
        return False

    def _find_centered_text(n):
        for c in n.get("children", []):
            if c["type"] == "TEXT" and (c.get("style", {}).get("textAlignHorizontal", "").upper() == "CENTER"):
                return c
            r = _find_centered_text(c)
            if r:
                return r
        return None

    def text_node(n, name=None):
        st = n.get("style", {})
        col = first_solid_fill(n) or "#FFFFFF"
        nd = {"name": name or n.get("name", "Text"), "type": "Text", "raycastTarget": False,
              "rect": rect(n),
              "text": {"content": n.get("characters", ""), "fontSize": round(st.get("fontSize", 16)),
                       "color": col, "alignment": text_align(n)}}
        nd["text"]["fontAsset"] = weight_font(st.get("fontWeight", 400))
        ls = _line_spacing(st)
        if ls is not None:
            nd["text"]["lineSpacing"] = ls
        cs = _char_spacing(st)
        if cs is not None:
            nd["text"]["characterSpacing"] = cs
        # 换行：Figma 固定宽度文本框(HEIGHT/NONE)按盒宽折行；自动宽度(WIDTH_AND_HEIGHT)是单行标签不换行
        if st.get("textAutoResize") in ("HEIGHT", "NONE"):
            nd["text"]["wrap"] = True
        return nd

    def _apply_v2(nd, n):
        """把 Figma 的渐变/整体不透明度写进节点的 v2 字段（spec 004 Phase 2）。"""
        g = first_gradient(n)
        # 仅当渐变"可见"(至少一个 stop 不透明度可观)才用它驱动顶点色；全透明渐变是装饰叠加，
        # 不能盖掉底层实色——否则节点整块变透明(如 CountdownPanel 半透卡片被微光渐变冲没)。
        if g and _grad_max_alpha(g) >= 0.1:
            nd["gradient"] = g
            nd["color"] = "#FFFFFF"   # 渐变由顶点色驱动，底色置白避免相乘偏色
        o = node_opacity(n)
        if o is not None:
            nd["opacity"] = round(o, 3)
        return nd

    def _stroke_field(n, cr):
        """stroke 收敛为节点 v2 字段；用镂空环精灵(ring*)实现描边（built-in UGUI）。"""
        scol, sw = first_stroke(n)
        if not scol:
            return None
        rp, rb = ring_sprite(cr)
        return {"color": scol, "weight": round(sw, 2), "align": "Inside",
                "sprite": rp, "border": {"l": rb, "t": rb, "r": rb, "b": rb}}

    # ===== 语义组件映射（spec 004 Phase 2.5）：Figma 命名 → 功能组件 =====
    def _find_text(n):
        if n.get("type") == "TEXT":
            return n
        for c in n.get("children", []):
            r = _find_text(c)
            if r:
                return r
        return None

    def _find_export_descendant(n):
        for c in n.get("children", []):
            if exports.get(c["id"]):
                return c
            r = _find_export_descendant(c)
            if r:
                return r
        return None

    def _is_input(n):
        """名字以 _InputField 结尾（命名约定，确定性），或 fill+stroke 框且含命名带 'input'/'输入' 的后代 → 输入框。"""
        if n.get("type") not in ("FRAME", "INSTANCE", "COMPONENT"):
            return False
        if (n.get("name", "") or "").lower().endswith("_inputfield"):
            return True
        if not (first_solid_fill(n) and first_stroke(n)[0]):
            return False
        def named_input(x):
            if x is not n and "input" in (x.get("name", "") or "").lower():
                return True
            if x.get("type") == "TEXT" and "输入" in (x.get("characters", "") or ""):
                return True
            return any(named_input(c) for c in x.get("children", []))
        return named_input(n)

    def _is_password(n):
        def chk(x):
            nm = x.get("name", "") or ""
            if "password" in nm.lower() or "密码" in nm:
                return True
            if x.get("type") == "TEXT" and "密码" in (x.get("characters", "") or ""):
                return True
            return any(chk(c) for c in x.get("children", []))
        return chk(n)

    def _opaque(hexc):
        return hexc[:7] if (hexc and len(hexc) == 9) else hexc

    def emit_input_field(n):
        cr = eff_corner_radius(n, 12)
        sp, b = round_sprite(cr)
        r = rect(n)
        nm = _san(n.get("name", "Input"))
        nd = {"name": nm + "Input", "type": "InputField", "color": first_solid_fill(n) or "#0A1E46",
              "sprite": sp, "imageType": "Sliced", "border": {"l": b, "t": b, "r": b, "b": b}, "rect": r,
              "contentType": "Password" if _is_password(n) else "Standard"}
        stroke = _stroke_field(n, cr)
        if stroke:
            nd["stroke"] = stroke
        ph = _find_text(n)
        if ph is not None:
            pr = rect(ph); stl = ph.get("style", {})
            nd["placeholder"] = {"content": ph.get("characters", ""), "fontSize": round(stl.get("fontSize", 16)),
                                 "color": first_solid_fill(ph) or "#8EC5FF40", "alignment": text_align(ph)}
            nd["padding"] = {"l": max(0, pr["x"] - r["x"]), "t": 6,
                             "r": max(0, (r["x"] + r["w"]) - (pr["x"] + pr["w"])), "b": 6}
            nd["textColor"] = _opaque(first_solid_fill(ph)) or "#E8F4FF"
        if nd["contentType"] == "Password":
            eye = _find_export_descendant(n)
            if eye is not None:
                nd["passwordToggle"] = {"sprite": f"{panel_dir}/Icons/{asset_path(eye['id'])}",
                                        "color": "#FFFFFF", "rect": rect(eye)}
        return _apply_v2(nd, n)

    def emit_image(n, role=None):
        nd = {"name": _san(n.get("name", "Image")), "type": "Image", "color": "#FFFFFF", "raycastTarget": False,
              "sprite": f"{panel_dir}/Icons/{asset_path(n['id'])}", "rect": rect(n)}
        o = node_opacity(n)
        if o is not None:
            nd["opacity"] = round(o, 3)
        return nd

    def emit_solid(n, name=None):
        cr = eff_corner_radius(n, 0)
        # 无实色填充的节点（如只有描边的分隔/容器框）用透明底，避免误填不透明白把面板冲白
        nd = {"name": name or _san(n.get("name", "Rect")), "type": "Image",
              "color": first_solid_fill(n) or "#FFFFFF00", "raycastTarget": False, "rect": rect(n)}
        if cr:
            sp, b = round_sprite(cr)
            nd.update({"sprite": sp, "imageType": "Sliced", "border": {"l": b, "t": b, "r": b, "b": b}})
        st = _stroke_field(n, cr or 12)
        if st:
            nd["stroke"] = st
        return _apply_v2(nd, n)

    def emit_bordered(n, as_button=False):
        """fill+stroke -> 单节点：round 精灵填充 + v2 stroke(环精灵)，built-in UGUI。"""
        cr = eff_corner_radius(n, 12)
        sp, b = round_sprite(cr)
        r = rect(n)
        nm = _san(n.get("name", "Field"))
        fill = first_solid_fill(n)
        stroke = _stroke_field(n, cr)
        if as_button:
            txt = _find_centered_text(n)
            nd = {"name": nm, "type": "Button", "color": fill or "#FFFFFF00", "rect": r}
            if fill:  # 有实色填充才用 round sprite；透明按钮不给(避免冷渲染/sprite 未就绪露白块)
                nd.update({"sprite": sp, "imageType": "Sliced", "border": {"l": b, "t": b, "r": b, "b": b}})
            if txt:
                stl = txt.get("style", {})
                nd["text"] = {"content": txt.get("characters", ""), "fontSize": round(stl.get("fontSize", 16)),
                              "color": first_solid_fill(txt) or "#FFFFFF", "alignment": "Center"}
                nd["text"]["fontAsset"] = weight_font(stl.get("fontWeight", 400))
                _cs = _char_spacing(stl)
                if _cs is not None:
                    nd["text"]["characterSpacing"] = _cs
        else:
            nd = {"name": nm + "Fill", "type": "Image", "color": fill or "#FFFFFF", "raycastTarget": False,
                  "sprite": sp, "imageType": "Sliced", "border": {"l": b, "t": b, "r": b, "b": b}, "rect": r}
        if stroke:
            nd["stroke"] = stroke
        return _apply_v2(nd, n)

    # ===== 标准 UGUI 组件映射（spec 004 Phase 2.6）：按命名识别 → 功能组件 =====
    def _name_has(n, *keys):
        low = (n.get("name", "") or "").lower()
        return any(k.lower() in low for k in keys)

    def _is_scrollbar(n):
        return n.get("type") in ("FRAME", "INSTANCE", "COMPONENT", "GROUP", "RECTANGLE") \
            and _name_has(n, "scrollbar", "滚动条")

    def _is_slider(n):
        return n.get("type") in ("FRAME", "INSTANCE", "COMPONENT") \
            and _name_has(n, "slider", "滑块", "滑动条", "进度条", "progress")

    def _is_dropdown(n):
        if n.get("type") not in ("FRAME", "INSTANCE", "COMPONENT"):
            return False
        # 确定性命名约定：名字以 _Dropdown 结尾 → 下拉。兜底：含 dropdown/下拉/选择器/select。
        if (n.get("name", "") or "").lower().endswith("_dropdown"):
            return True
        return _name_has(n, "dropdown", "下拉", "选择器", "select")

    def _is_toggle(n):
        return n.get("type") in ("FRAME", "INSTANCE", "COMPONENT", "RECTANGLE", "GROUP") \
            and _name_has(n, "toggle", "checkbox", "复选", "勾选", "radio", "单选")

    def _is_scroll_list(n):
        """命名含 scroll/list/列表/滚动，或 clipsContent 且含 >=3 个等高行 → 滚动列表。"""
        if n.get("type") != "FRAME":
            return False
        if _name_has(n, "scrolllist", "scrollview", "scroll", "list", "列表", "滚动"):
            return True
        if n.get("clipsContent"):
            rows = [c for c in n.get("children", []) if c.get("type") in ("FRAME", "INSTANCE", "COMPONENT", "GROUP")]
            if len(rows) >= 3:
                hs = sorted(rect(c)["h"] for c in rows)
                if hs[0] > 4 and hs[-1] <= hs[0] * 1.6:   # 行高聚类（最高≤最矮的1.6倍）
                    return True
        return False

    def _round_fields(n, default_cr=0):
        cr = int(n.get("cornerRadius") or default_cr)
        if not cr:
            return {}
        sp, b = round_sprite(cr)
        return {"sprite": sp, "imageType": "Sliced", "border": {"l": b, "t": b, "r": b, "b": b}}

    def emit_scroll_list(n):
        nd = {"name": _san(n.get("name", "List")), "type": "ScrollList", "raycastTarget": True,
              "rect": rect(n), "scroll": {"horizontal": False, "vertical": True}}
        fill = first_solid_fill(n)
        if fill:
            nd["color"] = fill
            nd.update(_round_fields(n))
        return _apply_v2(nd, n)

    def _finalize_scroll_list(nd, kids):
        """ScrollList 收尾：把纯容器包装的内容上提一层(让真实行成 Content 直接子项)，按 y 排序，按行推算 spacing/padding。"""
        # Figma 常把行套在一个(或多个)与视口同尺寸的纯分组容器里 → 上提其子，否则布局组只堆一个项、撑不开。
        # 只展开"纯 Container"(无填充/精灵的分组)，避免误拆有视觉的行底。仅展开 ScrollList 的直接子层。
        flat = []
        for k in kids:
            if k.get("type") == "Container" and k.get("children") \
                    and not k.get("color") and not k.get("sprite"):
                flat.extend(k["children"])
            else:
                flat.append(k)
        kids = flat
        # 竖向列表按 y 顺序堆叠（横向按 x）；LayoutGroup 按子节点顺序排布，需先排好。
        horiz = (nd.get("scroll") or {}).get("horizontal") and not (nd.get("scroll") or {}).get("vertical", True)
        axis, ext = ("x", "w") if horiz else ("y", "h")
        items = [k for k in kids if k.get("rect")]
        # 用行高(或行宽)中位数过滤掉异常尺寸的装饰（如侧边滚动条轨道、整列底框），只留均匀的列表项。
        if len(items) >= 3:
            med = sorted(k["rect"][ext] for k in items)[len(items) // 2]
            if med > 0:
                items = [k for k in items if 0.5 * med <= k["rect"][ext] <= 1.8 * med]
        rows = sorted(items, key=lambda k: k["rect"].get(axis, 0))
        kids = rows + [k for k in kids if not k.get("rect")]

        sc = nd.get("scroll") or {"horizontal": False, "vertical": True}
        if rows:
            r = nd["rect"]; f = rows[0]["rect"]
            top = max(0, round(f["y"] - r["y"]))
            sc["padding"] = {"l": max(0, round(f["x"] - r["x"])), "t": top,
                             "r": max(0, round((r["x"] + r["w"]) - (f["x"] + f["w"]))), "b": top}
            if len(rows) >= 2:
                sc["spacing"] = max(0, round(rows[1]["rect"]["y"] - (rows[0]["rect"]["y"] + rows[0]["rect"]["h"])))
            else:
                sc["spacing"] = 0
        nd["scroll"] = sc
        return kids

    def emit_dropdown(n):
        nd = {"name": _san(n.get("name", "Dropdown")), "type": "Dropdown",
              "color": first_solid_fill(n) or "#0A1E46", "rect": rect(n)}
        nd.update(_round_fields(n, 8))
        cap = _find_text(n)
        if cap is not None:
            stl = cap.get("style", {})
            nd["text"] = {"content": cap.get("characters", ""), "fontSize": round(stl.get("fontSize", 16)),
                          "color": first_solid_fill(cap) or "#E8F4FF", "alignment": "MidlineLeft"}
            _cs = _char_spacing(stl)
            if _cs is not None:
                nd["text"]["characterSpacing"] = _cs
        st = _stroke_field(n, int(n.get("cornerRadius") or 8))
        if st:
            nd["stroke"] = st
        return _apply_v2(nd, n)

    def emit_toggle(n):
        nd = {"name": _san(n.get("name", "Toggle")), "type": "Toggle",
              "color": first_solid_fill(n) or "#1B2B52", "rect": rect(n), "isOn": False}
        nd.update(_round_fields(n))
        st = _stroke_field(n, int(n.get("cornerRadius") or 6))
        if st:
            nd["stroke"] = st
        return _apply_v2(nd, n)

    def emit_slider(n):
        nd = {"name": _san(n.get("name", "Slider")), "type": "Slider",
              "color": first_solid_fill(n) or "#1B2B52", "rect": rect(n),
              "direction": "LeftToRight", "range": {"min": 0, "max": 1, "value": 1}}
        return _apply_v2(nd, n)

    def emit_scrollbar(n):
        r = rect(n)
        horiz = r["w"] >= r["h"]
        nd = {"name": _san(n.get("name", "Scrollbar")), "type": "Scrollbar",
              "color": first_solid_fill(n) or "#1B2B52", "rect": r,
              "direction": "LeftToRight" if horiz else "TopToBottom",
              "scrollbarSize": 0.3, "range": {"min": 0, "max": 1, "value": 1}}
        return _apply_v2(nd, n)

    # 递归建嵌套树：镜像 Figma 层级（上下级关系），坐标用整帧绝对值（builder 按 parentAbsX 解算相对偏移）。
    # in_scroll: 已在某个 ScrollList 内部 → 不再把后代识别为 ScrollList（避免 ScrollRect 套 ScrollRect）。
    def _collapse_list_items(parent, kids, require_suffix=True):
        """命名重复 item（同名 ≥2）→ 模板化：保留位置最靠前的 1 个(标记 isItemTemplate)，删其余；
        父容器标记 list(方向/间距/数量/itemPrefab)。builder 据此抽独立 prefab + 父容器加 LayoutGroup。
        require_suffix=True(普通容器)：只认带类型后缀(_Btn/_Image…)的同名重复(用户显式命名)；
        require_suffix=False(ScrollList 内)：同名重复即列表项(段行 emit 后名字未必带后缀)。"""
        from collections import Counter
        SUF = ("_Btn", "_Image", "_InputField", "_Dropdown", "_Text", "_Toggle", "_Slider")
        cnt = Counter(k.get("name", "") for k in kids)
        if require_suffix:
            repeated = [nm for nm, c in cnt.items() if nm and c >= 2 and any(nm.endswith(s) for s in SUF)]
        else:
            repeated = [nm for nm, c in cnt.items() if nm and c >= 2]
        if not repeated:
            return kids
        base = repeated[0]   # 一个容器一般一种重复 item
        items = [k for k in kids if k.get("name") == base and k.get("rect")]
        others = [k for k in kids if k.get("name") != base]
        if len(items) < 2:
            return kids
        items.sort(key=lambda k: (k["rect"]["y"], k["rect"]["x"]))   # 位置序，取最靠前的作模板
        ys = sorted(k["rect"]["y"] for k in items)
        xs = sorted(k["rect"]["x"] for k in items)
        vertical = (ys[-1] - ys[0]) >= (xs[-1] - xs[0])
        gap = round(ys[1] - ys[0] - items[0]["rect"]["h"]) if vertical else round(xs[1] - xs[0] - items[0]["rect"]["w"])
        template = items[0]
        template["isItemTemplate"] = True
        parent["list"] = {"vertical": vertical, "spacing": max(0, gap), "count": len(items), "itemPrefab": base}
        return others + [template]

    def build_node(n, in_scroll=False):
        t = n["type"]
        if t == "VECTOR":
            return None
        role = exports.get(n["id"])
        if role in ("bg", "image", "art", "corner"):
            return emit_image(n, role)              # 位图叶子
        if t == "TEXT":
            return text_node(n)                     # 文本叶子
        if _is_input(n):
            return emit_input_field(n)              # 自包含（占位符/眼睛在内）
        if is_button(n):
            # 简单按钮：自身有背景(实色/描边/渐变) + 居中 label → 自包含叶子，居中文字作 label，不下钻。
            # 背景在子节点的按钮(自身 fills=[]，如 Figma 把底色放进子 Container)→ 落到容器按钮分支：
            # 保留子树，背景子 Container 作 Image 同步过去，否则按钮背景 sprite 会丢。
            self_bg = first_solid_fill(n) or first_stroke(n)[0] or first_gradient(n)
            if _find_centered_text(n) is not None and self_bg:
                return emit_bordered(n, as_button=True)
            # 容器按钮（列表项等：无居中文字、内部有需保留的子树如图标/左对齐描述）→
            # Button 背景 + 下钻保留子节点（子文本仍作独立 Text 可绑/可刷新）。
            cr = int(n.get("cornerRadius") or 0)
            fill = first_solid_fill(n)
            nd = {"name": _san(n.get("name", "Button")), "type": "Button",
                  "color": fill or "#FFFFFF00", "rect": rect(n)}
            if fill:  # 有实色填充才用 round sprite；透明列表项按钮不给(避免冷渲染/sprite 未就绪露白块)
                sp, b = round_sprite(cr if cr else 12)
                nd.update({"sprite": sp, "imageType": "Sliced", "border": {"l": b, "t": b, "r": b, "b": b}})
            stf = _stroke_field(n, cr if cr else 12)
            if stf:
                nd["stroke"] = stf
            nd = _apply_v2(nd, n)
            kids = [k for k in (build_node(c, in_scroll) for c in n.get("children", [])) if k]
            # 按钮底图常被 Figma 设成玻璃态半透明(如 #388BFD a=0.15)，深背景上几乎不可见 →
            # 把彩色半透明背景子(有 round 精灵的 Image)提到可见；透明白/黑装饰层跳过(避免露白块)。
            for k in kids:
                if k.get("type") == "Image" and k.get("sprite") and isinstance(k.get("color"), str):
                    k["color"] = _bump_btn_bg_alpha(k["color"])
            if kids:
                nd["children"] = kids
            return nd
        # 标准 UGUI 组件：自包含叶子（内部结构由 builder 构造，不再下钻 Figma 子节点）。
        # 顺序：scrollbar 先于 scroll_list（名字含 "scroll"/"滚动" 会同时命中）。
        if _is_scrollbar(n):
            return emit_scrollbar(n)
        if _is_slider(n):
            return emit_slider(n)
        if _is_dropdown(n):
            return emit_dropdown(n)
        if _is_toggle(n):
            return emit_toggle(n)
        # ScrollList：容器型，保留子项（下钻），子项落到 builder 的 Content 下。嵌套内不再识别。
        if _is_scroll_list(n) and not in_scroll:
            nd = emit_scroll_list(n)
        else:
            has_fill = first_solid_fill(n) is not None
            has_stroke = first_stroke(n)[0] is not None
            cr = int(n.get("cornerRadius") or 0)
            if has_fill and has_stroke:
                nd = emit_bordered(n)               # 圆角+描边容器（如行底）
            elif has_fill or has_stroke:
                nd = emit_solid(n)                  # 实色/描边容器（圆角由 fill/stroke 决定 sprite）
            else:
                # 无填充无描边：纯透明分组容器。即使有 cornerRadius 也不生成 Image——
                # 圆角对透明节点不可见，且白色透明 Image(#FFFFFF00+round sprite)在冷渲染/sprite 未就绪时会露白块。
                nd = {"name": _san(n.get("name", "Group")), "type": "Container",
                      "raycastTarget": False, "rect": rect(n)}   # 纯分组容器
        child_in_scroll = in_scroll or nd.get("type") == "ScrollList"
        kids = [k for k in (build_node(c, child_in_scroll) for c in n.get("children", [])) if k]
        if nd.get("type") == "ScrollList":
            kids = _finalize_scroll_list(nd, kids)
            kids = _collapse_list_items(nd, kids, require_suffix=False)   # ScrollList 内同名重复即列表项 → 抽模板
        else:
            kids = _collapse_list_items(nd, kids)   # 命名重复 item → 抽模板(保留1个 + 父容器标 list)
        if kids:
            nd["children"] = kids
        return nd

    # 从卡片节点开始：CardBase(卡片底) + 卡片各子节点（保留各自层级嵌套），跳过外层画板帧。
    card_n = card["node"]
    if card_n:
        out_nodes.append(emit_solid(card_n, name="CardBase"))
        for c in card_n.get("children", []):
            k = build_node(c)
            if k:
                out_nodes.append(k)
    else:
        top = build_node(doc)
        if top:
            out_nodes.extend(top.get("children") or [top])

    # 组件命名后缀：给固定类型的节点名加类型后缀，便于消费方(YC-Ego)按名稳定绑定。
    # 在 dedupe 之前做，后缀引起的同名冲突由 _dedupe_names 兜底加序号。
    def _apply_type_suffixes(nodes):
        suffix = {"Button": "_Btn", "Image": "_Image", "Text": "_Text",
                  "InputField": "_InputField", "Dropdown": "_Dropdown"}
        def _walk(o):
            if isinstance(o, dict):
                sfx = suffix.get(o.get("type"))
                nm = o.get("name")
                # item 模板节点不加后缀：名字要与 list.itemPrefab(抽出的 prefab 名)保持一致
                if not o.get("isItemTemplate") and sfx and isinstance(nm, str) and not nm.endswith(sfx):
                    o["name"] = nm + sfx
                for c in o.get("children") or []:
                    _walk(c)
            elif isinstance(o, list):
                for c in o:
                    _walk(c)
        for n in nodes:
            _walk(n)
    _apply_type_suffixes(out_nodes)

    _dedupe_names(out_nodes)

    # 正文字号归一化：整屏正文(Text)拉平为同一号，消除 Figma 带来的 10/11/12/14 混杂。
    # 标题与大按钮(>= 阈值，如 20/24)原样保留；目标号 = 正文区间内出现最多的号(并列取较大者，更易读)。
    def _normalize_body_font_sizes(nodes, threshold=18):
        body = []
        def _collect(o):
            if isinstance(o, dict):
                if o.get("type") == "Text":
                    fs = (o.get("text") or {}).get("fontSize")
                    if isinstance(fs, (int, float)) and fs < threshold:
                        body.append(round(fs))
                for c in o.get("children") or []:
                    _collect(c)
            elif isinstance(o, list):
                for c in o:
                    _collect(c)
        for n in nodes:
            _collect(n)
        if not body:
            return None
        target = max(set(body), key=lambda s: (body.count(s), s))  # 众数，并列取大
        def _apply(o):
            if isinstance(o, dict):
                if o.get("type") == "Text":
                    t = o.get("text") or {}
                    fs = t.get("fontSize")
                    if isinstance(fs, (int, float)) and fs < threshold:
                        t["fontSize"] = target
                for c in o.get("children") or []:
                    _apply(c)
            elif isinstance(o, list):
                for c in o:
                    _apply(c)
        for n in nodes:
            _apply(n)
        return target
    _normalize_body_font_sizes(out_nodes)

    spec = {"schemaVersion": 1, "referenceWidth": FW, "referenceHeight": FH,
            "rootName": a.panel,
            "root": {"name": a.panel, "type": "Container", "anchorPreset": "center",
                     "rect": {"x": 0, "y": 0, "w": FW, "h": FH}, "children": out_nodes}}
    # 真相源收敛（spec 004 Phase 1）：直接生成/覆盖 <Panel>.json，不再写 .draft.json。
    # 人工微调走"覆盖式重生成 + git diff 审阅"，spec 是 Figma 的忠实生成投影、单一真相。
    out_json = f"{panel_dir}/{a.panel}.json"
    existed = os.path.exists(out_json)
    with io.open(out_json, "w", encoding="utf-8") as f:
        json.dump(spec, f, ensure_ascii=False, indent=2)

    # 7.5) 清理孤儿图标（release）：Figma 里删掉的图标对应 PNG 仍残留在 Icons/，
    #      新 spec 不再引用即视为可释放 → 连同 .meta 删除，避免包体里堆死图。
    def _collect_sprites(obj, acc):
        if isinstance(obj, dict):
            s = obj.get("sprite")
            if isinstance(s, str) and s:
                acc.add(s.replace("\\", "/"))
            for v in obj.values():
                _collect_sprites(v, acc)
        elif isinstance(obj, list):
            for v in obj:
                _collect_sprites(v, acc)

    _refs = set()
    _collect_sprites(spec["root"], _refs)
    _icons_prefix = icons_dir.replace("\\", "/").rstrip("/") + "/"
    _keep = {os.path.basename(s) for s in _refs if s.startswith(_icons_prefix)}
    released = []
    if os.path.isdir(icons_dir):
        for fn in sorted(os.listdir(icons_dir)):
            if not fn.lower().endswith(".png"):
                continue
            if fn in _keep:
                continue
            removed = False
            for ext in ("", ".meta"):
                p = os.path.join(icons_dir, fn + ext)
                if os.path.exists(p):
                    try:
                        os.remove(p); removed = True
                    except OSError:
                        pass
            if removed:
                released.append(fn)

    # 7) 版式报告（人读）
    with io.open(f"{meta_dir}/layout.txt", "w", encoding="utf-8") as f:
        f.write(f"fileKey={a.file} node={node} frame={FW}x{FH} card_r={card['r']} lastModified={data.get('lastModified')}\n")
        _dump(doc, OX, OY, exports, f)

    print("=== figma-sync done ===")
    print(f"node {node}  frame {FW}x{FH}  card r={card['r']}")
    print(f"assets -> {icons_dir}/ :")
    for e in exported:
        print("  " + e)
    print(f"spec  -> {out_json}" + ("  (覆盖式重生成，用 git diff 审阅改动)" if existed else "  (新建)"))
    if released:
        print(f"released -> 清理 {len(released)} 个孤儿图标(Figma 已删/不再引用): " + ", ".join(released))
    print(f"layout report-> {meta_dir}/layout.txt")
    print(f"truth image  -> {meta_dir}/truth.png")
    print("NEXT: 用 ui-build-render.ps1 构建（常态不渲染；要核对图加 -Verify $true）")


def _dedupe_names(nodes):
    """同级重名加序号后缀（UISpecValidator 要求兄弟唯一）；递归处理 children。"""
    seen = {}
    for nd in nodes:
        base = nd.get("name", "X")
        if base in seen:
            seen[base] += 1
            nd["name"] = f"{base}_{seen[base]}"
        else:
            seen[base] = 0
        kids = nd.get("children")
        if kids:
            _dedupe_names(kids)


def _san(s):
    return re.sub(r"[^0-9A-Za-z_]+", "", (s or "X").split("(")[0]) or "X"


def _bump_alpha(hexc, minA):
    if len(hexc) == 9:
        a = int(hexc[7:9], 16) / 255.0
        if a < minA:
            return hexc[:7] + "%02X" % round(minA * 255)
        return hexc
    return hexc  # 已不透明


def _bump_btn_bg_alpha(hexc, minA=0.5):
    """按钮底图在深背景上需可见：Figma 常把按钮底设成玻璃态半透明(如 #388BFD a=0.15)，
    在浅色画布上能看见、在深色运行背景上几乎消失 → 把彩色半透明底提到 minA(可见)。
    跳过接近白/黑的透明装饰层(提了会露白块/黑块)，跳过已足够不透明的。"""
    if len(hexc) != 9:
        return hexc  # 不透明，原样
    r, g, b = int(hexc[1:3], 16), int(hexc[3:5], 16), int(hexc[5:7], 16)
    a = int(hexc[7:9], 16) / 255.0
    if a >= minA:
        return hexc
    near_white = r > 230 and g > 230 and b > 230
    near_black = r < 25 and g < 25 and b < 25
    if near_white or near_black:
        return hexc  # 透明白/黑装饰层：提 alpha 会变成白块/黑块，保持透明
    return hexc[:7] + "%02X" % round(minA * 255)


def _dump(n, ox, oy, exports, f, d=0):
    if n["type"] == "VECTOR":
        return
    bb = n.get("absoluteBoundingBox")
    r = f"({round(bb['x']-ox)},{round(bb['y']-oy)} {round(bb['width'])}x{round(bb['height'])})" if bb else "(-)"
    ex = []
    sf = first_solid_fill(n)
    if sf: ex.append("fill=" + sf)
    if has_image_fill(n): ex.append("IMAGE")
    sc, sw = first_stroke(n)
    if sc: ex.append(f"stroke={sc}@{round(sw,1)}")
    if n.get("cornerRadius"): ex.append("r=%d" % int(n["cornerRadius"]))
    if n["type"] == "TEXT":
        st = n.get("style", {})
        ex.append(f'"{n.get("characters","")}" fs={round(st.get("fontSize",0),1)} w={st.get("fontWeight")} a={str(st.get("textAlignHorizontal"))[:1]}')
    if exports.get(n["id"]): ex.append("[EXPORT:%s]" % exports[n["id"]])
    f.write(f"{'  '*d}{n['type']:8} '{n.get('name','')}' {r} {' | '.join(ex)}\n")
    if exports.get(n["id"]) in ("art", "corner", "bg", "image"):
        return
    for c in n.get("children", []):
        _dump(c, ox, oy, exports, f, d + 1)


if __name__ == "__main__":
    main()

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


def round_sprite(r):
    if r <= 12:
        return "Assets/UI/Common/round12.png", 12
    if r <= 18:
        return "Assets/UI/Common/round16.png", 16
    return "Assets/UI/Common/round24.png", 24


def ring_sprite(r):
    """镚空描边环精灵(中间透明)，用于半透面板描边，避免实心底色把半透填充染色。"""
    if r <= 12:
        return "Assets/UI/Common/ring12.png", 12
    return "Assets/UI/Common/ring16.png", 16


def text_align(n):
    st = n.get("style", {})
    h = (st.get("textAlignHorizontal") or "LEFT").upper()
    return {"LEFT": "MidlineLeft", "RIGHT": "MidlineRight", "CENTER": "Center", "JUSTIFIED": "MidlineLeft"}.get(h, "MidlineLeft")


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

    def asset_path(nid):
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

    # 5) 合成图（真值参照）：导卡片节点（与渲染同框，便于 -Verify 做可靠 MAE），无卡片则退回整帧
    truth_id = card_id or node
    j = get_json(f"{API}/images/{a.file}?ids={truth_id}&format=png&scale=2", a.token)
    tu = j.get("images", {}).get(truth_id)
    if tu:
        curl(tu, out=f"{meta_dir}/truth.png")

    # 6) 生成 UISpec 草稿（扁平化：全部直接挂 root，rect 用整帧绝对像素）
    out_nodes = []

    def is_button(n):
        if not (n["type"] == "FRAME" and first_solid_fill(n)):
            return False
        if "button" not in n.get("name", "").lower():
            return False
        return _find_centered_text(n) is not None

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
        if st.get("fontWeight", 400) >= 600:
            nd["text"]["style"] = {"bold": True}
        return nd

    def _apply_v2(nd, n):
        """把 Figma 的渐变/整体不透明度写进节点的 v2 字段（spec 004 Phase 2）。"""
        g = first_gradient(n)
        if g:
            nd["gradient"] = g
            nd["color"] = "#FFFFFF"   # 渐变由顶点色驱动，底色置白避免相乘偏色
        o = node_opacity(n)
        if o is not None:
            nd["opacity"] = round(o, 3)
        return nd

    def _stroke_field(n, cr):
        """stroke 收敛为节点 v2 字段（spec 004 Phase 3：builder 用 SDF 直接画描边，不再用环精灵）。"""
        scol, sw = first_stroke(n)
        if not scol:
            return None
        return {"color": scol, "weight": round(sw, 2), "align": "Inside"}

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
        """fill+stroke 框，且含命名带 'input'/'输入' 的后代 → 视为输入框。"""
        if n.get("type") != "FRAME" or not (first_solid_fill(n) and first_stroke(n)[0]):
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
        cr = int(n.get("cornerRadius") or 12)
        r = rect(n)
        nm = _san(n.get("name", "Input"))
        nd = {"name": nm + "Input", "type": "InputField", "color": first_solid_fill(n) or "#0A1E46",
              "rect": r, "cornerRadius": cr,
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
        out_nodes.append(_apply_v2(nd, n))

    def emit_image(n, role):
        nd = {"name": _san(n.get("name", "Image")), "type": "Image", "color": "#FFFFFF", "raycastTarget": False,
              "sprite": f"{panel_dir}/Icons/{asset_path(n['id'])}", "rect": rect(n)}
        o = node_opacity(n)
        if o is not None:
            nd["opacity"] = round(o, 3)
        out_nodes.append(nd)

    def emit_solid(n):
        cr = int(n.get("cornerRadius") or 0)
        nd = {"name": _san(n.get("name", "Rect")), "type": "Shape" if cr else "Image",
              "color": first_solid_fill(n) or "#FFFFFF", "raycastTarget": False, "rect": rect(n)}
        if cr:
            nd["cornerRadius"] = cr
        st = _stroke_field(n, cr or 12)
        if st:
            nd["stroke"] = st
        out_nodes.append(_apply_v2(nd, n))

    def emit_bordered(n, as_button=False):
        """fill+stroke -> 单节点 SDF：cornerRadius + stroke(色/宽)，builder 用 UIShape 零纹理画圆角+描边。"""
        cr = int(n.get("cornerRadius") or 12)
        r = rect(n)
        nm = _san(n.get("name", "Field"))
        fill = first_solid_fill(n)
        stroke = _stroke_field(n, cr)
        if as_button:
            txt = _find_centered_text(n)
            nd = {"name": nm, "type": "Button", "color": fill or "#FFFFFF", "rect": r, "cornerRadius": cr}
            if txt:
                stl = txt.get("style", {})
                nd["text"] = {"content": txt.get("characters", ""), "fontSize": round(stl.get("fontSize", 16)),
                              "color": first_solid_fill(txt) or "#FFFFFF", "alignment": "Center"}
                if stl.get("fontWeight", 400) >= 600:
                    nd["text"]["style"] = {"bold": True}
        else:
            nd = {"name": nm + "Fill", "type": "Shape", "color": fill or "#FFFFFF", "raycastTarget": False,
                  "rect": r, "cornerRadius": cr}
        if stroke:
            nd["stroke"] = stroke
        out_nodes.append(_apply_v2(nd, n))

    def visit(n, in_button=False):
        t = n["type"]
        role = exports.get(n["id"])
        if t == "TEXT":
            if not in_button:
                out_nodes.append(text_node(n))
            return
        if role in ("bg", "image", "art", "corner"):
            emit_image(n, role)
            return
        if is_button(n):
            if first_stroke(n)[0]:
                emit_bordered(n, as_button=True)
            else:
                emit_solid(n)  # 简化：无描边按钮当实色（少见）
                for c in n.get("children", []):
                    visit(c, in_button=False)
            return
        if _is_input(n):
            emit_input_field(n)  # 语义组件：→ TMP_InputField（消费占位符+眼睛，不再递归）
            return
        has_fill = first_solid_fill(n) is not None
        has_stroke = first_stroke(n)[0] is not None
        is_card = n["id"] == card_id
        if is_card:
            emit_solid(n)  # CardBase
            for c in n.get("children", []):
                visit(c)
            return
        if has_fill and has_stroke:
            emit_bordered(n)  # 非输入的 fill+stroke 框（如普通面板）
            for c in n.get("children", []):
                visit(c)  # 占位符文字等作为扁平兄弟
            return
        if has_fill or (has_stroke and n.get("children")):
            emit_solid(n)
            for c in n.get("children", []):
                visit(c)
            return
        # 纯容器：透传递归
        for c in n.get("children", []):
            visit(c)

    visit(doc)
    _dedupe_names(out_nodes)

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

    # 7) 版式报告（人读）
    with io.open(f"{meta_dir}/layout.txt", "w", encoding="utf-8") as f:
        f.write(f"node={node} frame={FW}x{FH} card_r={card['r']}\n")
        _dump(doc, OX, OY, exports, f)

    print("=== figma-sync done ===")
    print(f"node {node}  frame {FW}x{FH}  card r={card['r']}")
    print(f"assets -> {icons_dir}/ :")
    for e in exported:
        print("  " + e)
    print(f"spec  -> {out_json}" + ("  (覆盖式重生成，用 git diff 审阅改动)" if existed else "  (新建)"))
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

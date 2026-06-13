# -*- coding: utf-8 -*-
"""由 figma/*.meta.json 重新生成 figma/RECOVERY.md 里的面板索引表（标记区间内）。
每次同步后调用，保证人读索引与机器元数据一致。用法: python scripts/figma_index.py
"""
import os, io, json, glob

RECOVERY = "figma/RECOVERY.md"
BEGIN = "<!-- BEGIN auto-index -->"
END = "<!-- END auto-index -->"


def main():
    metas = []
    for p in sorted(glob.glob("figma/*.meta.json")):
        try:
            metas.append(json.load(io.open(p, encoding="utf-8")))
        except Exception:
            pass

    lines = ["| Panel | folder | fileKey | node | 设计快照 | lastModified |",
             "|-------|--------|---------|------|----------|--------------|"]
    for m in metas:
        panel = m.get("panel", "?")
        snap_ok = os.path.exists(f"figma/{panel}.nodes.json") and m.get("snapshotAvailable", True)
        snap = f"✅ `figma/{panel}.nodes.json`" if snap_ok else "❌ 源 node 已失效，真相=spec"
        lines.append(f"| {panel} | {m.get('folder','')} | `{m.get('fileKey','')}` | "
                     f"`{m.get('node','')}` | {snap} | {m.get('lastModified') or '-'} |")
    table = "\n".join(lines)

    if not os.path.exists(RECOVERY):
        print(f"WARN: {RECOVERY} 不存在，跳过"); return
    txt = io.open(RECOVERY, encoding="utf-8").read()
    block = f"{BEGIN}\n{table}\n{END}"
    if BEGIN in txt and END in txt:
        pre = txt[:txt.index(BEGIN)]
        post = txt[txt.index(END) + len(END):]
        txt = pre + block + post
    else:
        # 没有标记就追加到文末
        txt = txt.rstrip() + "\n\n## 面板索引（自动生成）\n\n" + block + "\n"
    io.open(RECOVERY, "w", encoding="utf-8").write(txt)
    print(f"index -> {RECOVERY} ({len(metas)} panels)")


if __name__ == "__main__":
    main()

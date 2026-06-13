# -*- coding: utf-8 -*-
"""列出某 Figma 文件的顶层帧（id + name），用于在 node 未知/失效时发现新 node-id。
用法: python scripts/figma_frames.py <fileKey> [keyword] [--token TOK]
keyword 给了就只打印名字含该词的帧（大小写不敏感）。输出每行: <id>\t<name>
"""
import sys, os, json, argparse, urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
try:
    from figma_sync import FILE_KEY_DEFAULT
except Exception:
    FILE_KEY_DEFAULT = ""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("file", nargs="?", default="")
    ap.add_argument("--kw", default="")
    ap.add_argument("--token", default=os.environ.get("FIGMA_TOKEN", ""))
    ap.add_argument("--depth", type=int, default=2)
    a = ap.parse_args()
    if not a.file or a.file == "-":
        a.file = FILE_KEY_DEFAULT
    a.keyword = a.kw
    sys.stdout.reconfigure(encoding="utf-8")
    tok = a.token
    if not tok and os.path.exists(".figma-token"):
        tok = open(".figma-token", encoding="utf-8").read().strip()
    if not tok:
        print("ERROR: no token"); sys.exit(2)

    url = f"https://api.figma.com/v1/files/{a.file}?depth={a.depth}"
    req = urllib.request.Request(url, headers={"X-Figma-Token": tok})
    try:
        d = json.load(urllib.request.urlopen(req, timeout=60))
    except Exception as e:
        print(f"ERROR: {e}"); sys.exit(2)

    kw = a.keyword.lower()
    rows = []
    for pg in d.get("document", {}).get("children", []):
        for fr in pg.get("children", []):
            nm = fr.get("name", "") or ""
            if fr.get("type") in ("FRAME", "COMPONENT", "SECTION") and (not kw or kw in nm.lower()):
                rows.append((fr.get("id"), nm))
    print(f"# file={a.file} lastModified={d.get('lastModified')} frames={len(rows)}"
          + (f" filter={a.keyword!r}" if kw else ""))
    for nid, nm in rows:
        print(f"{nid}\t{nm}")


if __name__ == "__main__":
    main()

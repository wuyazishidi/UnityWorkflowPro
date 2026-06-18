# -*- coding: utf-8 -*-
"""
Figma 同步状态巡检：遍历 figma/*.meta.json，对比「上次同步记录的 lastModified」与
「Figma 文件当前 lastModified」，判断每个面板是否最新；并识别 Figma /nodes 的 CDN 缓存窗口
（文件已改但 /nodes 还回旧值 → 此时同步会拿旧版，应稍后再同步）。

判断（每面板）：
  file_lm == meta_lm                       -> UPTODATE     已最新
  file_lm != meta_lm 且 node 不存在          -> NODE-GONE    设计重构，node 失效，需新 node-id
  file_lm != meta_lm 且 nodes_lm == file_lm  -> STALE-READY  过期且 /nodes 缓存已刷新 -> 可安全同步
  file_lm != meta_lm 且 nodes_lm <  file_lm  -> STALE-CACHED 过期但 /nodes 仍在缓存窗口 -> 稍后再同步

用 curl 子进程访问 Figma（继承系统代理，与 figma_sync.py 一致；无需手动设 HTTPS_PROXY）。
用法：
  python scripts/figma_check.py          # 人读表格
  python scripts/figma_check.py --json    # 仅输出「可安全同步」列表(JSON)，供 ps1 自动同步消费
"""
import sys, os, json, glob, subprocess, argparse, time

API = "https://api.figma.com/v1"
try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass


def curl(url, token, timeout=40):
    r = subprocess.run(["curl", "-s", "-m", str(timeout), "-H", f"X-Figma-Token: {token}", url],
                       capture_output=True)
    return r.stdout


def get_json(url, token, tries=3):
    for i in range(tries):
        raw = curl(url, token)
        s = raw.decode("utf-8", "replace").strip()
        if s:
            try:
                return json.loads(s)
            except Exception:
                pass
        time.sleep(1.5 * (i + 1))
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--token", default=os.environ.get("FIGMA_TOKEN", ""))
    ap.add_argument("--json", action="store_true", help="只输出可安全同步列表(JSON)")
    a = ap.parse_args()

    token = a.token
    if not token and os.path.exists(".figma-token"):
        token = open(".figma-token", encoding="utf-8").read().strip()
    if not token:
        print("ERROR: 缺少 token（.figma-token 或 --token / FIGMA_TOKEN）", file=sys.stderr)
        sys.exit(2)

    rows = []
    sync_ready = []
    for mf in sorted(glob.glob("figma/*.meta.json")):
        m = json.load(open(mf, encoding="utf-8"))
        panel, fk, node, meta_lm = m.get("panel"), m.get("fileKey"), m.get("node"), m.get("lastModified")

        fd = get_json(f"{API}/files/{fk}?depth=1", token)
        if not fd:
            rows.append((panel, node, "QUERY-FAIL", "查询 /files 失败（网络/限流）"))
            continue
        file_lm = fd.get("lastModified")

        if file_lm == meta_lm:
            rows.append((panel, node, "UPTODATE", "已最新"))
            continue

        nd = get_json(f"{API}/files/{fk}/nodes?ids={node}", token)
        node_obj = (nd or {}).get("nodes", {}).get(node)
        nodes_lm = (nd or {}).get("lastModified")
        if not node_obj:
            rows.append((panel, node, "NODE-GONE", f"node 已失效（设计重构）；Figma当前 {file_lm}，需新 node-id"))
        elif nodes_lm == file_lm:
            rows.append((panel, node, "STALE-READY", f"过期，/nodes 缓存已刷新 -> 可同步（{meta_lm} -> {file_lm}）"))
            sync_ready.append({"panel": panel, "node": node, "fileKey": fk})
        else:
            rows.append((panel, node, "STALE-CACHED", f"过期，但 /nodes 仍缓存于 {nodes_lm}（文件 {file_lm}）-> 稍后再同步"))

    if a.json:
        print(json.dumps(sync_ready, ensure_ascii=False))
        return

    print(f'{"Panel":<18}{"node":<9}{"状态":<14}说明')
    print("-" * 96)
    for panel, node, status, detail in rows:
        print(f'{str(panel):<18}{str(node):<9}{status:<14}{detail}')
    print("-" * 96)
    ready = [s["panel"] for s in sync_ready]
    gone = [r[0] for r in rows if r[2] == "NODE-GONE"]
    cached = [r[0] for r in rows if r[2] == "STALE-CACHED"]
    print(f'可安全同步(过期且缓存就绪): {ready or "无"}')
    if cached:
        print(f'缓存窗口内(稍后再同步)      : {cached}')
    if gone:
        print(f'node 失效(需在 Figma 取新 node-id): {gone}')


if __name__ == "__main__":
    main()

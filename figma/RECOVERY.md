# Figma 来源与恢复索引（committed，无密钥）

> 目的：上下文丢失 / 换机 / Figma 被清空后，**最快恢复 UI 同步工作**。
> 这里只放「来源、接口、命令」；**密钥在 `secrets.local.md`（gitignore，不入库）**。

## 0. 30 秒恢复清单

1. 确认密钥：项目根 `.figma-token` 在不在？不在 → 从 `secrets.local.md` 抄回（同一个值）。
2. 确认环境：Unity 已打开（端口实测 **3212/3213**，无 `.port` 走默认）；调闸门/同步的 shell 先设
   `$env:NO_PROXY="127.0.0.1,localhost"`（Figma 外网仍走系统代理，本地 Unity 直连）。详见记忆 `uto-proxy-fix`。
3. 要重建某面板 → 看下表拿 `fileKey`+`node`，跑 `resync` 命令（见 §3）。
4. 设计已从 Figma 删除但要看原始数据 → 读 `figma/<Panel>.nodes.json`（原始节点树快照）。

## 1. 面板来源索引

机器可读真相 = `figma/*.meta.json`（每次 `figma-sync` 自动写）。下表由 `scripts/figma_index.py` 自动生成（勿手改标记区间内）：

<!-- BEGIN auto-index -->
| Panel | folder | fileKey | node | 设计快照 | lastModified |
|-------|--------|---------|------|----------|--------------|
| LoginPanel | Assets/UI/LoginPanel | `wGp5DXqAjtpwuPS4qMWkxP` | `20:388` | ✅ `figma/LoginPanel.nodes.json` | 2026-06-13T07:37:39Z |
| TaskDetailPanel | Assets/UI/TaskDetailPanel | `wGp5DXqAjtpwuPS4qMWkxP` | `8:40` | ❌ 源 node 已失效，真相=spec | - |
| TaskListPanel | Assets/UI/TaskListPanel | `wGp5DXqAjtpwuPS4qMWkxP` | `1:3` | ❌ 源 node 已失效，真相=spec | - |
| UpLoadPanel | Assets/UI/UpLoadPanel | `0yO2Lx5vRsG7X6OtbPqzb2` | `6:763` | ✅ `figma/UpLoadPanel.nodes.json` | 2026-06-13T10:47:37Z |
<!-- END auto-index -->

> ⚠️ 早期面板（Upload/TaskList/TaskDetail）的源 node 因从 **Figma Make 重新粘贴进 Design** 被覆盖/改名而失效（见记忆 `figma-make-paste-corruption`）。它们生成的 `<Panel>.json` 已入库，是当前存活真相；要再改设计须在 Figma 重做并给新 node-id。

## 2. 用到的 Figma REST 接口

Base：`https://api.figma.com/v1`　认证：请求头 `X-Figma-Token: <token>`（scope `file_content:read`）

| 用途 | 接口 |
|------|------|
| 拉节点树（同步核心） | `GET /files/{key}/nodes?ids={node}` |
| 导出图片/精灵/核对图 | `GET /images/{key}?ids={ids}&format=png&scale={n}` |
| 发现文件内有哪些帧（浅） | `GET /files/{key}?depth=2`（列页面+顶层帧及其 id） |
| 全量文件（慎用，大/易断） | `GET /files/{key}` |

代码：`scripts/figma_sync.py`（`curl` 子进程，继承系统代理；`get_json` 带限流重试）。

## 3. 恢复 / 同步命令

```powershell
# 0) shell 先关本地代理拦截（每次新 shell）
$env:NO_PROXY="127.0.0.1,localhost"; $env:no_proxy="127.0.0.1,localhost"

# 1) 同步某面板（自动：拉取→导资源→生成 spec→快照→Refresh→打图集→构建[→核对图]）
.\Packages\cn.etetet.yiuimcp\Config\figma-sync.ps1 -Node <node> -Panel <Panel> -FileKey <key> -Verify

# 2) 只重建 prefab（不打 Figma API，应用 builder 改动）
.\Packages\cn.etetet.yiuimcp\Config\ui-build-render.ps1 -Spec Assets/UI/<Panel>/<Panel>.json -Prefab Assets/UI/<Panel>/<Panel>.prefab

# 3) 发现某文件现有的顶层帧（拿新 node-id）
python - <<'PY'
import json,urllib.request
tok=open('.figma-token').read().strip(); key="wGp5DXqAjtpwuPS4qMWkxP"
d=json.load(urllib.request.urlopen(urllib.request.Request(f"https://api.figma.com/v1/files/{key}?depth=2",headers={"X-Figma-Token":tok})))
for pg in d["document"]["children"]:
    for fr in pg.get("children",[]): print(fr.get("id"), fr.get("name"))
PY
```

## 4. 快照机制（自动）

`figma_sync.py` 每次同步调用 `save_source_snapshot()`，向 `figma/` 写：
- `figma/<Panel>.nodes.json`：该 node 的完整原始子树（设计离线备份，Figma 清掉也还在）；
- `figma/<Panel>.meta.json`：`{fileKey, node, folder, spec, lastModified, api, resync}`。

`figma/` 顶层目录**入库**（`/Assets/UI/*/.figma/` 那个是易变中间产物，被 gitignore，别混淆）。

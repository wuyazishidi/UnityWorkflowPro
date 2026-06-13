---
name: figma-sync
description: 把 Figma 设计同步成 UGUI prefab 的自治流程。当用户给出 Figma 链接（figma.com/design/...）、给出 node-id、或说"某某面板更新了/刷新下设计/同步设计/改了设计图"时使用。自动完成：解析来源→设代理→发现/同步→存快照→核对→回填恢复索引。
---

# Figma → UGUI 同步

把"一个 Figma 链接 / 一句'XX 面板更新了'"变成"同步好的 UGUI prefab + 离线快照 + 更新的恢复索引"。
底层统一调 `Packages/cn.etetet.yiuimcp/Config/figma.ps1`（`/figma` 命令与本 Skill 共用它）。
**前提**：Unity 编辑器已打开本工程（端口实测 3212/3213）。上下文背景见 `figma/RECOVERY.md`。

## 流程（尽量自驱动，少打断）

### 1. 确定来源（fileKey + node + Panel）
- 用户给了 **URL** → 用 `-Url`，脚本自动解析 fileKey + node-id。
- 用户给了 **node-id** → 用 `-Node`（默认 fileKey 在 `figma_sync.py`，跨文件加 `-FileKey`）。
- **Panel 名**：从 URL 文件名 / 既有 `Assets/UI/<Panel>/` / 用户话里推断。已有面板的来源可查 `figma/<Panel>.meta.json`。
- 用户只说"XX 面板更新了"但没给链接，且 `figma/<Panel>.meta.json` 里的 node 可能已失效 → 先 **发现帧**（见 §3），按名匹配后用 AskUserQuestion 确认 node，**不要瞎猜乱同步**。

### 2. 同步（核对图默认开）
```powershell
powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma.ps1 -Url "<URL>" -Panel <Panel>
# 或： -Node <id> -Panel <Panel> [-FileKey <key>]   渲染慢可加 -NoVerify
```
脚本会：设 `NO_PROXY` → 拉取+导资源+生成 `<Panel>.json` → **自动快照** `figma/<Panel>.{nodes,meta}.json` → Refresh+打图集+构建 prefab →（-Verify 时）出 `_render.png` 并算 MAE → 回填 `figma/RECOVERY.md` 索引。

### 3. node 未知 / 失效 → 发现帧
```powershell
powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma.ps1 -Discover -Panel <关键词>
# 或列某文件全部顶层帧： -Discover -FileKey <key>
```
若文件 `lastModified` 没变且找不到目标帧 → 说明设计在**另一个文件**，向用户要那个面板的 Figma 链接（见记忆 `figma-make-paste-corruption`：MakeIt 重粘会让旧 node 失效）。

### 4. 核对与收尾
- 读 `Assets/UI/<Panel>/_render.png` 目视核对（重点核对此次改动处），报告全局 MAE 与是否 DONE。
- 如实报告：同步的 node、MAE、快照是否写入。MAE 偏大或目视有差就指出，别声称完美。

## 边界
- 改了 `Assets/*.cs`（如扩展 builder/映射）才需走编译闸门 + DoD；纯同步/改 spec/python 不触发。
- Unity 未开 → 构建会失败，明确标注"待构建未完成"，不要伪造。
- 密钥在 `secrets.local.md`（不入库）；`.figma-token` 缺了从那里抄回。

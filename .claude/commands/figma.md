---
description: 同步 Figma 设计到 UGUI prefab（给 URL 或 node-id）——解析→设代理→发现/同步→快照→核对→回填恢复索引
argument-hint: <Figma URL> 或 <node-id> [Panel名]
---

把一个 Figma 设计同步成 UGUI prefab。参数 `$ARGUMENTS` 可以是完整 Figma URL（含 fileKey+node-id），或 `node-id [Panel]`。

底层统一调 `Packages/cn.etetet.yiuimcp/Config/figma.ps1`（Skill `figma-sync` 与本命令共用它）。**前提**：Unity 编辑器已打开本工程。

## 执行

1. **解析参数 `$ARGUMENTS`**：
   - 是 URL → 直接传 `-Url`（脚本自动取 fileKey + node-id）。
   - 是 `node-id [Panel]` → 传 `-Node`（必要时 `-Panel`、`-FileKey`）。
   - **Panel 名**：从 URL 文件名/用户话里推断（如 LoginPanel / UpLoadPanel）；拿不准就用 `-Discover` 先列帧再问用户确认。

2. **同步**（核对图默认开，渲染慢可加 `-NoVerify`；**默认带 `-Deliver` 自动交付到 YC-Ego**）：
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma.ps1 -Url "<URL>" -Panel <Panel> -Deliver
   # 或： -Node <id> -Panel <Panel> [-FileKey <key>] -Deliver
   ```
   `-Deliver`：同步后自动 publish（binding + `.unitypackage`）并 sync 到消费方 YC-Ego（`../YC-Ego/Assets/Resources/UI/<Panel>/`，连 item prefab + 依赖脚本）。一条命令走完 Figma → prefab → binding → YC-Ego。只想看 prefab、不交付时去掉 `-Deliver`。

3. **node 找不到 / 没给链接** → 先发现帧，按 Panel 名匹配后让用户确认 node：
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma.ps1 -Discover -Panel <关键词>
   ```

4. **核对与收尾**：读 `Assets/UI/<Panel>/_render.png` 目视核对、报告全局 MAE；脚本已自动把原始设计快照写入 `figma/<Panel>.nodes.json`、来源写入 `figma/<Panel>.meta.json`，并回填 `figma/RECOVERY.md` 索引。

注意：脚本已内置 `NO_PROXY` 避开本机 Clash 拦 127.0.0.1（见 `figma/RECOVERY.md`）。若 Unity 未开，构建步骤会失败——明确标注未完成，勿伪造。

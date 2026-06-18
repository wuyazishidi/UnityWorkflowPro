---
name: ui-publish
description: 把一个已生成的 UI 面板发布为可移植交付包（binding 描述 + .unitypackage），供消费方（YC-Ego）解耦消费。当用户说"发布/交付/导出 XX 面板"、"把 XX 面板给到 YC-Ego/客户端"、"更新交付包"、"重新发布 UI"时使用。自动完成：清代理→（可选重建）→产 binding.json/.md→打包→写 README。
---

# UI 发布交付（Prefab + binding 描述 → .unitypackage）

把"一个已生成的面板"变成"消费方拿来即用的交付包"：`<Panel>.binding.json/.md` + `delivery/<Panel>.unitypackage` + `README`。
底层统一调 `Packages/cn.etetet.yiuimcp/Config/publish-ui.ps1`（`/publish-ui` 命令与本 Skill 共用它）。

**职责红线（解耦）**：本工程只产「Prefab + 描述 + 依赖包」；消费方（YC-Ego）只读 `binding.json` + 导入 `.unitypackage`，**绝不引用本工程代码**。唯一共享接口 = `binding.json` 格式（spec `005-ui-binding-contract.md`，schema v1.0）。

**前提**：Unity 编辑器已打开本工程（端口实测 3212/3213）。

## 流程（尽量自驱动）

### 1. 确定面板 + 是否重建
- Panel 名从用户话 / `Assets/UI/<Panel>/` 推断；拿不准就列已有面板用 AskUserQuestion 确认，别瞎发。
- 设计**刚同步过**（先跑了 `figma-sync`）→ 加 `-Build`，据 `<Panel>.json` 重建 prefab 再发布，确保描述对得上最新设计；否则发布现有 prefab。

### 2. 发布
```powershell
powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\publish-ui.ps1 -Panel <Panel> [-Build]
```
脚本会：清 Clash 代理 →（-Build）`BuildUIFromSpec` →`ExportBindingDescriptor`(走真实 prefab 树产 binding.json/.md) →`ExportUiPackage`(打包含依赖+.meta) → 写 README。每步按**产物文件刷新**判成败（UTO 经 Write-Host 输出、成功不设退出码，不能解析 stdout/退出码）；连续调用偶发端口冲突已内置清 node+重试。

### 3. 核对并报告（如实）
- 读 `Assets/UI/<Panel>/<Panel>.binding.md`，报：可绑元素数、`keyAuto` 个数（Figma 名非 ASCII 的兜底键，建议改名后重发布拿稳定 key）、关键交互是否齐全。
- **关键缺口**：若本该可点的按钮 `type` 仍是 `Text`（Figma 里只是 Container+Text）→ 明确告诉用户「该按钮在 Figma 没成 Button，YC-Ego 接不了 onClick；需在 Figma 用稳定 ASCII 名 + 填充框/居中文字后 `-Build` 重发布」。
- 报告交付物：`delivery/<Panel>.unitypackage` + `README` 路径与大小。

### 4. 消费方提示（仅告知，不在本工程做）
- YC-Ego 导入 `.unitypackage`；首次需 `Window > TextMeshPro > Import TMP Essential Resources`（binding.json 的 `needsTmpEssentials=true` 已提示）。
- YC-Ego 据 `binding.json` 的 `key→path→type` 绑事件，全程不接触 Figma、不引用本工程。

> Unity 未开则失败——标注未完成勿伪造。批量发布：对多个面板循环调用即可。

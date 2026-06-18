---
description: 把一个 UI 面板发布为可移植交付包（binding 描述 + .unitypackage），供消费方（YC-Ego）解耦消费
argument-hint: <Panel名> [-Build]
---

把一个已生成的面板 prefab 发布成交付物：`<Panel>.binding.json/.md`（绑定描述）+ `delivery/<Panel>.unitypackage`（prefab + 图标 + atlas + 字体/公共精灵依赖，连 `.meta`，GUID 跨工程保真）+ `README`。

**职责红线**：本工程只产「Prefab + 描述 + 依赖包」；消费方（YC-Ego）只读 `binding.json` + 导入包，**不引用本工程任何代码**。共享的唯一契约 = `binding.json` 格式（spec `005-ui-binding-contract.md`）。

底层调 `Packages/cn.etetet.yiuimcp/Config/publish-ui.ps1`。**前提**：Unity 编辑器已打开本工程（端口实测 3212/3213）。

## 执行

1. **解析参数 `$ARGUMENTS`**：第一个 token = Panel 名（如 `TaskDetailPanel`）；含 `-Build` 则先按 `<Panel>.json` 重建 prefab 再发布（设计刚同步过、prefab 可能旧时用）。
   - Panel 名拿不准 → 列 `Assets/UI/*/` 下已有面板让用户确认；别瞎发。

2. **发布**：
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\publish-ui.ps1 -Panel <Panel> [-Build] [-Out delivery/<Panel>.unitypackage]
   ```
   脚本会：清本机 Clash 代理（UTO 走回环）→（-Build 时）`BuildUIFromSpec` 重建 → `ExportBindingDescriptor` 走真实 prefab 树产 `binding.json/.md` → `ExportUiPackage` 打包 → 写 `delivery/<Panel>.README.md`。每步按**产物文件刷新**判成败（UTO 用 Write-Host 不进输出流、成功不设退出码，故不能解析 stdout/退出码）。

3. **核对与收尾**：
   - 读 `Assets/UI/<Panel>/<Panel>.binding.md`，向用户报告：可绑元素数、**有几个 `keyAuto`（Figma 名非 ASCII 的兜底键）**、关键交互（Button/InputField）是否齐全。
   - 若关键按钮 `type` 仍是 `Text` 而非 `Button` → 提示「该交互在 Figma 没生成成 Button，需先在 Figma 用稳定 ASCII 名 + 填充框/居中文字，再 `-Build` 重发布」（Phase A 缺口）。
   - 报告交付物：`delivery/<Panel>.unitypackage` 大小 + `README`。

注意：Unity 未开则 UTO 步骤会失败——明确标注未完成，勿伪造。连续 UTO 调用偶发端口冲突，脚本已内置清 node + 重试 3 次。

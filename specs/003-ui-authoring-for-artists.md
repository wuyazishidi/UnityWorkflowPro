# 003 —（已废弃，并入 004）美术用 Figma 编辑 UI

- 状态：**已废弃** —— 目标已由 `specs/004-figma-first-lossless-spec.md` 的 Figma 工具链实现；原 B 路（平面图 PNG + AI 看图估算）已整体移除。
- 关联：`specs/004-figma-first-lossless-spec.md`、`specs/002`（构建引擎）

## 为什么废弃

本规约原计划"先兼容 A=Figma / B=平面图 PNG"两条美术入口。现已定调：

- **Figma 是唯一入口**。美术在 Figma 设计 → `figma-sync.ps1 -Node <id>` 一条命令产出 Prefab（见 CLAUDE.md §4 与 spec 004）。美术不开 Unity、不写 JSON、不碰 Hierarchy。
- **B 路（设计图 PNG → 看图估算 UISpec → 像素 diff 迭代）已移除**：它为"无结构数据的扁平图"而生，Figma 提供精确数据后成为损耗源（详见 004 §1）。

共享契约（一个面板 = 一个目录：`<Panel>.json` + `Icons/` + `<Panel>.prefab` + `<Panel>.spriteatlas`）与组件/命名映射，现统一由 004 的 `scripts/figma_sync.py` 翻译器承载。"美术端入口"目标的当前实现全部见 004；本文件仅作历史留存。

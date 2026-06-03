---
description: 基于模板创建一个新的功能规约(spec)
argument-hint: <功能名，如 player-jump>
---

为功能 "$ARGUMENTS" 创建规约：

1. 读取 `specs/_TEMPLATE.md` 和 `specs/constitution.md`。
2. 在 `specs/` 下查看现有编号，取下一个三位编号 NNN（核心系统 001–099，玩法内容 100+）。
3. 复制模板创建 `specs/NNN-$ARGUMENTS.md`，按功能填写：目标、范围（含明确不做）、接口/数据结构、约束、**可自动化验证的验收标准**。
4. 必须遵守 constitution：规约先行、可测试架构（逻辑抽纯函数）、至少一条可自动化测试的验收项。
5. 不写实现代码。完成后向我展示 spec 摘要，并提示下一步用 `/spec-plan NNN`。

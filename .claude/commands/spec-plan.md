---
description: 为指定编号的 spec 生成实现计划(plan)
argument-hint: <spec 编号，如 002>
---

为 spec 编号 "$ARGUMENTS" 生成实现计划：

1. 读取 `specs/$ARGUMENTS-*.md`、`specs/_PLAN_TEMPLATE.md`、`specs/constitution.md`、根目录 `CLAUDE.md`。
2. 复制计划模板创建 `specs/$ARGUMENTS-*.plan.md`（与 spec 同前缀）。
3. 确定：涉及哪些 asmdef、关键类型职责、可复用的现有代码、可测试性设计（哪些逻辑抽纯函数）、是否引入新依赖（默认否，引入需理由）、边界与回退。
4. 给出验证策略：列出将成为 EditMode 测试的验证点。
5. 不写实现代码。完成后展示计划要点，提示下一步 `/spec-tasks $ARGUMENTS`。

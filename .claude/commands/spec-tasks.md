---
description: 把指定 spec 的计划拆成可独立验证的任务清单
argument-hint: <spec 编号，如 002>
---

为 spec 编号 "$ARGUMENTS" 拆解任务：

1. 读取 `specs/$ARGUMENTS-*.plan.md`、`specs/_TASKS_TEMPLATE.md`、`specs/constitution.md`。
2. 复制任务模板创建 `specs/$ARGUMENTS-*.tasks.md`。
3. 拆成小到可独立实现并验证的任务，顺序合理；每个任务标注验证方式（编译/某测试用例/断言）。
4. 必须包含：补 EditMode 测试用例的任务、以及最后跑 `/dod` 门禁的任务。
5. 不写实现代码。完成后展示任务清单，提示开始实现时严格按 constitution 第三条"完成的定义"逐条达成。

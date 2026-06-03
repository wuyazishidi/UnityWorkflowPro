# NNN — <功能名> · 实现计划（Plan）

> 由 `/spec-plan` 基于对应 spec 生成。计划是 spec 与 tasks 之间的桥：确定技术方案与边界，不写具体步骤清单（那是 tasks）。

## 关联 Spec
specs/NNN-*.md

## 技术方案
- 涉及程序集（asmdef）：<Game / 新建?>
- 关键类型与职责：
- 数据流 / 状态：
- 复用的现有代码：<避免重复造轮子，先列可复用项>

## 架构决策（对照 constitution）
- 可测试性：哪些逻辑抽为纯函数/可注入，如何被 EditMode 测试覆盖
- 依赖：是否引入新包/库（默认不引入；如需，说明理由）
- 性能/平台约束：

## 边界与风险
- 明确不做什么：
- 风险与回退方案：

## 验证策略（喂给 tasks 与 DoD）
- 自动化测试点：<将成为 EditMode 测试用例>
- 编译/控制台门禁：`compile-unity-flow.ps1` + `get_console_error.ps1`

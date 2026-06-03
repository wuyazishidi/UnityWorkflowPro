# 项目宪法（Constitution）

> 仿 [GitHub Spec Kit](https://github.com/github/spec-kit) 的 constitution 概念：本文件是项目的"架构 DNA"，
> 是 AI 在 specify→plan→tasks→implement 全流程中**不可违背**的最高约束。与 `CLAUDE.md` 互补：
> CLAUDE.md 管"怎么干活/命令"，本文件管"必须坚守的原则"。

## 第一条：规约先行（Spec First）
功能级改动必须先有 `specs/NNN-*.md` 规约并确认，再实现。规约是真理来源；代码与规约冲突时，先对齐规约。

## 第二条：可测试架构（Testable by Design）
- 业务/数学逻辑尽量抽成**纯函数或不依赖场景的类**，便于 EditMode 单元测试。
- 需要被测的运行时代码必须归属某个 `asmdef`（如 `Game`），测试程序集引用它。
- 每条功能 spec 的验收标准中，至少有一项是**可自动化执行**的测试或断言。

## 第三条：完成的定义（Definition of Done）
一个改动算"完成"，当且仅当全部满足：
1. 通过编译闸门：`compile-unity-flow.ps1` → `Success, No errors!`
2. 相关 EditMode 测试全部通过（见 `/dod` 命令）
3. 控制台无新增 `error`
4. 对应 spec 的验收清单已逐项勾选
**编译不过或测试不过，不得声称完成。**

## 第四条：最小且可审查的改动
一次只解决一个明确问题；贴合既有代码风格；不做无关重构；不擅自改 `ProjectSettings/`、`Packages/`、`Packages/cn.etetet.yiuimcp/**`。

## 第五条：可预期优先于聪明
宁可让 AI 走固定、可验证的流程，也不要"自由发挥"。流程的确定性（hooks/命令/测试）高于一次性的巧解。

## 第六条：诚实报告
如实报告编译/测试结果。失败就说失败并附输出；跳过就说跳过；不伪造"绿色"。

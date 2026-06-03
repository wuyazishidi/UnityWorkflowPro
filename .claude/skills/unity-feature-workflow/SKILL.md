---
name: unity-feature-workflow
description: 本工程实现新功能/需求时的自治流水线。当用户给出一句 Unity 功能需求（如"玩家能二段跳""加个背包""做个登录界面"），或要求按规范实现/修改游戏功能时使用。自动跑完 澄清→规约→计划→任务→实现→DoD门禁→审核包 全流程，开发者只需在 Unity 中审核结果。
---

# Unity 自治功能流水线

把"一句需求"变成"通过门禁的代码 + 一份给开发者在 Unity 里审核的清单"。
**最高约束**：`specs/constitution.md`；操作规范：`CLAUDE.md`。前提：Unity 编辑器已打开本工程。
除遇"暂停点"外，**自驱动跑完整条线，不要逐步停下等确认**。

## 流程

### 1. 澄清（仅必要时）
仅当需求存在**会实质改变实现**的歧义时，用 AskUserQuestion 提 ≤3 个针对性问题；否则按合理默认推进，把假设记入 spec。

### 2. 规约（Spec）
在 `specs/` 取下一个三位编号，按 `specs/_TEMPLATE.md` 写 `NNN-<功能>.md`：目标、范围（含**明确不做**）、接口/数据结构、约束、**至少一条可自动化验证的验收标准**。

### 3. 计划 + 任务
按 `_PLAN_TEMPLATE.md` / `_TASKS_TEMPLATE.md` 产出 `NNN-*.plan.md` 与 `NNN-*.tasks.md`。任务要小到可独立验证；**必须包含**"补 EditMode 测试"与"跑 DoD"两项。

### 4. 实现（遵守架构约定）
- 业务/数学逻辑抽成**纯类**或**可注入服务**（用 VContainer 注册到 `GameLifetimeScope`，异步用 UniTask）；MonoBehaviour 仅作组合根，不放业务逻辑。
- 需被测的运行时代码归入 `Game` 程序集（`Assets/Scripts/Game.asmdef`）。
- 为可测逻辑在 `Assets/Tests/EditMode/` 写 NUnit 测试。
- 改动最小、贴合既有风格；不动 `Packages/cn.etetet.yiuimcp/**`、`ProjectSettings/`。

### 5. 门禁循环（关键，不可跳过）
跑 `powershell -ExecutionPolicy Bypass -File .\scripts\dod.ps1`（= 编译闸门 + EditMode 测试）。
- **红了就修，循环到全绿**（`Success, No errors!` 且 `result=PASS failed=0`）。未绿不得声称完成。
- 触发编译/测试用 YIUIMCP（见 `CLAUDE.md` 命令速查）。Unity 不可用导致门禁跑不了时，明确标注"未验证"并停下报告。

### 6. 审核包（产出给开发者）
全绿后输出，让人在 Unity 里**快速审核**：
- **改了什么**：新增/修改的文件清单与职责。
- **在 Unity 哪里看**：哪个场景、把哪个组件挂到哪个 GameObject、进入 Play 后按什么键 / 预期看到什么现象。
- **测试结果**：passed/failed 数。
- **已知限制 / 建议后续**。

## 暂停点（必须停下问开发者）
1. 需求有实质歧义（见第 1 步）。
2. 需要引入新第三方包或大改架构。
3. 门禁因环境（Unity 未开等）无法运行。

## 完成标准
DoD 全绿 + spec 验收清单逐项勾选 + 给出审核包。结尾明确：**DONE** 还是 **还有什么待办**。

## 复盘飞轮（让产出越来越符合预期）
若开发者审核后指出偏差，把可泛化的纠正写回 `specs/constitution.md` 或 `CLAUDE.md`（或新增/充实相应 skill），使后续同类任务自动遵守。

> 备注：本 skill 是 `.claude/commands/feature.md`（`/feature` 命令）的自动触发版——命中"实现功能"类需求时自动套用，无需手动输入命令。

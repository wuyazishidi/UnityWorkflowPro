# UnityWorkflowPro 游戏开发工作流

> 本文件说明本工程整合的"AI 严格遵守、结果可预期"的游戏开发工作流：选了什么、为什么选、怎么用、以及后续路线图。
> 配套约束见根目录 `CLAUDE.md`（怎么干活）与 `specs/constitution.md`（不可违背的原则）。

## 1. 设计原则：可预期 = 规约 + 规则 + 硬闸门 + 自动化测试

让 AI 产出可预期，靠的不是堆 MCP 工具，而是把开发约束成一条**线性、可验证、可回滚**的流水线。本工程分五层：

| 层 | 实现 | 作用 |
|----|------|------|
| 工具层 | YIUIMCP（`Packages/cn.etetet.yiuimcp`） | CLI-first 驱动 Unity：编译、读日志、调菜单、Domain Reload 恢复 |
| 规约层 | `specs/`（constitution + spec/plan/tasks 模板） | 规约先行，AI 按契约实现 |
| 规则层 | `CLAUDE.md` | 会话级标准作业指令、命令速查、强制流程 |
| 闸门层 | `.claude/hooks/` + `scripts/dod.ps1` | 改 .cs 提醒走门禁；编译 + 测试硬验证 |
| 测试层 | Unity Test Framework（`Assets/Tests/`） | 自动化单元测试，CLI / CI 可跑 |

核心闭环：**`/spec-new` → `/spec-plan` → `/spec-tasks` → 实现 → `/dod`（编译+测试全绿）**。

## 2. 选型：筛掉了什么，整合了什么，以及理由

调研了 GitHub 上四类"游戏开发工作流"资产，按"成熟、增量、可测试、直接服务可预期目标"筛选：

### ✅ 已整合

**A. Unity Test Framework（自动化测试层）** — Unity 官方
- **为什么**：工作流最大的缺口。整合前只有"编译 + 控制台"门禁，没有逻辑正确性的自动化验证。UTF 是官方方案，支持 EditMode/PlayMode、asmdef 隔离、命令行/CI 运行。
- **怎么做**：运行时代码归入 `Game` 程序集（`Assets/Scripts/Game.asmdef`）；测试在 `Assets/Tests/EditMode/`（`Game.Tests.EditMode` 程序集，Editor-only，引用 nunit）；可测逻辑抽成纯函数（如 `PlayerController.ComputeDisplacement`）。
- **创新点**：`YIUIMCPTestRunner` 把 EditMode 测试接入 YIUIMCP——`ExecuteMenu "YIUIMCP/Run EditMode Tests"` 触发，结果写控制台 `[YIUIMCP-TESTS]` 与 `Logs/EditMode-test-results.txt`，由 `AssertConsoleContains` 断言。**编辑器开着也能跑、不占项目锁**。
- 来源：[Unity Test Framework 文档](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/edit-mode-vs-play-mode-tests.html)

**B. Spec-Driven Development（规约方法论层）** — 仿 [GitHub Spec Kit](https://github.com/github/spec-kit)（官方）
- **为什么**：Spec Kit 把 AI 开发结构化为 constitution→specify→plan→tasks→implement，每步产出喂下一步，正是"可预期"的方法论母体，且原生支持 Claude Code。
- **怎么做**：未引入其 CLI 依赖，而是**适配到本工程**：`specs/constitution.md`（架构 DNA）、`_PLAN_TEMPLATE.md`/`_TASKS_TEMPLATE.md`，以及原生 Claude 斜杠命令 `/spec-new`、`/spec-plan`、`/spec-tasks`、`/dod`（`.claude/commands/`）。轻量、无外部依赖、无 Unity 风险。
- 来源：[Spec Kit](https://github.com/github/spec-kit)、[Spec-Driven Development（GitHub Blog）](https://github.blog/ai-and-ml/generative-ai/spec-driven-development-with-ai-get-started-with-a-new-open-source-toolkit/)

**C. GameCI（CI 就绪件）** — [game-ci](https://github.com/game-ci/unity-actions) 事实标准
- **为什么**：本地门禁 + 云端 PR 把关的双层。提交即测试/构建，缓存 Library 提速。
- **怎么做**：`.github/workflows/unity-ci.yml` 已就绪（test + build）。**当前工程未初始化 git，故为就绪件**：`git init`+推送+配置 `UNITY_LICENSE` 等 secrets 后生效。
- 来源：[GameCI 文档](https://game.ci/docs/github/getting-started/)

### ❌ 评估后未强塞（避免绑架架构，列入路线图）

| 候选 | 量级 | 结论 |
|------|------|------|
| [QFramework](https://github.com/liangxiegame/QFramework) / [ET](https://github.com/egametang/ET) / [UnityGameFramework](https://github.com/EllanJiang/UnityGameFramework) | 整框架 | 侵入性强、改变整体架构。YIUIMCP 已属 ET 生态，要上 ET 应整体决策，不宜半整合 |
| [VContainer](https://github.com/hadashiA/VContainer)（2.9k★）+ [UniTask](https://github.com/Cysharp/UniTask) | 库 | **强烈推荐作为下一步**：DI + 零 GC 异步，让逻辑脱离 MonoBehaviour、天然可测。但属架构决策，应由你显式采纳 |

## 3. 怎么用（日常流程）

```text
/spec-new player-jump        # 1. 生成规约 specs/00X-player-jump.md
/spec-plan 00X               # 2. 生成实现计划
/spec-tasks 00X              # 3. 拆成可验证任务
# ... AI 按任务实现，逻辑尽量抽纯函数，并在 Assets/Tests/EditMode 补测试 ...
/dod                         # 4. 门禁：编译 + EditMode 测试，全绿才算完成
```

手动等价命令（Unity 须打开本工程）：
```powershell
# 编译闸门
powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1' -Force 0 -NoWait 1"
# 只跑测试
powershell -ExecutionPolicy Bypass -File .\scripts\run-editmode-tests.ps1
# 完整 DoD（编译 + 测试）
powershell -ExecutionPolicy Bypass -File .\scripts\dod.ps1
```

## 4. 路线图（建议的下一步，按收益排序）

1. **VContainer + UniTask**：引入 DI 与 UniTask，把游戏逻辑从 MonoBehaviour 解耦为可注入、可测的纯 C#。届时 EditMode 测试覆盖率可大幅提升。
2. **PlayMode 测试**：为涉及场景/物理/输入的功能补 `Assets/Tests/PlayMode/`（asmdef `includePlatforms: []`）。
3. **git + GameCI 启用**：初始化仓库、配置 secrets，让 `unity-ci.yml` 生效，形成本地+云端双门禁。
4. **把 hook 升级为硬阻断**：当前 `.cs` 编辑 hook 是提醒；可加 Stop hook 在 `/dod` 未过时阻断收尾。

## 5. 已验证（本次整合的测试结果）

- 编译闸门：`Result: Success, No errors!`
- EditMode 测试：`[YIUIMCP-TESTS] result=PASS passed=5 failed=0`（经 YIUIMCP 触发并断言）
- 详见 `specs/001-player-movement.md` 验收记录。

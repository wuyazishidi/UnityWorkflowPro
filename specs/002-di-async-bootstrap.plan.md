# 002 — DI 与异步初始化骨架 · 实现计划（Plan）

## 关联 Spec
specs/002-di-async-bootstrap.md

## 技术方案
- 程序集：复用 `Game`（`Assets/Scripts/Game.asmdef`），新增 references `["VContainer", "UniTask"]`。
- 包安装：manifest.json 用 git URL（VContainer 1.18.0 / UniTask 2.5.11）。
- 类型与职责：
  - `Game.Services.IGreetingService` / `GreetingService`：纯逻辑（拼问候语），无 Unity 依赖 → 可直接单测。
  - `Game.GameLifetimeScope : LifetimeScope`：组合根，注册服务 + 入口点。
  - `Game.GameBootstrap : IStartable`：构造注入服务；`Start()` 内 `InitAsync().Forget()`，`InitAsync` 用 `UniTask`。

## 架构决策（对照 constitution 第二条）
- 逻辑（GreetingService）与组合根（LifetimeScope）分离；MonoBehaviour 不含业务逻辑。
- 入口点用 `IStartable`（始终可用）而非 `IAsyncStartable`（依赖 UniTask 集成宏），降低编译耦合。
- 测试只针对纯逻辑 GreetingService，不需要场景/容器。

## 边界与风险
- 不做：把 DI 套到 PlayerController、场景自动布线。
- 风险：UPM 经代理解析 git 包可能慢/失败 → 缓解：git 通道已验证可用，用 git URL 而非 npm；失败则查 PackageCache 与 Editor.log。

## 验证策略
- 单测点：Compose("Alice")→"Hello, Alice!"；空白→"Hello, World!"；" Bob "→"Hello, Bob!"。
- 门禁：`scripts/dod.ps1`（编译 + EditMode 测试）。

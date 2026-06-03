# 002 — 依赖注入与异步初始化骨架（VContainer + UniTask）

- 状态：已确认
- 负责人：路线图第 1 项
- 关联：[[constitution]] 第二条"可测试架构"；docs/WORKFLOW.md 路线图 #1

## 1. 目标（Why）
引入 VContainer（DI）+ UniTask（异步），把游戏逻辑从 MonoBehaviour 解耦为**可注入、可单元测试**的纯 C# 类，提升可测试性，作为后续业务的架构底座与样板。

## 2. 范围（Scope）
- 包含：安装两个包；一个可注入的示例服务 `IGreetingService`/`GreetingService`（纯逻辑）；一个 VContainer `LifetimeScope` 注册服务与入口点；一个 `IStartable` 入口点用 UniTask 演示异步初始化；EditMode 单元测试覆盖纯逻辑。
- **不包含**：把 DI 套用到现有 `PlayerController`（保持示例独立）；网络/存档等真实业务；场景自动布线（由使用者把 `GameLifetimeScope` 挂到 GameObject）。

## 3. 设计与接口（What）
程序集：归入现有 `Game`（`Assets/Scripts/Game.asmdef`），新增对 `VContainer`、`UniTask` 程序集的引用。

```csharp
// Game.Services
public interface IGreetingService { string Compose(string name); }      // 纯逻辑，可脱离 Unity 单测
public class GreetingService : IGreetingService { /* 空名回退 World，去空白 */ }

// Game —— DI 容器
public class GameLifetimeScope : VContainer.Unity.LifetimeScope {
    protected override void Configure(IContainerBuilder b) {
        b.Register<IGreetingService, GreetingService>(Lifetime.Singleton);
        b.RegisterEntryPoint<GameBootstrap>();
    }
}

// Game —— 入口点（IStartable 保证不依赖 UniTask 集成宏；UniTask 直接 fire-and-forget）
public class GameBootstrap : VContainer.Unity.IStartable {
    public GameBootstrap(IGreetingService greeting) { ... }   // 构造注入
    public void Start() { InitAsync().Forget(); }
    private async UniTaskVoid InitAsync() { await UniTask.Yield(); /* 用注入的服务 */ }
}
```

- 依赖（首次显式引入，理由：路线图既定的可测试架构底座）：
  - `jp.hadashikick.vcontainer` 1.18.0（git URL）
  - `com.cysharp.unitask` 2.5.11（git URL）

## 4. 约束（Constraints）
- 业务逻辑放纯类，不放 MonoBehaviour；MonoBehaviour 仅作组合根（LifetimeScope）。
- 不改 `PlayerController`；不动 YIUIMCP 包。
- 命名/目录遵循 CLAUDE.md。

## 5. 验收标准（Acceptance — 必须可验证）
- [x] 两个包成功解析：`PackageCache` 出现 `jp.hadashikick.vcontainer@1.18.0`、`com.cysharp.unitask@2.5.11`，packages-lock 已登记
- [x] 编译通过：`compile-unity-flow.ps1` → `Success, No errors!`（19.85s 含域重建）
- [x] EditMode 测试通过：`GreetingServiceTests` 覆盖正常/空白/去空白，全套 `result=PASS passed=8 failed=0`
- [x] DoD 门禁 `scripts/dod.ps1` → DONE

## 6. 备注
- 入口点用 `IStartable` 而非 `IAsyncStartable`，避免依赖 `VCONTAINER_UNITASK_INTEGRATION` 宏是否自动开启；UniTask 以 `UniTaskVoid + Forget()` 演示异步，二者都被引用即可编译。
- 用 git URL 而非 OpenUPM：本机有系统代理，git 通道已验证可用，比 npm 源更稳。

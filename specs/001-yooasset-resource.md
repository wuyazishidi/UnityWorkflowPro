# 001 — YooAsset 资源系统接入（最小启动样例）

- 状态：已实现（②：装包 + 最小启动样例；运行期加载需一次性收集器配置）
- 负责人：Jinwanpeng
- 关联：`Packages/manifest.json`（com.tuyoogame.yooasset 2.3.19）、`Assets/Scripts/Resource/`

## 1. 目标（Why）

引入 YooAsset 作为本工程的资源加载/热更底座，并提供一个可在 Unity 中审核的最小启动链路：
初始化默认资源包 → 加载一个资源。当前阶段只求“跑通 + 可测 + 可回滚”，不定死资源服务抽象。

## 2. 范围（Scope）

- 包含：
  - 通过 OpenUPM 装入 **YooAsset 2.3.19**（稳定版）。
  - `ResourceModule`：封装 EditorSimulateMode 下的初始化与异步加载。
  - `ResourceBootstrap`：场景内 MonoBehaviour，Play 时自动初始化并可选加载一个样例地址。
  - `ResourceModuleTests`：对可纯函数化的约定做 EditMode 覆盖。
- **不包含**（明确划界）：
  - Offline / Host / Web 运行模式（按真实需求再扩展）。
  - 把 YooAsset 封装成 `IResourceService` 并接入 VContainer DI（接口应由第 2~3 个真实用例倒逼，避免过早抽象）。
  - 热更下载、版本比对、断点续传、加密。
  - 自动化的收集器(Collector)配置与样例资源生成。

## 3. 设计与接口（What）

```csharp
namespace Game.Resource
{
    public static class ResourceModule
    {
        public const string DefaultPackageName = "DefaultPackage";
        public static ResourcePackage Package { get; }
        public static bool IsReady { get; }

        // Initialize → CreatePackage → InitializeAsync → RequestPackageVersionAsync → UpdatePackageManifestAsync
        public static Task<bool> InitializeAsync(string packageName = DefaultPackageName,
                                                 EPlayMode playMode = EPlayMode.EditorSimulateMode);

        public static Task<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object;

        public static string ValidateLocation(string location); // 纯函数，便于单测
    }
}
```

- 依赖：YooAsset 2.3.19（`YooAsset` 程序集，已在 `Game.asmdef` references 登记）。
- 异步：YooAsset 操作经 `op.Task`（`System.Threading.Tasks.Task`）await；Play 模式下由 YooAsset 驱动器逐帧推进。
- 版本差异提醒：2.x 用 `EOperationStatus.Succeed`、`InitializeAsync`、`UpdatePackageManifestAsync`、
  `EditorSimulateModeHelper.SimulateBuild`；3.x 改名为 `Succeeded`/`InitializePackageAsync`/`LoadPackageManifestAsync`/`EditorSimulateBuildInvoker.Build`。本 spec 按 2.3.19。

## 4. 约束（Constraints）

- 命名 / 目录：运行时代码在 `Assets/Scripts/Resource/`，遵循 `CLAUDE.md` 第 3 节。
- 不改 `Packages/cn.etetet.yiuimcp/**`、`ProjectSettings/`。
- EditMode 测试不得触碰 YooAsset 运行时（无驱动循环，异步操作不会推进）。

## 5. 验收标准（Acceptance）

- [ ] 编译通过：`compile-unity-flow.ps1 -Force 0 -NoWait 1` → Success（待 Unity 重新解析 2.3.19 后验证）
- [ ] 控制台无报错：`get_console_error.ps1 -NoWait 1`
- [ ] DoD 绿：`scripts/dod.ps1`（编译 + EditMode 测试，含 `ResourceModuleTests`）
- [ ] 在 Unity 中审核运行期链路（需一次性前置，见“怎么玩”）：新建空物体挂 `ResourceBootstrap`，进入 Play，
      控制台出现 `[ResourceModule] 资源包 'DefaultPackage' 就绪 ...`。

## 6. 怎么玩（运行期审核的一次性前置）

EditorSimulateMode 不是零配置：YooAsset 需要一份收集器(Collector)配置告诉它“哪些资源属于 DefaultPackage”。
这是一步 GUI 操作，无法在无头环境替你完成：

1. 菜单 `YooAsset → AssetBundle Collector`，新建 Package 命名为 **DefaultPackage**，
   新建一个 Group，添加一个 Collector 指向任意有资源的目录（如 `Assets/GameRes`），勾选 Addressable。
2. 在该目录放至少一个资源（如一个 Prefab）。
3. 新建空 GameObject 挂 `ResourceBootstrap`，把 `Sample Address` 填成该资源的 Addressable 地址。
4. 进入 Play，观察控制台初始化与加载日志。

## 7. 备注 / 决策记录

- 选用稳定版 2.3.19 而非 3.0.2-beta：避免 beta API 漂移（曾短暂装过 3.x，已切回）。
- 暂不接 DI：抽象边界（句柄释放、多包、同步/异步）应由真实用例驱动，过早封装易返工。

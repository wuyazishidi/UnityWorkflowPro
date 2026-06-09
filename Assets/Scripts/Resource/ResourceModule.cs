using System;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Game.Resource
{
    /// <summary>
    /// YooAsset 资源服务的最小封装（② 启动样例，针对 YooAsset 2.3.19）。
    /// 当前仅接入编辑器模拟模式 EditorSimulateMode：编辑器下不打 AssetBundle，
    /// 直接按收集器(Collector)配置读源资源，最适合开发期。
    /// Offline / Host / Web 模式留待后续按真实需求接入（见 specs/001-yooasset-resource.md）。
    ///
    /// 典型用法：
    ///   await ResourceModule.InitializeAsync();
    ///   var go = await ResourceModule.LoadAssetAsync&lt;GameObject&gt;("ui/Login");
    /// </summary>
    public static class ResourceModule
    {
        /// <summary>默认资源包名。必须与 YooAsset 收集器里配置的 Package 同名。</summary>
        public const string DefaultPackageName = "DefaultPackage";

        /// <summary>初始化成功的资源包；未初始化为 null。</summary>
        public static ResourcePackage Package { get; private set; }

        /// <summary>资源包是否已就绪。</summary>
        public static bool IsReady => Package != null;

        /// <summary>
        /// 初始化资源系统并准备好资源包。
        /// 流程：Initialize → CreatePackage → InitializeAsync → RequestPackageVersionAsync → UpdatePackageManifestAsync。
        /// </summary>
        /// <returns>全部步骤成功返回 true；任一步失败打印错误并返回 false。</returns>
        public static async Task<bool> InitializeAsync(
            string packageName = DefaultPackageName,
            EPlayMode playMode = EPlayMode.EditorSimulateMode)
        {
            if (!YooAssets.Initialized)
                YooAssets.Initialize();

            var package = YooAssets.TryGetPackage(packageName) ?? YooAssets.CreatePackage(packageName);

            InitializeParameters initParameters = CreateInitializeParameters(packageName, playMode);
            var initOp = package.InitializeAsync(initParameters);
            await initOp.Task;
            if (initOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[ResourceModule] 初始化资源包失败: {initOp.Error}");
                return false;
            }

            var versionOp = package.RequestPackageVersionAsync();
            await versionOp.Task;
            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[ResourceModule] 请求资源版本失败: {versionOp.Error}");
                return false;
            }

            var manifestOp = package.UpdatePackageManifestAsync(versionOp.PackageVersion);
            await manifestOp.Task;
            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[ResourceModule] 更新资源清单失败: {manifestOp.Error}");
                return false;
            }

            Package = package;
            Debug.Log($"[ResourceModule] 资源包 '{packageName}' 就绪 (version={versionOp.PackageVersion}, mode={playMode})。");
            return true;
        }

        /// <summary>
        /// 按地址(Addressable location)异步加载一个资源。需先 InitializeAsync 成功。
        /// </summary>
        public static async Task<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            ValidateLocation(location);
            if (!IsReady)
                throw new InvalidOperationException("[ResourceModule] 资源包尚未初始化，先 await InitializeAsync()。");

            var handle = Package.LoadAssetAsync<T>(location);
            await handle.Task;
            if (handle.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[ResourceModule] 加载资源 '{location}' 失败 (status={handle.Status})。");
                return null;
            }
            return handle.AssetObject as T;
        }

        /// <summary>
        /// 纯函数：校验资源地址并返回去除首尾空白后的地址。空或纯空白抛 ArgumentException。
        /// 抽成 public 静态方法以便 EditMode 单测覆盖（不依赖 Unity 运行时）。
        /// </summary>
        public static string ValidateLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("资源地址不能为空。", nameof(location));
            return location.Trim();
        }

        private static InitializeParameters CreateInitializeParameters(string packageName, EPlayMode playMode)
        {
            switch (playMode)
            {
                case EPlayMode.EditorSimulateMode:
#if UNITY_EDITOR
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
                    var initParameters = new EditorSimulateModeParameters();
                    initParameters.EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
                    return initParameters;
#else
                    throw new NotSupportedException("EditorSimulateMode 仅在 Unity 编辑器中可用。");
#endif
                default:
                    throw new NotSupportedException(
                        $"② 样例当前仅接入 EditorSimulateMode，尚未支持 {playMode}（按需求再扩展）。");
            }
        }
    }
}

using UnityEngine;
using YooAsset;

namespace Game.Resource
{
    /// <summary>
    /// 把 ResourceModule 挂到场景里用于审核 ② 样例：进入 Play 后自动初始化默认资源包
    /// (EditorSimulateMode)，若填了 SampleAddress 再尝试加载一次并打印结果。
    /// </summary>
    public sealed class ResourceBootstrap : MonoBehaviour
    {
        [Tooltip("可选：一个已被 YooAsset 收集器收集的资源地址(Addressable)。留空则只做初始化。")]
        [SerializeField] private string _sampleAddress = "";

        private async void Start()
        {
            bool ok = await ResourceModule.InitializeAsync(
                ResourceModule.DefaultPackageName, EPlayMode.EditorSimulateMode);

            if (!ok)
            {
                Debug.LogError("[ResourceBootstrap] 资源初始化失败：请确认已在 YooAsset 收集器窗口配置了名为 "
                               + $"'{ResourceModule.DefaultPackageName}' 的 Package 并指向有资源的目录。");
                return;
            }

            if (!string.IsNullOrWhiteSpace(_sampleAddress))
            {
                var asset = await ResourceModule.LoadAssetAsync<Object>(_sampleAddress);
                Debug.Log(asset != null
                    ? $"[ResourceBootstrap] 样例加载成功: {_sampleAddress} -> {asset.name}"
                    : $"[ResourceBootstrap] 样例加载失败: {_sampleAddress}");
            }
        }
    }
}

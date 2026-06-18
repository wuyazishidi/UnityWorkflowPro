using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 构建期临时标记：标识一个 GameObject 是「列表项模板」（命名重复 item 抽出的第 1 个）。
    /// Editor 侧（UIBuilder）据此把它 SaveAsPrefabAsset 成独立 prefab，再从主树移除——
    /// 所以这个组件不会出现在最终交付的 prefab 里。运行时 builder 不能直接抽 prefab（Editor API），故用标记中转。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIItemTemplateMarker : MonoBehaviour
    {
        /// <summary>item prefab 的文件名（= item 节点名，如 TaskItem_Btn）。</summary>
        public string itemName;
    }
}

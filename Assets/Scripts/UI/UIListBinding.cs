using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 持久组件：标识一个「模板列表」父容器。item 已被抽成独立 prefab、从主 panel 去掉，
    /// 本容器只保留 LayoutGroup+ContentSizeFitter。消费方（YC-Ego）运行时据此：
    ///   1. 加载 itemPrefab 对应的 prefab；
    ///   2. 按数据条数 Instantiate 到本容器（transform）下；
    ///   3. LayoutGroup 自动按方向/间距排序、ContentSizeFitter 自动撑开。
    /// 这个组件随主 panel prefab 一起交付，binding.json 也会描述它。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIListBinding : MonoBehaviour
    {
        /// <summary>item 模板 prefab 名（= 同目录下 &lt;itemPrefab&gt;.prefab）。</summary>
        public string itemPrefab;
        /// <summary>排序方向：true=竖向(VerticalLayoutGroup)，false=横向。</summary>
        public bool vertical = true;
        /// <summary>item 间距（像素）。</summary>
        public float spacing;
    }
}

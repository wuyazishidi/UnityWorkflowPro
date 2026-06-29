using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 全选开关：挂在"全选" Toggle 上。勾选 → 列表里所有 Item 的 Toggle 一起勾选；
    /// 取消 → 全部取消。Item 由消费方(YC-Ego)运行时实例化进 ScrollList Content，故联动在
    /// onValueChanged 触发时即时扫描 content 下的所有 Toggle，不依赖实例化时机。
    /// 自身在 Awake 接线，prefab 只需序列化 master + itemsRoot 两个引用（builder 在构建期写入），
    /// 无需持久化 UnityEvent（builder 是运行期程序集，接不了持久监听）。
    /// </summary>
    [DisallowMultipleComponent]
    public class SelectAllToggle : MonoBehaviour
    {
        [Tooltip("主控全选 Toggle；为空时取本物体上的 Toggle")]
        public Toggle master;
        [Tooltip("Item 所在容器(ScrollList 的 Content)；builder 在构建期写入。为空时运行期回退到面板内首个 ScrollRect.content")]
        public RectTransform itemsRoot;

        // 防止批量设置子项时的潜在重入（子项若被 YC-Ego 反向接回主控，不会形成回环）。
        private bool _suppress;

        private void Awake()
        {
            if (master == null) master = GetComponent<Toggle>();
            if (master != null) master.onValueChanged.AddListener(OnMasterChanged);
        }

        private void OnDestroy()
        {
            if (master != null) master.onValueChanged.RemoveListener(OnMasterChanged);
        }

        private void OnMasterChanged(bool on)
        {
            if (_suppress) return;
            var root = ResolveItemsRoot();
            if (root == null) return;
            var items = root.GetComponentsInChildren<Toggle>(true);
            _suppress = true;
            foreach (var t in items)
            {
                if (t == null || t == master) continue;
                t.isOn = on;
            }
            _suppress = false;
        }

        private RectTransform ResolveItemsRoot()
        {
            if (itemsRoot != null) return itemsRoot;
            // 回退：在面板内找首个 ScrollRect 的 content（builder 正常会直接写好 itemsRoot）。
            var sr = GetComponentInParent<ScrollRect>();
            if (sr == null && transform.root != null) sr = transform.root.GetComponentInChildren<ScrollRect>(true);
            if (sr != null) itemsRoot = sr.content;
            return itemsRoot;
        }
    }
}

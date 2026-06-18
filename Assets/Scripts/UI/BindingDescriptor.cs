using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    /// <summary>可绑组件类型（与 binding.json 的 type 字段一一对应）。</summary>
    public enum BindKind { Button, InputField, Toggle, Slider, Dropdown, Scrollbar, ScrollList, List, Text }

    /// <summary>binding.json 的一个可绑元素。</summary>
    public sealed class BindElement
    {
        public string key;       // 稳定 ASCII 键（YC-Ego 绑这个）；含类型后缀(如 returnBtn) → 跨类型不撞键
        public string path;      // prefab 内 transform 路径（从 panel 根起，不含根）
        public BindKind kind;    // 组件类型
        public string text;      // 当前文案（best-effort，供人核对；可空）
        public bool keyAuto;     // true=key 是 <type><序号> 兜底，建议在 Figma 用稳定 ASCII 名
        public bool nameTypeMismatch; // 名称类型后缀(_Btn/_InputField/_Dropdown)与实际组件不符 → 暴露「交互元素未生成为对应组件」
        public string itemPrefab; // kind=List 时：item 模板 prefab 名（同目录 <itemPrefab>.prefab），消费方据此实例化
    }

    /// <summary>一个面板的绑定描述（不含依赖/尺寸——那些由 Editor 导出器据 AssetDatabase 补全）。</summary>
    public sealed class BindDoc
    {
        public string panel;
        public List<BindElement> elements = new List<BindElement>();
    }

    /// <summary>
    /// 走「已构建 prefab 的真实 GameObject 树」→ 可绑元素表。纯逻辑：只读 Unity 组件，
    /// 不触 AssetDatabase / 文件 IO / Editor API，可在 EditMode 直接对内存树断言。
    /// </summary>
    public static class BindingDescriptorBuilder
    {
        // 交互组件视为「自包含叶子」——命中即收并停止下钻（其内部 Label/Placeholder/Item 等是 builder 构造物，不入表）。
        // 据 UIHierarchyBuilder：Button/TMP_InputField/TMP_Dropdown/Toggle/Slider/Scrollbar/ScrollRect(=ScrollList)。
        public static BindDoc Build(GameObject root, string panel)
        {
            var doc = new BindDoc { panel = panel };
            if (root == null) return doc;
            var autoCounts = new Dictionary<BindKind, int>();
            foreach (Transform child in root.transform)
                Walk(child, "", doc, autoCounts);
            return doc;
        }

        static void Walk(Transform node, string parentPath, BindDoc doc, Dictionary<BindKind, int> autoCounts)
        {
            if (node == null) return;
            string path = string.IsNullOrEmpty(parentPath) ? node.name : parentPath + "/" + node.name;

            // 模板列表父容器：item 已抽成独立 prefab，本容器自包含（不下钻）。记 itemPrefab 供消费方实例化。
            var listBind = node.GetComponent<UIListBinding>();
            if (listBind != null)
            {
                // 有 ScrollRect=可滚动列表(ScrollList)，否则普通模板列表(List)；两者都带 itemPrefab。
                var lk = node.GetComponent<ScrollRect>() != null ? BindKind.ScrollList : BindKind.List;
                var le = MakeElement(node.gameObject, lk, path, autoCounts);
                le.itemPrefab = listBind.itemPrefab;
                doc.elements.Add(le);
                return;
            }

            if (TryInteractive(node.gameObject, out var kind))
            {
                doc.elements.Add(MakeElement(node.gameObject, kind, path, autoCounts));
                return; // 自包含叶子：不下钻
            }

            // 非交互节点：独立动态文本（不在任何交互组件内，因为交互节点不下钻）→ 计为 Text
            if (node.GetComponent<TextMeshProUGUI>() != null)
                doc.elements.Add(MakeElement(node.gameObject, BindKind.Text, path, autoCounts));

            foreach (Transform child in node)
                Walk(child, path, doc, autoCounts);
        }

        /// <summary>按优先级识别交互组件；命中返回 true + kind。</summary>
        public static bool TryInteractive(GameObject go, out BindKind kind)
        {
            if (go.GetComponent<Button>() != null) { kind = BindKind.Button; return true; }
            if (go.GetComponent<TMP_InputField>() != null) { kind = BindKind.InputField; return true; }
            if (go.GetComponent<TMP_Dropdown>() != null) { kind = BindKind.Dropdown; return true; }
            if (go.GetComponent<Toggle>() != null) { kind = BindKind.Toggle; return true; }
            if (go.GetComponent<Slider>() != null) { kind = BindKind.Slider; return true; }
            if (go.GetComponent<Scrollbar>() != null) { kind = BindKind.Scrollbar; return true; }
            if (go.GetComponent<ScrollRect>() != null) { kind = BindKind.ScrollList; return true; }
            kind = BindKind.Text; return false;
        }

        static BindElement MakeElement(GameObject go, BindKind kind, string path, Dictionary<BindKind, int> autoCounts)
        {
            int next = autoCounts.TryGetValue(kind, out var n) ? n + 1 : 1;
            string key = ToKey(go.name, kind, next, out bool keyAuto);
            if (keyAuto) autoCounts[kind] = next;
            bool mismatch = TrySuffixKind(go.name, out var sufKind) && sufKind != kind;
            return new BindElement
            {
                key = key, path = path, kind = kind, text = ExtractText(go, kind),
                keyAuto = keyAuto, nameTypeMismatch = mismatch
            };
        }

        // 命名类型后缀 → 组件类型（与翻译器 figma_sync.py 的 _apply_type_suffixes 对齐）。
        // Image 不入此表（非可绑元素，不进描述）。
        static readonly Dictionary<string, BindKind> NameSuffixKind = new Dictionary<string, BindKind>
        {
            { "_Btn", BindKind.Button },
            { "_InputField", BindKind.InputField },
            { "_Dropdown", BindKind.Dropdown },
            { "_Text", BindKind.Text },
        };

        /// <summary>名字带已知类型后缀(_Btn/_InputField/_Dropdown/_Text) → 返回其期望 kind。</summary>
        public static bool TrySuffixKind(string goName, out BindKind kind)
        {
            kind = BindKind.Text;
            if (string.IsNullOrEmpty(goName)) return false;
            foreach (var kv in NameSuffixKind)
                if (goName.EndsWith(kv.Key)) { kind = kv.Value; return true; }
            return false;
        }

        /// <summary>
        /// key 规则：GameObject 名（=Figma 名）若为 ASCII 标识符（字母开头，仅 [A-Za-z0-9 _]）→ 驼峰化；
        /// 否则兜底 <typeLower><autoIndex>（如 button1）并置 keyAuto=true。
        /// </summary>
        public static string ToKey(string goName, BindKind kind, int autoIndex, out bool keyAuto)
        {
            if (IsAsciiIdentifier(goName))
            {
                keyAuto = false;
                return Camelize(goName);
            }
            keyAuto = true;
            return kind.ToString().ToLowerInvariant() + autoIndex;
        }

        static bool IsAsciiIdentifier(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            char c0 = s[0];
            if (!((c0 >= 'A' && c0 <= 'Z') || (c0 >= 'a' && c0 <= 'z'))) return false;
            foreach (char c in s)
            {
                bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == ' ' || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        /// <summary>"BtnStart"/"Btn Start"/"btn_start" → "btnStart"。去分隔符并保持驼峰，首字母小写。</summary>
        static string Camelize(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool upNext = false;
            foreach (char c in s.Trim())
            {
                if (c == ' ' || c == '_') { upNext = sb.Length > 0; continue; }
                sb.Append(upNext ? char.ToUpperInvariant(c) : c);
                upNext = false;
            }
            if (sb.Length > 0) sb[0] = char.ToLowerInvariant(sb[0]);
            return sb.ToString();
        }

        /// <summary>best-effort 取当前文案：Button/Dropdown 取内部首个 TMP；InputField 取 Placeholder；Text 取自身。</summary>
        static string ExtractText(GameObject go, BindKind kind)
        {
            switch (kind)
            {
                case BindKind.Text:
                    return go.GetComponent<TextMeshProUGUI>()?.text;
                case BindKind.InputField:
                    var ph = go.transform.Find("Text Area/Placeholder");
                    return ph != null ? ph.GetComponent<TextMeshProUGUI>()?.text : null;
                case BindKind.Button:
                case BindKind.Dropdown:
                    var t = go.GetComponentInChildren<TextMeshProUGUI>(true);
                    return t != null ? t.text : null;
                default:
                    return null;
            }
        }
    }
}

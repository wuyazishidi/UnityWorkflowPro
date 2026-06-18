using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;

namespace Game.UI.EditorTools
{
    public sealed class BindingExportResult
    {
        public bool Ok;
        public string JsonPath;
        public string MdPath;
        public int ElementCount;
        public int AutoKeyCount;
        public List<string> Errors = new List<string>();
    }

    /// <summary>
    /// 载入已构建 prefab → 走真实树（BindingDescriptorBuilder）→ 据 AssetDatabase 补依赖/尺寸 →
    /// 手写 &lt;Panel&gt;.binding.json + &lt;Panel&gt;.binding.md，落在 prefab 同目录。
    /// 这是与消费方（YC-Ego）解耦的唯一接口产物。
    /// </summary>
    public static class BindingDescriptorExporter
    {
        public static BindingExportResult Export(string prefabPath)
        {
            var r = new BindingExportResult();
            if (string.IsNullOrWhiteSpace(prefabPath) || !prefabPath.EndsWith(".prefab"))
            {
                r.Errors.Add($"路径必须以 .prefab 结尾: {prefabPath}");
                return r;
            }
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (root == null)
            {
                r.Errors.Add($"载入 prefab 失败: {prefabPath}");
                return r;
            }

            string panel = Path.GetFileNameWithoutExtension(prefabPath);
            string dir = Path.GetDirectoryName(prefabPath).Replace('\\', '/');

            var doc = BindingDescriptorBuilder.Build(root, panel);
            var deps = ResolveDependencies(root, dir, panel);
            var size = ResolveSize(root);

            string json = ToJson(doc, panel, prefabPath, size, deps);
            string md = ToMarkdown(doc, panel, prefabPath, deps);

            r.JsonPath = $"{dir}/{panel}.binding.json";
            r.MdPath = $"{dir}/{panel}.binding.md";
            File.WriteAllText(r.JsonPath, json, new UTF8Encoding(false));
            File.WriteAllText(r.MdPath, md, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(r.JsonPath);
            AssetDatabase.ImportAsset(r.MdPath);

            r.Ok = true;
            r.ElementCount = doc.elements.Count;
            r.AutoKeyCount = doc.elements.Count(e => e.keyAuto);
            return r;
        }

        // ---------- 依赖 / 尺寸 ----------

        sealed class Deps
        {
            public string font;
            public List<string> commonSprites = new List<string>();
            public string atlas;
            public bool needsTmpEssentials = true;
        }

        static Deps ResolveDependencies(GameObject root, string dir, string panel)
        {
            var d = new Deps();

            // 字体：取用得最多的 TMP 字体名
            var fontCounts = new Dictionary<string, int>();
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                if (t.font != null)
                    fontCounts[t.font.name] = fontCounts.TryGetValue(t.font.name, out var n) ? n + 1 : 1;
            if (fontCounts.Count > 0)
                d.font = fontCounts.OrderByDescending(kv => kv.Value).First().Key;

            // 公共精灵：round* / ring*（Assets/UI/Common 下的圆角/描边 9-slice）
            var sprites = new HashSet<string>();
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                var sp = img.sprite;
                if (sp == null) continue;
                if (sp.name.StartsWith("round") || sp.name.StartsWith("ring"))
                    sprites.Add(sp.name);
            }
            d.commonSprites = sprites.OrderBy(s => s).ToList();

            // 图集：Assets/UI/<Panel>/<Panel>.spriteatlas 存在则填
            string atlas = $"{dir}/{panel}.spriteatlas";
            if (File.Exists(atlas)) d.atlas = atlas;

            return d;
        }

        static Vector2 ResolveSize(GameObject root)
        {
            var rt = root.GetComponent<RectTransform>();
            if (rt == null) return Vector2.zero;
            var s = rt.rect.size;
            if (s.x < 1f && s.y < 1f) s = rt.sizeDelta;
            return s;
        }

        // ---------- 序列化（手写，稳定字段序、人可 diff） ----------

        static string ToJson(BindDoc doc, string panel, string prefabPath, Vector2 size, Deps deps)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"schemaVersion\": \"1.0\",\n");
            sb.Append($"  \"panel\": {Str(panel)},\n");
            sb.Append($"  \"prefab\": {Str(prefabPath)},\n");
            sb.Append($"  \"size\": {{ \"w\": {Mathf.RoundToInt(size.x)}, \"h\": {Mathf.RoundToInt(size.y)} }},\n");

            sb.Append("  \"dependencies\": {\n");
            sb.Append($"    \"font\": {Str(deps.font)},\n");
            sb.Append($"    \"commonSprites\": [{string.Join(", ", deps.commonSprites.Select(Str))}],\n");
            sb.Append($"    \"atlas\": {Str(deps.atlas)},\n");
            sb.Append($"    \"needsTmpEssentials\": {(deps.needsTmpEssentials ? "true" : "false")}\n");
            sb.Append("  },\n");

            sb.Append("  \"elements\": [");
            for (int i = 0; i < doc.elements.Count; i++)
            {
                var e = doc.elements[i];
                sb.Append(i == 0 ? "\n" : ",\n");
                sb.Append("    { ");
                sb.Append($"\"key\": {Str(e.key)}, ");
                sb.Append($"\"path\": {Str(e.path)}, ");
                sb.Append($"\"type\": {Str(e.kind.ToString())}, ");
                sb.Append($"\"text\": {Str(e.text)}");
                if (e.keyAuto) sb.Append(", \"keyAuto\": true");
                if (e.nameTypeMismatch) sb.Append(", \"nameTypeMismatch\": true");
                if (!string.IsNullOrEmpty(e.itemPrefab)) sb.Append($", \"itemPrefab\": {Str(e.itemPrefab)}");
                sb.Append(" }");
            }
            sb.Append(doc.elements.Count > 0 ? "\n  ]\n" : "]\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        static string ToMarkdown(BindDoc doc, string panel, string prefabPath, Deps deps)
        {
            var sb = new StringBuilder();
            sb.Append($"# {panel} — UI 绑定描述\n\n");
            sb.Append($"- prefab：`{prefabPath}`\n");
            sb.Append($"- 依赖：字体 `{deps.font}`；公共精灵 {(deps.commonSprites.Count > 0 ? "`" + string.Join("`, `", deps.commonSprites) + "`" : "（无）")}；");
            sb.Append($"图集 {(string.IsNullOrEmpty(deps.atlas) ? "（无）" : "`" + deps.atlas + "`")}；needsTmpEssentials = `{deps.needsTmpEssentials.ToString().ToLowerInvariant()}`\n\n");
            sb.Append("YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。\n\n");
            sb.Append("| key | 类型 | 文案 | path | 备注 |\n");
            sb.Append("|-----|------|------|------|------|\n");
            foreach (var e in doc.elements)
            {
                var notes = new List<string>();
                if (e.kind == BindKind.List && !string.IsNullOrEmpty(e.itemPrefab))
                    notes.Add($"📋 列表：运行时实例化 `{e.itemPrefab}.prefab` 到本容器（见末尾说明）");
                if (e.nameTypeMismatch) notes.Add($"❗ 名称后缀与实际组件({e.kind})不符——交互元素未生成为对应组件");
                if (e.keyAuto) notes.Add("⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名");
                sb.Append($"| `{e.key}` | {e.kind} | {Md(e.text)} | `{e.path}` | {string.Join("；", notes)} |\n");
            }
            int autoN = doc.elements.Count(e => e.keyAuto);
            int mismN = doc.elements.Count(e => e.nameTypeMismatch);
            sb.Append("\n> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。\n");
            if (autoN > 0)
                sb.Append($">\n> 有 {autoN} 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。\n");
            if (mismN > 0)
                sb.Append($">\n> 有 {mismN} 个元素名称后缀与实际组件不符（交互元素未生成为对应组件，需在 Figma/翻译器侧修可绑性）。\n");
            var lists = doc.elements.Where(e => e.kind == BindKind.List).ToList();
            if (lists.Count > 0)
            {
                sb.Append("\n## 列表（type=List）使用方法（给 YC-Ego）\n");
                sb.Append("命名重复的列表项已抽成**独立 prefab**；主 panel 的父容器只剩 LayoutGroup（item 已去掉）。运行时这样填充：\n\n");
                sb.Append("```csharp\n");
                sb.Append("// listEl = binding.json 里 type==\"List\" 的元素\n");
                sb.Append("var parent = root.transform.Find(listEl.path);                    // 列表父容器(已带 LayoutGroup)\n");
                sb.Append("var itemPrefab = Resources.Load<GameObject>(\"UI/<Panel>/\" + listEl.itemPrefab); // 同目录 <itemPrefab>.prefab\n");
                sb.Append("foreach (var data in dataList) {\n");
                sb.Append("    var go = Object.Instantiate(itemPrefab, parent);              // LayoutGroup 自动排序、ContentSizeFitter 自动撑开\n");
                sb.Append("    // 用 data 填充 go 内部子元素（FindDeep 取其中的文本/按钮）\n");
                sb.Append("}\n");
                sb.Append("```\n");
                foreach (var e in lists)
                    sb.Append($"- `{e.key}`（path `{e.path}`）→ item prefab `{e.itemPrefab}.prefab`\n");
                sb.Append("\n> 父容器带 `UIListBinding` 组件（itemPrefab / vertical / spacing），运行时也可直接读它拿配置，无需解析 binding.json。\n");
            }
            return sb.ToString();
        }

        static string Str(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        static string Md(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|").Replace("\n", " ");
    }
}

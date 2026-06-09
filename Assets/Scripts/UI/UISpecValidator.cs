using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>纯函数：校验已反序列化的 UISpec，返回错误列表（空 = 通过）。不依赖 Unity 资源/场景。</summary>
    public static class UISpecValidator
    {
        public static readonly HashSet<string> ValidTypes =
            new HashSet<string> { "Container", "Image", "RawImage", "Text", "Button" };

        public static readonly HashSet<string> ValidImageTypes =
            new HashSet<string> { "Simple", "Sliced", "Tiled", "Filled" };

        public static List<string> Validate(UISpec spec)
        {
            var errors = new List<string>();
            if (spec == null) { errors.Add("spec 为 null"); return errors; }

            if (spec.schemaVersion != 1)
                errors.Add($"不支持的 schemaVersion={spec.schemaVersion}（仅支持 1）");
            if (spec.referenceWidth <= 0 || spec.referenceHeight <= 0)
                errors.Add($"参考分辨率非法: {spec.referenceWidth}x{spec.referenceHeight}");
            if (spec.root == null)
            {
                errors.Add("root 为空");
                return errors;
            }

            ValidateNode(spec.root, "root", errors);
            return errors;
        }

        private static void ValidateNode(UINode node, string path, List<string> errors)
        {
            if (node == null) { errors.Add($"{path}: 节点为 null"); return; }

            if (string.IsNullOrWhiteSpace(node.name))
                errors.Add($"{path}: name 不能为空");

            if (string.IsNullOrWhiteSpace(node.type) || !ValidTypes.Contains(node.type))
                errors.Add($"{path}({node.name}): 非法 type='{node.type}'");

            if (node.rect == null)
                errors.Add($"{path}({node.name}): rect 缺失");
            else if (node.rect.w < 0 || node.rect.h < 0)
                errors.Add($"{path}({node.name}): rect 宽高不能为负 ({node.rect.w}x{node.rect.h})");

            if (!string.IsNullOrWhiteSpace(node.color) && !ColorUtil.TryParseHex(node.color, out _))
                errors.Add($"{path}({node.name}): 非法颜色 '{node.color}'");

            if (!string.IsNullOrWhiteSpace(node.imageType) && !ValidImageTypes.Contains(node.imageType))
                errors.Add($"{path}({node.name}): 非法 imageType='{node.imageType}'");

            if (node.type == "Text" && (node.text == null || string.IsNullOrEmpty(node.text.content)))
                errors.Add($"{path}({node.name}): type=Text 必须有 text.content");

            if (node.text != null && !string.IsNullOrWhiteSpace(node.text.alignment)
                && !AlignmentMap.TryGet(node.text.alignment, out _))
                errors.Add($"{path}({node.name}): 非法 alignment='{node.text.alignment}'");

            // 兄弟名唯一（同层 SetSiblingIndex/查找不歧义）
            if (node.children != null && node.children.Count > 0)
            {
                var seen = new HashSet<string>();
                for (int i = 0; i < node.children.Count; i++)
                {
                    var child = node.children[i];
                    var childPath = $"{path}/{(child?.name ?? "?")}";
                    if (child != null && !string.IsNullOrWhiteSpace(child.name) && !seen.Add(child.name))
                        errors.Add($"{path}({node.name}): 兄弟节点重名 '{child.name}'");
                    ValidateNode(child, childPath, errors);
                }
            }
        }
    }
}

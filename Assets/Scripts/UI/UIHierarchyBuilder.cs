using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    /// <summary>
    /// 资源解析抽象：把"路径 → Sprite/字体"与建树逻辑解耦，便于 EditMode 测试（传 null 即不解析）。
    /// 编辑器导入器用 AssetDatabase 实现；测试可传 null 或桩。
    /// </summary>
    public interface IUIAssetResolver
    {
        Sprite ResolveSprite(string path);
        TMP_FontAsset ResolveFont(string path);
    }

    /// <summary>
    /// 资源无关的建树器：UISpec → GameObject 树（含 Image/TMP/Button 组件与 RectTransform 布局）。
    /// 不触碰 AssetDatabase/PrefabUtility，可在 EditMode 直接运行并断言。
    /// 返回的根节点未挂到任何 Canvas；调用方负责 SetParent 到 Canvas 或存为 prefab。
    /// </summary>
    public static class UIHierarchyBuilder
    {
        public static GameObject Build(UISpec spec, IUIAssetResolver resolver)
        {
            if (spec == null || spec.root == null) return null;
            return BuildNode(spec.root, 0f, 0f, resolver);
        }

        private static GameObject BuildNode(UINode node, float parentAbsX, float parentAbsY, IUIAssetResolver resolver)
        {
            var go = new GameObject(node.name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;

            // 布局
            RectLayout layout;
            if (node.anchorPreset == "stretch-full")
                layout = UISpecMath.StretchFull();
            else if (node.anchorPreset == "center")
                layout = UISpecMath.CenterFixed(node.rect.w, node.rect.h);
            else
                layout = UISpecMath.TopLeft(node.rect.x, node.rect.y, node.rect.w, node.rect.h, parentAbsX, parentAbsY);
            UISpecMath.Apply(rt, layout);

            // 组件
            switch (node.type)
            {
                case "Image": BuildImage(go, node, resolver); break;
                case "RawImage": BuildRawImage(go, node, resolver); break;
                case "Text": BuildText(go, node, resolver); break;
                case "Button": BuildButton(go, node, resolver); break;
                case "Container":
                default: break;
            }

            // 子节点（按数组序追加 → 兄弟序 = 绘制层级，index 0 在底）
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    if (child == null) continue;
                    var childGo = BuildNode(child, node.rect.x, node.rect.y, resolver);
                    childGo.transform.SetParent(go.transform, false);
                }
            }

            // v2 效果（spec 004 Phase 2）：渐变 / 描边 / 整体不透明度
            ApplyV2Effects(go, node, resolver);

            return go;
        }

        /// <summary>渐变(顶点色)、描边(镂空环子物体，叠最上)、整体不透明度(CanvasGroup)。</summary>
        private static void ApplyV2Effects(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            // 渐变：节点本身有图形(Image)时挂顶点色修饰器
            if (node.gradient != null && node.gradient.stops != null && node.gradient.stops.Count >= 2
                && go.GetComponent<Graphic>() != null)
            {
                go.AddComponent<UIVertexGradient>().Configure(node.gradient);
            }

            // 描边：镂空环 9-slice 子物体，染描边色，置于最上层（子物体晚于父图形绘制）
            if (node.stroke != null && !string.IsNullOrWhiteSpace(node.stroke.sprite))
            {
                var sgo = new GameObject(node.name + "_Stroke", typeof(RectTransform));
                var srt = (RectTransform)sgo.transform;
                UISpecMath.Apply(srt, UISpecMath.StretchFull());
                sgo.transform.SetParent(go.transform, false);
                var simg = sgo.AddComponent<Image>();
                simg.color = ColorUtil.ParseHexOr(node.stroke.color, Color.white);
                simg.raycastTarget = false;
                if (resolver != null) simg.sprite = resolver.ResolveSprite(node.stroke.sprite);
                simg.type = Image.Type.Sliced;
            }

            // 整体不透明度
            if (node.opacity < 0.999f)
            {
                var cg = go.GetComponent<CanvasGroup>();
                if (cg == null) cg = go.AddComponent<CanvasGroup>();
                cg.alpha = Mathf.Clamp01(node.opacity);
            }
        }

        private static void BuildImage(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var img = go.AddComponent<Image>();
            img.color = ColorUtil.ParseHexOr(node.color, Color.white);
            img.raycastTarget = node.raycastTarget;
            if (resolver != null && !string.IsNullOrWhiteSpace(node.sprite))
                img.sprite = resolver.ResolveSprite(node.sprite);
            img.type = ParseImageType(node.imageType);
        }

        private static void BuildRawImage(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var raw = go.AddComponent<RawImage>();
            raw.color = ColorUtil.ParseHexOr(node.color, Color.white);
            raw.raycastTarget = node.raycastTarget;
            if (resolver != null && !string.IsNullOrWhiteSpace(node.sprite))
            {
                var sp = resolver.ResolveSprite(node.sprite);
                if (sp != null) raw.texture = sp.texture;
            }
        }

        private static void BuildText(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            ApplyText(tmp, node.text, resolver);
            tmp.raycastTarget = node.raycastTarget;
        }

        private static void BuildButton(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var img = go.AddComponent<Image>();
            img.color = ColorUtil.ParseHexOr(node.color, Color.white);
            img.raycastTarget = true;
            if (resolver != null && !string.IsNullOrWhiteSpace(node.sprite))
                img.sprite = resolver.ResolveSprite(node.sprite);
            img.type = ParseImageType(node.imageType);

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            if (node.text != null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                var lrt = (RectTransform)labelGo.transform;
                UISpecMath.Apply(lrt, UISpecMath.StretchFull()); // 文字填满按钮
                labelGo.transform.SetParent(go.transform, false);
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                ApplyText(tmp, node.text, resolver);
                tmp.raycastTarget = false;
            }
        }

        private static void ApplyText(TextMeshProUGUI tmp, UIText text, IUIAssetResolver resolver)
        {
            if (text == null) return;
            tmp.text = text.content ?? "";
            tmp.fontSize = text.fontSize;
            tmp.color = ColorUtil.ParseHexOr(text.color, Color.white);
            tmp.alignment = AlignmentMap.GetOr(text.alignment);
            if (text.style != null)
            {
                var style = FontStyles.Normal;
                if (text.style.bold) style |= FontStyles.Bold;
                if (text.style.italic) style |= FontStyles.Italic;
                tmp.fontStyle = style;
            }
            if (resolver != null && !string.IsNullOrWhiteSpace(text.fontAsset))
            {
                var font = resolver.ResolveFont(text.fontAsset);
                if (font != null) tmp.font = font;
            }
        }

        private static Image.Type ParseImageType(string imageType)
        {
            switch (imageType)
            {
                case "Sliced": return Image.Type.Sliced;
                case "Tiled": return Image.Type.Tiled;
                case "Filled": return Image.Type.Filled;
                case "Simple":
                default: return Image.Type.Simple;
            }
        }
    }
}

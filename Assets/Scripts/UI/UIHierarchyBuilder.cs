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
                case "Shape": BuildShape(go, node, resolver); break;
                case "RawImage": BuildRawImage(go, node, resolver); break;
                case "Text": BuildText(go, node, resolver); break;
                case "Button": BuildButton(go, node, resolver); break;
                case "InputField": BuildInputField(go, node, resolver); break;
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
            AddBackground(go, node, resolver, node.raycastTarget, forceShape: false);
        }

        private static void BuildShape(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            AddBackground(go, node, resolver, node.raycastTarget, forceShape: true);
        }

        /// <summary>
        /// 节点底图：cornerRadius&gt;0 且无 sprite（或 forceShape）→ UIShape(SDF 零纹理圆角+描边)；否则 Image(精灵/纯色)。
        /// 描边在 shape 模式由 UIShape 内画（取 node.stroke 的 color/weight）；sprite 模式下描边走 ApplyV2Effects 的环精灵。
        /// 返回所建 Graphic（供 Button/InputField 作 targetGraphic）。
        /// </summary>
        private static Graphic AddBackground(GameObject go, UINode node, IUIAssetResolver resolver, bool raycast, bool forceShape)
        {
            bool shapeMode = forceShape || (node.cornerRadius > 0f && string.IsNullOrWhiteSpace(node.sprite));
            if (shapeMode)
            {
                var s = go.AddComponent<UIShape>();
                s.color = ColorUtil.ParseHexOr(node.color, Color.white);
                s.raycastTarget = raycast;
                s.cornerRadius = node.cornerRadius;
                if (node.stroke != null)
                {
                    s.borderWidth = node.stroke.weight;
                    s.borderColor = ColorUtil.ParseHexOr(node.stroke.color, Color.clear);
                }
                return s;
            }
            var img = go.AddComponent<Image>();
            img.color = ColorUtil.ParseHexOr(node.color, Color.white);
            img.raycastTarget = raycast;
            if (resolver != null && !string.IsNullOrWhiteSpace(node.sprite))
                img.sprite = resolver.ResolveSprite(node.sprite);
            img.type = ParseImageType(node.imageType);
            return img;
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
            var g = AddBackground(go, node, resolver, true, forceShape: false);
            var button = go.AddComponent<Button>();
            button.targetGraphic = g;

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

        private static void BuildInputField(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            // 背景（作 targetGraphic）：shape 模式 → UIShape，否则 Image
            var bg = AddBackground(go, node, resolver, true, forceShape: false);

            var input = go.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = node.contentType == "Password"
                ? TMP_InputField.ContentType.Password
                : TMP_InputField.ContentType.Standard;

            // Text Area（视口）：RectMask2D + 内边距
            var pad = node.padding ?? new UIBorder { l = 16, t = 8, r = 16, b = 8 };
            var areaGo = new GameObject("Text Area", typeof(RectTransform));
            var art = (RectTransform)areaGo.transform;
            art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one; art.pivot = new Vector2(0.5f, 0.5f);
            art.offsetMin = new Vector2(pad.l, pad.b);
            art.offsetMax = new Vector2(-pad.r, -pad.t);
            areaGo.AddComponent<RectMask2D>();
            areaGo.transform.SetParent(go.transform, false);

            // 占位符
            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            UISpecMath.Apply((RectTransform)phGo.transform, UISpecMath.StretchFull());
            phGo.transform.SetParent(areaGo.transform, false);
            var ph = phGo.AddComponent<TextMeshProUGUI>();
            ApplyText(ph, node.placeholder ?? new UIText { content = "", color = "#8EC5FF40" }, resolver);
            ph.raycastTarget = false;

            // 输入文字
            var txtGo = new GameObject("Text", typeof(RectTransform));
            UISpecMath.Apply((RectTransform)txtGo.transform, UISpecMath.StretchFull());
            txtGo.transform.SetParent(areaGo.transform, false);
            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            ApplyText(txt, new UIText
            {
                content = "",
                fontSize = node.placeholder != null ? node.placeholder.fontSize : 16f,
                color = node.textColor ?? "#E8F4FF",
                alignment = node.placeholder != null ? node.placeholder.alignment : "MidlineLeft",
                fontAsset = node.placeholder != null ? node.placeholder.fontAsset : null
            }, resolver);
            txt.raycastTarget = false;

            input.textViewport = art;
            input.textComponent = txt;
            input.placeholder = ph;
            input.targetGraphic = bg;
            input.text = "";

            // 密码显隐切换（眼睛）
            if (node.passwordToggle != null && !string.IsNullOrWhiteSpace(node.passwordToggle.sprite))
            {
                var eyeGo = new GameObject("PwToggle", typeof(RectTransform));
                var ert = (RectTransform)eyeGo.transform;
                var t = node.passwordToggle.rect;
                UISpecMath.Apply(ert, UISpecMath.TopLeft(t.x, t.y, t.w, t.h, node.rect.x, node.rect.y));
                eyeGo.transform.SetParent(go.transform, false);
                var eimg = eyeGo.AddComponent<Image>();
                eimg.color = ColorUtil.ParseHexOr(node.passwordToggle.color, Color.white);
                if (resolver != null) eimg.sprite = resolver.ResolveSprite(node.passwordToggle.sprite);
                var ebtn = eyeGo.AddComponent<Button>();
                ebtn.targetGraphic = eimg;
                var toggle = eyeGo.AddComponent<PasswordVisibilityToggle>();
                toggle.input = input;
                toggle.button = ebtn;
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

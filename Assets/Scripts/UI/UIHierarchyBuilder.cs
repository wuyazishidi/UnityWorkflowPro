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
        /// <summary>全局字号放大系数：Figma 字号在 TMP 下视觉偏小，统一温和放大。</summary>
        private const float FontScale = 1.15f;

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

            // 组件。childParent = 子节点的实际挂点（多数组件即 go 本身；ScrollList 重定向到 Content）。
            Transform childParent = go.transform;
            switch (node.type)
            {
                case "Image": BuildImage(go, node, resolver); break;
                case "RawImage": BuildRawImage(go, node, resolver); break;
                case "Text": BuildText(go, node, resolver); break;
                case "Button": BuildButton(go, node, resolver); break;
                case "InputField": BuildInputField(go, node, resolver); break;
                case "ScrollList": childParent = BuildScrollList(go, node, resolver); break;
                // ScrollList 的列表项接线放到子节点建好后（见下方 AddLayoutElements 调用）
                case "Dropdown": BuildDropdown(go, node, resolver); break;
                case "Toggle": BuildToggle(go, node, resolver); break;
                case "Slider": BuildSlider(go, node, resolver); break;
                case "Scrollbar": BuildScrollbar(go, node, resolver); break;
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
                    childGo.transform.SetParent(childParent, false);
                }
            }

            // ScrollList：子项建好后给每项加 LayoutElement，使 LayoutGroup 按设计尺寸堆叠、ContentSizeFitter 正确撑开
            if (node.type == "ScrollList")
                AddScrollItemLayoutElements(childParent, node.scroll);

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
            AddBackground(go, node, resolver, node.raycastTarget);
        }

        /// <summary>节点底图：Image(精灵/纯色)。返回所建 Graphic（供 Button/InputField 作 targetGraphic）。</summary>
        private static Graphic AddBackground(GameObject go, UINode node, IUIAssetResolver resolver, bool raycast)
        {
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
            var g = AddBackground(go, node, resolver, true);
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
            // 背景（作 targetGraphic）：Image(精灵/纯色)
            var bg = AddBackground(go, node, resolver, true);

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

        // ===== 标准 UGUI 组件（spec 004 Phase 2.6） =====

        /// <summary>ScrollList → ScrollRect：Viewport(RectMask2D 裁剪) + Content(LayoutGroup 自动堆叠 + ContentSizeFitter 自动撑开)。
        /// 列表项越多 Content 越高，超过视口即可拖拽滚动。返回 Content 作为子节点挂点。</summary>
        private static Transform BuildScrollList(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            // 可选底图（有 color/sprite 才加，避免无意义白底）
            if (!string.IsNullOrWhiteSpace(node.color) || !string.IsNullOrWhiteSpace(node.sprite))
                AddBackground(go, node, resolver, true);

            bool horizontal = node.scroll != null && node.scroll.horizontal && !(node.scroll.vertical);
            var sr = go.AddComponent<ScrollRect>();
            sr.horizontal = horizontal;
            sr.vertical = !horizontal;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 24f;

            // Viewport：裁剪窗口（RectMask2D 无需图形，省一次 draw）
            var vpGo = new GameObject("Viewport", typeof(RectTransform));
            UISpecMath.Apply((RectTransform)vpGo.transform, UISpecMath.StretchFull());
            vpGo.transform.SetParent(go.transform, false);
            vpGo.AddComponent<RectMask2D>();
            sr.viewport = (RectTransform)vpGo.transform;

            // Content：横向=左对齐沿 x 撑开；竖向(默认)=顶对齐沿 y 撑开
            var contentGo = new GameObject("Content", typeof(RectTransform));
            var crt = (RectTransform)contentGo.transform;
            var pad = node.scroll != null ? node.scroll.padding : null;
            float spacing = node.scroll != null ? node.scroll.spacing : 0f;
            var ro = pad != null
                ? new RectOffset((int)pad.l, (int)pad.r, (int)pad.t, (int)pad.b)
                : new RectOffset(0, 0, 0, 0);

            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            if (horizontal)
            {
                crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(0f, 1f);
                crt.pivot = new Vector2(0f, 0.5f);
                var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
                hlg.childControlWidth = false; hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.spacing = spacing; hlg.padding = ro;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            else
            {
                crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
                crt.pivot = new Vector2(0.5f, 1f);
                var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
                vlg.childControlWidth = true; vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.spacing = spacing; vlg.padding = ro;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            crt.sizeDelta = Vector2.zero;
            crt.anchoredPosition = Vector2.zero;
            contentGo.transform.SetParent(vpGo.transform, false);
            sr.content = crt;

            return contentGo.transform;
        }

        /// <summary>给 ScrollList 每个列表项加 LayoutElement(按设计尺寸)，让 LayoutGroup 按行高堆叠、ContentSizeFitter 正确撑开。</summary>
        private static void AddScrollItemLayoutElements(Transform content, UIScroll scroll)
        {
            bool horizontal = scroll != null && scroll.horizontal && !scroll.vertical;
            for (int i = 0; i < content.childCount; i++)
            {
                if (!(content.GetChild(i) is RectTransform rt)) continue;
                var le = rt.GetComponent<LayoutElement>();
                if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
                if (horizontal)
                {
                    le.minWidth = rt.sizeDelta.x;
                    le.preferredWidth = rt.sizeDelta.x;
                }
                else
                {
                    le.minHeight = rt.sizeDelta.y;
                    le.preferredHeight = rt.sizeDelta.y;
                }
            }
        }

        /// <summary>Dropdown → TMP_Dropdown：底图 + Label(caption) + Arrow + Template(下拉列表，默认隐藏)。</summary>
        private static void BuildDropdown(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var bg = AddBackground(go, node, resolver, true);
            var dd = go.AddComponent<TMP_Dropdown>();
            dd.targetGraphic = bg;

            // Label（显示当前选中项）
            var labelGo = new GameObject("Label", typeof(RectTransform));
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.offsetMin = new Vector2(12f, 6f); lrt.offsetMax = new Vector2(-28f, -7f);
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            ApplyText(label, node.text ?? new UIText { content = "", fontSize = 16f, color = "#E8F4FF", alignment = "MidlineLeft" }, resolver);
            label.raycastTarget = false;
            dd.captionText = label;

            // Arrow（右侧下拉箭头）
            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            var art = (RectTransform)arrowGo.transform;
            art.anchorMin = new Vector2(1f, 0.5f); art.anchorMax = new Vector2(1f, 0.5f); art.pivot = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(20f, 20f); art.anchoredPosition = new Vector2(-16f, 0f);
            arrowGo.transform.SetParent(go.transform, false);
            var arrow = arrowGo.AddComponent<Image>();
            arrow.color = ColorUtil.ParseHexOr("#E8F4FF", Color.white);
            arrow.raycastTarget = false;

            // Template（展开列表，TMP_Dropdown 要求；默认隐藏）
            var templateGo = BuildDropdownTemplate(go, node, resolver, out var itemLabel);
            dd.template = (RectTransform)templateGo.transform;
            dd.itemText = itemLabel;

            dd.ClearOptions();
            if (node.options != null && node.options.Count > 0)
                dd.AddOptions(node.options);

            templateGo.SetActive(false);
        }

        /// <summary>TMP_Dropdown 标准模板：Template(ScrollRect)+Viewport(Mask)+Content+Item(Toggle+背景/勾选/文字)。</summary>
        private static GameObject BuildDropdownTemplate(GameObject parent, UINode node, IUIAssetResolver resolver, out TextMeshProUGUI itemLabel)
        {
            string panelBg = node.color ?? "#0A1E46";

            var template = new GameObject("Template", typeof(RectTransform));
            var trt = (RectTransform)template.transform;
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 0f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, 2f); trt.sizeDelta = new Vector2(0f, 150f);
            template.transform.SetParent(parent.transform, false);
            var tsr = template.AddComponent<ScrollRect>();
            var timg = template.AddComponent<Image>();
            timg.color = ColorUtil.ParseHexOr(panelBg, Color.white);
            tsr.horizontal = false; tsr.vertical = true; tsr.movementType = ScrollRect.MovementType.Clamped;

            // Viewport（Mask 裁剪）
            var vp = new GameObject("Viewport", typeof(RectTransform));
            var vrt = (RectTransform)vp.transform;
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.pivot = new Vector2(0f, 1f);
            vrt.sizeDelta = Vector2.zero; vrt.anchoredPosition = Vector2.zero;
            vp.transform.SetParent(template.transform, false);
            var vmask = vp.AddComponent<Mask>(); vmask.showMaskGraphic = false;
            var vimg = vp.AddComponent<Image>(); vimg.color = Color.white;
            tsr.viewport = vrt;

            // Content
            var content = new GameObject("Content", typeof(RectTransform));
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f); crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 28f); crt.anchoredPosition = Vector2.zero;
            content.transform.SetParent(vp.transform, false);
            tsr.content = crt;

            // Item（被 TMP_Dropdown 复制为每个候选项）
            var item = new GameObject("Item", typeof(RectTransform));
            var irt = (RectTransform)item.transform;
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(1f, 0.5f); irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(0f, 24f); irt.anchoredPosition = Vector2.zero;
            item.transform.SetParent(content.transform, false);
            var itemToggle = item.AddComponent<Toggle>();

            // Item Background
            var itemBg = new GameObject("Item Background", typeof(RectTransform));
            UISpecMath.Apply((RectTransform)itemBg.transform, UISpecMath.StretchFull());
            itemBg.transform.SetParent(item.transform, false);
            var itemBgImg = itemBg.AddComponent<Image>();
            itemBgImg.color = new Color(1f, 1f, 1f, 0.04f);
            itemToggle.targetGraphic = itemBgImg;

            // Item Checkmark
            var check = new GameObject("Item Checkmark", typeof(RectTransform));
            var chrt = (RectTransform)check.transform;
            chrt.anchorMin = new Vector2(0f, 0.5f); chrt.anchorMax = new Vector2(0f, 0.5f); chrt.pivot = new Vector2(0.5f, 0.5f);
            chrt.sizeDelta = new Vector2(16f, 16f); chrt.anchoredPosition = new Vector2(12f, 0f);
            check.transform.SetParent(item.transform, false);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = ColorUtil.ParseHexOr("#4F8CFF", Color.white);
            itemToggle.graphic = checkImg;

            // Item Label
            var itemLabelGo = new GameObject("Item Label", typeof(RectTransform));
            var ilrt = (RectTransform)itemLabelGo.transform;
            ilrt.anchorMin = Vector2.zero; ilrt.anchorMax = Vector2.one; ilrt.pivot = new Vector2(0.5f, 0.5f);
            ilrt.offsetMin = new Vector2(28f, 1f); ilrt.offsetMax = new Vector2(-10f, -2f);
            itemLabelGo.transform.SetParent(item.transform, false);
            itemLabel = itemLabelGo.AddComponent<TextMeshProUGUI>();
            ApplyText(itemLabel, new UIText { content = "Option", fontSize = 14f, color = "#E8F4FF", alignment = "MidlineLeft" }, resolver);
            itemLabel.raycastTarget = false;

            return template;
        }

        /// <summary>Toggle：背景图(targetGraphic 占满节点) + 勾选图(graphic 内缩)。节点带 text 时右侧加 Label。</summary>
        private static void BuildToggle(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var toggle = go.AddComponent<Toggle>();

            var bgGo = new GameObject("Background", typeof(RectTransform));
            UISpecMath.Apply((RectTransform)bgGo.transform, UISpecMath.StretchFull());
            bgGo.transform.SetParent(go.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = ColorUtil.ParseHexOr(node.color, Color.white);
            if (resolver != null && !string.IsNullOrWhiteSpace(node.sprite))
                bgImg.sprite = resolver.ResolveSprite(node.sprite);
            bgImg.type = ParseImageType(node.imageType);
            toggle.targetGraphic = bgImg;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            var chrt = (RectTransform)checkGo.transform;
            chrt.anchorMin = Vector2.zero; chrt.anchorMax = Vector2.one; chrt.pivot = new Vector2(0.5f, 0.5f);
            chrt.offsetMin = new Vector2(4f, 4f); chrt.offsetMax = new Vector2(-4f, -4f);
            checkGo.transform.SetParent(go.transform, false);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = ColorUtil.ParseHexOr("#4F8CFF", Color.white);
            checkImg.raycastTarget = false;
            toggle.graphic = checkImg;

            toggle.isOn = node.isOn;

            if (node.text != null && !string.IsNullOrEmpty(node.text.content))
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                var lrt = (RectTransform)labelGo.transform;
                lrt.anchorMin = new Vector2(1f, 0f); lrt.anchorMax = new Vector2(1f, 1f); lrt.pivot = new Vector2(0f, 0.5f);
                lrt.sizeDelta = new Vector2(node.rect.w * 3f, 0f); lrt.anchoredPosition = new Vector2(8f, 0f);
                labelGo.transform.SetParent(go.transform, false);
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                ApplyText(tmp, node.text, resolver);
                tmp.raycastTarget = false;
            }
        }

        /// <summary>Slider：Background + Fill Area/Fill + Handle Slide Area/Handle，按 range/direction 接线。</summary>
        private static void BuildSlider(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var slider = go.AddComponent<Slider>();

            var bgGo = new GameObject("Background", typeof(RectTransform));
            var brt = (RectTransform)bgGo.transform;
            brt.anchorMin = new Vector2(0f, 0.25f); brt.anchorMax = new Vector2(1f, 0.75f);
            brt.pivot = new Vector2(0.5f, 0.5f); brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
            bgGo.transform.SetParent(go.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = ColorUtil.ParseHexOr(node.color ?? "#1B2B52", Color.white);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fart = (RectTransform)fillArea.transform;
            fart.anchorMin = new Vector2(0f, 0.25f); fart.anchorMax = new Vector2(1f, 0.75f);
            fart.pivot = new Vector2(0.5f, 0.5f); fart.offsetMin = new Vector2(5f, 0f); fart.offsetMax = new Vector2(-15f, 0f);
            fillArea.transform.SetParent(go.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform));
            var frt = (RectTransform)fill.transform;
            frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f); frt.pivot = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(10f, 0f);
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = ColorUtil.ParseHexOr("#4F8CFF", Color.white);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var hart = (RectTransform)handleArea.transform;
            hart.anchorMin = new Vector2(0f, 0f); hart.anchorMax = new Vector2(1f, 1f); hart.pivot = new Vector2(0.5f, 0.5f);
            hart.offsetMin = new Vector2(10f, 0f); hart.offsetMax = new Vector2(-10f, 0f);
            handleArea.transform.SetParent(go.transform, false);
            var handle = new GameObject("Handle", typeof(RectTransform));
            var hrt = (RectTransform)handle.transform;
            hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(0f, 1f); hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(20f, 0f);
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = ColorUtil.ParseHexOr("#E8F4FF", Color.white);

            slider.fillRect = frt;
            slider.handleRect = hrt;
            slider.targetGraphic = handleImg;
            slider.direction = ParseSliderDirection(node.direction);
            var range = node.range ?? new UIRange();
            slider.minValue = range.min; slider.maxValue = range.max; slider.value = range.value;
        }

        /// <summary>Scrollbar：底图 + Sliding Area/Handle，按 size/value/direction 接线。</summary>
        private static void BuildScrollbar(GameObject go, UINode node, IUIAssetResolver resolver)
        {
            var bgImg = go.AddComponent<Image>();
            bgImg.color = ColorUtil.ParseHexOr(node.color ?? "#1B2B52", Color.white);

            var scrollbar = go.AddComponent<Scrollbar>();

            var slideArea = new GameObject("Sliding Area", typeof(RectTransform));
            var sart = (RectTransform)slideArea.transform;
            sart.anchorMin = Vector2.zero; sart.anchorMax = Vector2.one; sart.pivot = new Vector2(0.5f, 0.5f);
            sart.offsetMin = new Vector2(10f, 10f); sart.offsetMax = new Vector2(-10f, -10f);
            slideArea.transform.SetParent(go.transform, false);

            var handle = new GameObject("Handle", typeof(RectTransform));
            var hrt = (RectTransform)handle.transform;
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one; hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
            handle.transform.SetParent(slideArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = ColorUtil.ParseHexOr("#4F8CFF", Color.white);

            scrollbar.handleRect = hrt;
            scrollbar.targetGraphic = handleImg;
            scrollbar.direction = ParseScrollbarDirection(node.direction);
            scrollbar.size = Mathf.Clamp01(node.scrollbarSize);
            scrollbar.value = node.range != null ? Mathf.Clamp01(node.range.value) : 1f;
        }

        private static Slider.Direction ParseSliderDirection(string d)
        {
            switch (d)
            {
                case "RightToLeft": return Slider.Direction.RightToLeft;
                case "BottomToTop": return Slider.Direction.BottomToTop;
                case "TopToBottom": return Slider.Direction.TopToBottom;
                case "LeftToRight":
                default: return Slider.Direction.LeftToRight;
            }
        }

        private static Scrollbar.Direction ParseScrollbarDirection(string d)
        {
            switch (d)
            {
                case "RightToLeft": return Scrollbar.Direction.RightToLeft;
                case "BottomToTop": return Scrollbar.Direction.BottomToTop;
                case "TopToBottom": return Scrollbar.Direction.TopToBottom;
                case "LeftToRight":
                default: return Scrollbar.Direction.LeftToRight;
            }
        }

        private static void ApplyText(TextMeshProUGUI tmp, UIText text, IUIAssetResolver resolver)
        {
            if (text == null) return;
            tmp.text = text.content ?? "";
            // 全局适当放大字号：Figma 字号在 TMP 下视觉偏小
            tmp.fontSize = text.fontSize * FontScale;
            tmp.color = ColorUtil.ParseHexOr(text.color, Color.white);
            tmp.alignment = AlignmentMap.GetOr(text.alignment);
            // 单行标签不换行、不裁切：避免窄盒子（如“刷 新”）把文字挤成两行或截断
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
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

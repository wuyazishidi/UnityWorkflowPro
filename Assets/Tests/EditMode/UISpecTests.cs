using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// M1 纯核心覆盖：坐标数学、颜色解析、对齐映射、Spec 校验、JSON 解析、建树结构。
    /// 不渲染，只断言数据与组件结构（EditMode 可建 GameObject/加 UI 组件）。
    /// </summary>
    public class UISpecTests
    {
        // ---------- UISpecMath ----------

        [Test]
        public void TopLeft_NestedChild_MapsToParentLocalOffset()
        {
            // 子(810,600,300,80) 在父(710,360) 下 → 局部偏移 (100,-240)
            var layout = UISpecMath.TopLeft(810, 600, 300, 80, 710, 360);
            Assert.AreEqual(new Vector2(0, 1), layout.anchorMin);
            Assert.AreEqual(new Vector2(0, 1), layout.anchorMax);
            Assert.AreEqual(new Vector2(0, 1), layout.pivot);
            Assert.AreEqual(new Vector2(300, 80), layout.sizeDelta);
            Assert.AreEqual(new Vector2(100, -240), layout.anchoredPosition);
        }

        [Test]
        public void TopLeft_RootLevel_UsesAbsoluteCoords()
        {
            var layout = UISpecMath.TopLeft(100, 50, 200, 60, 0, 0);
            Assert.AreEqual(new Vector2(100, -50), layout.anchoredPosition);
            Assert.AreEqual(new Vector2(200, 60), layout.sizeDelta);
        }

        [Test]
        public void CenterFixed_FixedSizeCentered()
        {
            var layout = UISpecMath.CenterFixed(300, 380);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), layout.anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), layout.anchorMax);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), layout.pivot);
            Assert.AreEqual(new Vector2(300, 380), layout.sizeDelta);
            Assert.AreEqual(Vector2.zero, layout.anchoredPosition);
        }

        [Test]
        public void StretchFull_FillsParent()
        {
            var layout = UISpecMath.StretchFull();
            Assert.AreEqual(new Vector2(0, 0), layout.anchorMin);
            Assert.AreEqual(new Vector2(1, 1), layout.anchorMax);
            Assert.AreEqual(Vector2.zero, layout.sizeDelta);
            Assert.AreEqual(Vector2.zero, layout.anchoredPosition);
        }

        // ---------- ColorUtil ----------

        [Test]
        public void ParseHex_RGB()
        {
            Assert.IsTrue(ColorUtil.TryParseHex("#FF8000", out var c));
            Assert.AreEqual(1f, c.r, 1e-3);
            Assert.AreEqual(128f / 255f, c.g, 1e-3);
            Assert.AreEqual(0f, c.b, 1e-3);
            Assert.AreEqual(1f, c.a, 1e-3);
        }

        [Test]
        public void ParseHex_RGBA_AndNoHashAndLowercase()
        {
            Assert.IsTrue(ColorUtil.TryParseHex("00ff0080", out var c));
            Assert.AreEqual(0f, c.r, 1e-3);
            Assert.AreEqual(1f, c.g, 1e-3);
            Assert.AreEqual(128f / 255f, c.a, 1e-3);
        }

        [TestCase("#GGGGGG")]
        [TestCase("#FFF")]
        [TestCase("")]
        [TestCase(null)]
        public void ParseHex_Invalid_ReturnsFalse(string bad)
        {
            Assert.IsFalse(ColorUtil.TryParseHex(bad, out _));
        }

        // ---------- AlignmentMap ----------

        [Test]
        public void Alignment_KnownAndCaseInsensitive()
        {
            Assert.IsTrue(AlignmentMap.TryGet("center", out var a));
            Assert.AreEqual(TextAlignmentOptions.Center, a);
            Assert.IsTrue(AlignmentMap.TryGet("TopLeft", out var b));
            Assert.AreEqual(TextAlignmentOptions.TopLeft, b);
        }

        [Test]
        public void Alignment_Unknown_ReturnsFalse_GetOrFallsBack()
        {
            Assert.IsFalse(AlignmentMap.TryGet("nope", out _));
            Assert.AreEqual(TextAlignmentOptions.Center, AlignmentMap.GetOr("nope"));
        }

        // ---------- UISpecValidator ----------

        [Test]
        public void Validate_GoodSpec_NoErrors()
        {
            var spec = NewSimpleSpec();
            Assert.IsEmpty(UISpecValidator.Validate(spec));
        }

        [Test]
        public void Validate_MissingRoot_Errors()
        {
            var spec = new UISpec { rootName = "X", root = null };
            Assert.IsNotEmpty(UISpecValidator.Validate(spec));
        }

        [Test]
        public void Validate_BadTypeAndColorAndDuplicateSibling_Errors()
        {
            var spec = new UISpec
            {
                root = new UINode
                {
                    name = "Root", type = "Container", anchorPreset = "stretch-full",
                    rect = new UIRect(0, 0, 1920, 1080),
                    children = new List<UINode>
                    {
                        new UINode { name = "A", type = "Bogus", rect = new UIRect(0, 0, 10, 10) },
                        new UINode { name = "A", type = "Image", color = "#ZZ", rect = new UIRect(0, 0, 10, 10) },
                    }
                }
            };
            var errors = UISpecValidator.Validate(spec);
            Assert.IsTrue(errors.Exists(e => e.Contains("type")), "应报非法 type");
            Assert.IsTrue(errors.Exists(e => e.Contains("颜色")), "应报非法颜色");
            Assert.IsTrue(errors.Exists(e => e.Contains("重名")), "应报兄弟重名");
        }

        [Test]
        public void Validate_TextWithoutContent_Errors()
        {
            var spec = NewSimpleSpec();
            spec.root.children.Add(new UINode { name = "T", type = "Text", rect = new UIRect(0, 0, 100, 30) });
            Assert.IsTrue(UISpecValidator.Validate(spec).Exists(e => e.Contains("text.content")));
        }

        // ---------- UISpecJson ----------

        [Test]
        public void Json_Parse_ValidRoundTrips()
        {
            const string json = @"{
              ""schemaVersion"":1, ""referenceWidth"":1920, ""referenceHeight"":1080, ""rootName"":""P"",
              ""root"":{ ""name"":""P"", ""type"":""Container"", ""anchorPreset"":""stretch-full"",
                ""rect"":{""x"":0,""y"":0,""w"":1920,""h"":1080},
                ""children"":[ {""name"":""Btn"",""type"":""Button"",""rect"":{""x"":810,""y"":600,""w"":300,""h"":80},
                  ""text"":{""content"":""确认"",""fontSize"":32,""alignment"":""Center""}} ] } }";
            var r = UISpecJson.Parse(json);
            Assert.IsTrue(r.Ok, string.Join("; ", r.Errors));
            Assert.AreEqual("P", r.Spec.rootName);
            Assert.AreEqual(1, r.Spec.root.children.Count);
            Assert.AreEqual("确认", r.Spec.root.children[0].text.content);
        }

        [Test]
        public void Json_Parse_MalformedFails()
        {
            var r = UISpecJson.Parse("{ not json");
            Assert.IsFalse(r.Ok);
            Assert.IsNotEmpty(r.Errors);
        }

        // ---------- UIHierarchyBuilder ----------

        [Test]
        public void Build_Hierarchy_StructureAndLayoutAndOrder()
        {
            var spec = new UISpec
            {
                rootName = "Panel",
                root = new UINode
                {
                    name = "Panel", type = "Container", anchorPreset = "stretch-full",
                    rect = new UIRect(0, 0, 1920, 1080),
                    children = new List<UINode>
                    {
                        new UINode { name = "Bg", type = "Image", color = "#202020", rect = new UIRect(0, 0, 1920, 1080) },
                        new UINode
                        {
                            name = "Btn", type = "Button", rect = new UIRect(810, 600, 300, 80),
                            text = new UIText { content = "确认", fontSize = 32, alignment = "Center" }
                        },
                    }
                }
            };

            var root = UIHierarchyBuilder.Build(spec, null);
            try
            {
                Assert.AreEqual("Panel", root.name);
                Assert.AreEqual(2, root.transform.childCount);

                // 绘制层级：index 0 = Bg（底），index 1 = Btn（上）
                Assert.AreEqual("Bg", root.transform.GetChild(0).name);
                Assert.AreEqual("Btn", root.transform.GetChild(1).name);

                var bg = root.transform.GetChild(0).gameObject;
                Assert.IsNotNull(bg.GetComponent<Image>());

                var btn = root.transform.GetChild(1).gameObject;
                Assert.IsNotNull(btn.GetComponent<Image>());
                Assert.IsNotNull(btn.GetComponent<Button>());
                var label = btn.transform.Find("Label");
                Assert.IsNotNull(label, "Button 应有 Label 子节点");
                Assert.AreEqual("确认", label.GetComponent<TextMeshProUGUI>().text);

                // 布局：Btn 绝对(810,600) 在父(0,0) 下 → anchoredPosition (810,-600)，sizeDelta (300,80)
                var brt = (RectTransform)btn.transform;
                Assert.AreEqual(new Vector2(810, -600), brt.anchoredPosition);
                Assert.AreEqual(new Vector2(300, 80), brt.sizeDelta);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- v2 无损字段（spec 004 Phase 2） ----------

        [Test]
        public void Json_Parse_V2Fields_RoundTrip()
        {
            const string json = @"{
              ""schemaVersion"":1, ""referenceWidth"":1106, ""referenceHeight"":778, ""rootName"":""P"",
              ""root"":{ ""name"":""P"", ""type"":""Container"", ""rect"":{""x"":0,""y"":0,""w"":1106,""h"":778},
                ""children"":[ {""name"":""Field"",""type"":""Image"",""color"":""#0A1E4640"",
                  ""rect"":{""x"":0,""y"":0,""w"":300,""h"":56}, ""opacity"":0.5,
                  ""stroke"":{""color"":""#388BFDB3"",""weight"":2,""sprite"":""Assets/UI/Common/ring12.png"",
                    ""border"":{""l"":12,""t"":12,""r"":12,""b"":12}},
                  ""gradient"":{""type"":""Linear"",""angle"":90,""stops"":[{""color"":""#2563EB"",""pos"":0},{""color"":""#4F46E5"",""pos"":1}]},
                  ""constraints"":{""horizontal"":""Center"",""vertical"":""Top""}} ] } }";
            var r = UISpecJson.Parse(json);
            Assert.IsTrue(r.Ok, string.Join("; ", r.Errors));
            var f = r.Spec.root.children[0];
            Assert.AreEqual(0.5f, f.opacity, 1e-4);
            Assert.IsNotNull(f.stroke);
            Assert.AreEqual("Assets/UI/Common/ring12.png", f.stroke.sprite);
            Assert.IsNotNull(f.gradient);
            Assert.AreEqual(2, f.gradient.stops.Count);
            Assert.AreEqual(90f, f.gradient.angle, 1e-4);
            Assert.AreEqual("Center", f.constraints.horizontal);
        }

        [Test]
        public void Json_Parse_V1Spec_DefaultsAndStillBuilds()
        {
            // v1 旧 spec（无任何 v2 字段）：opacity 默认 1、无 stroke/gradient，照常 build（回归）
            const string json = @"{ ""schemaVersion"":1, ""referenceWidth"":800, ""referenceHeight"":600, ""rootName"":""P"",
              ""root"":{ ""name"":""P"", ""type"":""Image"", ""color"":""#222222"", ""rect"":{""x"":0,""y"":0,""w"":800,""h"":600} } }";
            var r = UISpecJson.Parse(json);
            Assert.IsTrue(r.Ok, string.Join("; ", r.Errors));
            Assert.AreEqual(1f, r.Spec.root.opacity, 1e-4);
            Assert.IsNull(r.Spec.root.stroke);
            var go = UIHierarchyBuilder.Build(r.Spec, null);
            try { Assert.IsNull(go.GetComponent<CanvasGroup>()); Assert.IsNull(go.transform.Find("P_Stroke")); }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Build_StrokeOpacityGradient_Realized()
        {
            var spec = new UISpec
            {
                rootName = "Field",
                root = new UINode
                {
                    name = "Field", type = "Image", color = "#0A1E4640", anchorPreset = "center",
                    rect = new UIRect(0, 0, 300, 56), opacity = 0.5f,
                    stroke = new UIStroke { color = "#388BFD", sprite = "Assets/UI/Common/ring12.png",
                        border = new UIBorder { l = 12, t = 12, r = 12, b = 12 } },
                    gradient = new UIGradient { type = "Linear", angle = 90,
                        stops = new List<UIGradientStop> {
                            new UIGradientStop { color = "#2563EB", pos = 0 },
                            new UIGradientStop { color = "#4F46E5", pos = 1 } } }
                }
            };
            var go = UIHierarchyBuilder.Build(spec, null);
            try
            {
                // 描边 → 子物体 _Stroke（Sliced Image，置最上）
                var stroke = go.transform.Find("Field_Stroke");
                Assert.IsNotNull(stroke, "应生成描边子物体");
                Assert.AreEqual(Image.Type.Sliced, stroke.GetComponent<Image>().type);
                Assert.AreEqual(go.transform.childCount - 1, stroke.GetSiblingIndex(), "描边应在最上层");
                // 不透明度 → CanvasGroup
                var cg = go.GetComponent<CanvasGroup>();
                Assert.IsNotNull(cg); Assert.AreEqual(0.5f, cg.alpha, 1e-4);
                // 渐变 → 顶点色修饰器
                Assert.IsNotNull(go.GetComponent<UIVertexGradient>());
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Validate_BadV2Fields_Errors()
        {
            var spec = NewSimpleSpec();
            spec.root.children[0].opacity = 1.5f;                         // 越界
            spec.root.children[0].stroke = new UIStroke { color = "#ZZZ" }; // 非法色
            var errs = UISpecValidator.Validate(spec);
            Assert.IsTrue(errs.Exists(e => e.Contains("opacity")));
            Assert.IsTrue(errs.Exists(e => e.Contains("描边色")));
        }

        // ---------- 语义组件：InputField（spec 004 Phase 2.5） ----------

        [Test]
        public void Json_Parse_InputField_RoundTrips()
        {
            const string json = @"{ ""schemaVersion"":1, ""referenceWidth"":1106, ""referenceHeight"":778, ""rootName"":""P"",
              ""root"":{ ""name"":""P"", ""type"":""Container"", ""rect"":{""x"":0,""y"":0,""w"":1106,""h"":778},
                ""children"":[ {""name"":""PwInput"",""type"":""InputField"",""color"":""#0A1E4640"",
                  ""rect"":{""x"":436,""y"":309,""w"":307,""h"":57},""contentType"":""Password"",
                  ""placeholder"":{""content"":""请输入密码"",""fontSize"":16,""color"":""#8EC5FF40"",""alignment"":""MidlineLeft""},
                  ""passwordToggle"":{""sprite"":""Assets/UI/Login/Icons/art1.png"",""color"":""#FFFFFF"",""rect"":{""x"":711,""y"":330,""w"":15,""h"":15}}} ] } }";
            var r = UISpecJson.Parse(json);
            Assert.IsTrue(r.Ok, string.Join("; ", r.Errors));
            var f = r.Spec.root.children[0];
            Assert.AreEqual("InputField", f.type);
            Assert.AreEqual("Password", f.contentType);
            Assert.AreEqual("请输入密码", f.placeholder.content);
            Assert.IsNotNull(f.passwordToggle);
        }

        [Test]
        public void Build_InputField_WiresTMPInputField()
        {
            var spec = new UISpec
            {
                rootName = "P",
                root = new UINode
                {
                    name = "P", type = "Container", anchorPreset = "center", rect = new UIRect(0, 0, 1106, 778),
                    children = new List<UINode>
                    {
                        new UINode
                        {
                            name = "PwInput", type = "InputField", color = "#0A1E4640",
                            rect = new UIRect(436, 309, 307, 57), contentType = "Password",
                            placeholder = new UIText { content = "请输入密码", fontSize = 16, color = "#8EC5FF40", alignment = "MidlineLeft" },
                            passwordToggle = new UIPasswordToggle { sprite = "x.png", color = "#FFFFFF", rect = new UIRect(711, 330, 15, 15) }
                        }
                    }
                }
            };
            var root = UIHierarchyBuilder.Build(spec, null);
            try
            {
                var fieldGo = root.transform.Find("PwInput").gameObject;
                var input = fieldGo.GetComponent<TMP_InputField>();
                Assert.IsNotNull(input, "应生成 TMP_InputField");
                Assert.IsNotNull(input.textComponent, "textComponent 接线");
                Assert.IsNotNull(input.placeholder, "placeholder 接线");
                Assert.IsNotNull(input.textViewport, "textViewport 接线");
                Assert.IsNotNull(input.targetGraphic, "targetGraphic 接线");
                Assert.AreEqual(TMP_InputField.ContentType.Password, input.contentType);
                Assert.IsNotNull(fieldGo.transform.Find("Text Area").GetComponent<RectMask2D>(), "Text Area 应有 RectMask2D");
                // 眼睛切换：PwToggle 子物体 + 组件接线
                var eye = fieldGo.transform.Find("PwToggle");
                Assert.IsNotNull(eye, "密码框应有眼睛切换");
                var toggle = eye.GetComponent<PasswordVisibilityToggle>();
                Assert.IsNotNull(toggle);
                Assert.AreSame(input, toggle.input, "toggle 关联到该 InputField");
                Assert.IsNotNull(toggle.button);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ---------- 标准 UGUI 组件（spec 004 Phase 2.6） ----------

        [Test]
        public void Validate_NewUGUITypes_Accepted()
        {
            var spec = NewSimpleSpec();
            spec.root.children.Add(new UINode { name = "L", type = "ScrollList", rect = new UIRect(0, 0, 10, 10) });
            spec.root.children.Add(new UINode { name = "D", type = "Dropdown", rect = new UIRect(0, 0, 10, 10) });
            spec.root.children.Add(new UINode { name = "T", type = "Toggle", rect = new UIRect(0, 0, 10, 10) });
            spec.root.children.Add(new UINode { name = "S", type = "Slider", rect = new UIRect(0, 0, 10, 10) });
            spec.root.children.Add(new UINode { name = "B", type = "Scrollbar", rect = new UIRect(0, 0, 10, 10) });
            Assert.IsEmpty(UISpecValidator.Validate(spec));
        }

        [Test]
        public void Validate_BadDirectionAndRange_Errors()
        {
            var spec = NewSimpleSpec();
            spec.root.children.Add(new UINode { name = "S", type = "Slider", rect = new UIRect(0, 0, 10, 10),
                direction = "Sideways", range = new UIRange { min = 5, max = 1 } });
            var errs = UISpecValidator.Validate(spec);
            Assert.IsTrue(errs.Exists(e => e.Contains("direction")), "应报非法 direction");
            Assert.IsTrue(errs.Exists(e => e.Contains("range")), "应报 range 越界");
        }

        [Test]
        public void Build_ScrollList_WiresScrollRectAndReparentsChildren()
        {
            var spec = WrapSingle(new UINode
            {
                name = "List", type = "ScrollList", rect = new UIRect(0, 0, 400, 200),
                scroll = new UIScroll { horizontal = false, vertical = true },
                children = new List<UINode>
                {
                    new UINode { name = "Row0", type = "Image", color = "#222222", rect = new UIRect(0, 0, 400, 56) },
                    new UINode { name = "Row1", type = "Image", color = "#333333", rect = new UIRect(0, 60, 400, 56) },
                }
            });
            var root = UIHierarchyBuilder.Build(spec, null);
            try
            {
                var list = root.transform.Find("List");
                var sr = list.GetComponent<ScrollRect>();
                Assert.IsNotNull(sr, "应有 ScrollRect");
                Assert.IsTrue(sr.vertical); Assert.IsFalse(sr.horizontal);
                Assert.IsNotNull(list.Find("Viewport").GetComponent<RectMask2D>(), "Viewport 应有 RectMask2D");
                var content = root.transform.Find("List/Viewport/Content");
                Assert.IsNotNull(content, "应有 Viewport/Content");
                Assert.AreSame(content, sr.content, "ScrollRect.content 接线");
                Assert.AreEqual(2, content.childCount, "行应重定向到 Content 下");
                Assert.IsNotNull(content.Find("Row0"));
                // 自动撑开：VerticalLayoutGroup + ContentSizeFitter(竖向 PreferredSize)
                Assert.IsNotNull(content.GetComponent<VerticalLayoutGroup>(), "Content 应有 VerticalLayoutGroup");
                var fitter = content.GetComponent<ContentSizeFitter>();
                Assert.IsNotNull(fitter, "Content 应有 ContentSizeFitter");
                Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.verticalFit);
                // 每个列表项带 LayoutElement(行高)，使布局组按行高堆叠
                var le0 = content.Find("Row0").GetComponent<LayoutElement>();
                Assert.IsNotNull(le0, "列表项应有 LayoutElement");
                Assert.AreEqual(56f, le0.preferredHeight, 1e-3, "preferredHeight=设计行高");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Build_Dropdown_WiresTMPDropdown()
        {
            var spec = WrapSingle(new UINode
            {
                name = "DD", type = "Dropdown", color = "#0A1E46", rect = new UIRect(0, 0, 200, 40),
                options = new List<string> { "A", "B", "C" },
                text = new UIText { content = "选择", fontSize = 14, alignment = "MidlineLeft" }
            });
            var root = UIHierarchyBuilder.Build(spec, null);
            try
            {
                var dd = root.transform.Find("DD").GetComponent<TMP_Dropdown>();
                Assert.IsNotNull(dd, "应有 TMP_Dropdown");
                Assert.IsNotNull(dd.captionText, "captionText 接线");
                Assert.IsNotNull(dd.itemText, "itemText 接线");
                Assert.IsNotNull(dd.template, "template 接线");
                Assert.AreEqual(3, dd.options.Count, "options 写入");
                Assert.IsFalse(root.transform.Find("DD/Template").gameObject.activeSelf, "模板默认隐藏");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Build_Toggle_WiresToggle()
        {
            var spec = WrapSingle(new UINode { name = "Tg", type = "Toggle", color = "#1B2B52", rect = new UIRect(0, 0, 30, 30), isOn = true });
            var root = UIHierarchyBuilder.Build(spec, null);
            try
            {
                var t = root.transform.Find("Tg").GetComponent<Toggle>();
                Assert.IsNotNull(t, "应有 Toggle");
                Assert.IsTrue(t.isOn, "isOn 写入");
                Assert.IsNotNull(t.targetGraphic, "targetGraphic 接线");
                Assert.IsNotNull(t.graphic, "graphic(勾选) 接线");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Build_Slider_WiresSlider()
        {
            var spec = WrapSingle(new UINode { name = "Sl", type = "Slider", rect = new UIRect(0, 0, 200, 20),
                direction = "LeftToRight", range = new UIRange { min = 0, max = 10, value = 5 } });
            var root = UIHierarchyBuilder.Build(spec, null);
            try
            {
                var s = root.transform.Find("Sl").GetComponent<Slider>();
                Assert.IsNotNull(s, "应有 Slider");
                Assert.IsNotNull(s.fillRect, "fillRect 接线");
                Assert.IsNotNull(s.handleRect, "handleRect 接线");
                Assert.AreEqual(10f, s.maxValue, 1e-4);
                Assert.AreEqual(5f, s.value, 1e-4);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Build_Scrollbar_WiresScrollbar()
        {
            var spec = WrapSingle(new UINode { name = "Sb", type = "Scrollbar", rect = new UIRect(0, 0, 20, 200),
                scrollbarSize = 0.4f, range = new UIRange { value = 0.5f } });
            var root = UIHierarchyBuilder.Build(spec, null);
            try
            {
                var sb = root.transform.Find("Sb").GetComponent<Scrollbar>();
                Assert.IsNotNull(sb, "应有 Scrollbar");
                Assert.IsNotNull(sb.handleRect, "handleRect 接线");
                Assert.AreEqual(0.4f, sb.size, 1e-4);
                Assert.AreEqual(0.5f, sb.value, 1e-4);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ---------- helpers ----------

        private static UISpec WrapSingle(UINode child)
        {
            return new UISpec
            {
                schemaVersion = 1, referenceWidth = 800, referenceHeight = 600, rootName = "P",
                root = new UINode
                {
                    name = "P", type = "Container", anchorPreset = "stretch-full", rect = new UIRect(0, 0, 800, 600),
                    children = new List<UINode> { child }
                }
            };
        }

        private static UISpec NewSimpleSpec()
        {
            return new UISpec
            {
                schemaVersion = 1, referenceWidth = 1920, referenceHeight = 1080, rootName = "Root",
                root = new UINode
                {
                    name = "Root", type = "Container", anchorPreset = "stretch-full",
                    rect = new UIRect(0, 0, 1920, 1080),
                    children = new List<UINode>
                    {
                        new UINode { name = "Bg", type = "Image", color = "#FFFFFF", rect = new UIRect(0, 0, 1920, 1080) }
                    }
                }
            };
        }
    }
}

using NUnit.Framework;
using Game.UI;

namespace Game.Tests.EditMode
{
    /// <summary>FigmaToUISpec 转换器的纯逻辑覆盖（内联一个最小 Figma 文件 JSON）。</summary>
    public class FigmaToUISpecTests
    {
        private const string Json = @"{
          ""document"": { ""type"":""DOCUMENT"",""name"":""Doc"",""children"":[
            { ""type"":""CANVAS"",""name"":""Page"",""children"":[
              { ""type"":""FRAME"",""name"":""Panel"",
                ""absoluteBoundingBox"":{""x"":100,""y"":50,""width"":300,""height"":200},
                ""fills"":[{""type"":""SOLID"",""color"":{""r"":0.94,""g"":0.96,""b"":0.97,""a"":1}}],
                ""children"":[
                  { ""type"":""TEXT"",""name"":""Title"",
                    ""absoluteBoundingBox"":{""x"":120,""y"":66,""width"":200,""height"":24},
                    ""characters"":""标题"",
                    ""style"":{""fontSize"":18,""textAlignHorizontal"":""LEFT""},
                    ""fills"":[{""type"":""SOLID"",""color"":{""r"":0.1,""g"":0.1,""b"":0.1,""a"":1}}] },
                  { ""type"":""FRAME"",""name"":""btn_ok"",
                    ""absoluteBoundingBox"":{""x"":280,""y"":200,""width"":100,""height"":36},
                    ""fills"":[{""type"":""SOLID"",""color"":{""r"":0.1451,""g"":0.3882,""b"":0.9216,""a"":1}}],
                    ""children"":[
                      { ""type"":""TEXT"",""name"":""OkText"",
                        ""absoluteBoundingBox"":{""x"":280,""y"":208,""width"":100,""height"":20},
                        ""characters"":""确定"",
                        ""style"":{""fontSize"":15,""textAlignHorizontal"":""CENTER""},
                        ""fills"":[{""type"":""SOLID"",""color"":{""r"":1,""g"":1,""b"":1,""a"":1}}] }
                    ] }
                ] }
            ] }
          ] }
        }";

        [Test]
        public void Convert_BuildsSpec_FromFrame()
        {
            var r = FigmaToUISpec.Convert(Json, "Panel");
            Assert.IsTrue(r.Ok, string.Join("; ", r.Errors));
            Assert.AreEqual(300, r.Spec.referenceWidth);
            Assert.AreEqual(200, r.Spec.referenceHeight);
            Assert.AreEqual("center", r.Spec.root.anchorPreset);
            // Bg(来自 Frame 填充) + Title + btn_ok = 3
            Assert.AreEqual(3, r.Spec.root.children.Count);
        }

        [Test]
        public void Convert_TextAndButton_MappedWithRelativeCoords()
        {
            var spec = FigmaToUISpec.Convert(Json, "Panel").Spec;

            var title = spec.root.children.Find(c => c.name == "Title");
            Assert.IsNotNull(title);
            Assert.AreEqual("Text", title.type);
            Assert.AreEqual("标题", title.text.content);
            // 绝对像素 = abs(120,66) - frame(100,50) = (20,16)
            Assert.AreEqual(20, title.rect.x, 0.5f);
            Assert.AreEqual(16, title.rect.y, 0.5f);

            var btn = spec.root.children.Find(c => c.name == "btn_ok");
            Assert.IsNotNull(btn);
            Assert.AreEqual("Image", btn.type);          // 有填充 → Image
            Assert.AreEqual("#2563EB", btn.color);       // 0.14,0.39,0.92 → ~#2563EB
            Assert.AreEqual(1, btn.children.Count);       // 子文本
            Assert.AreEqual("确定", btn.children[0].text.content);
        }

        [Test]
        public void Convert_FrameNotFound_Fails()
        {
            var r = FigmaToUISpec.Convert(Json, "Nope");
            Assert.IsFalse(r.Ok);
            Assert.IsNotEmpty(r.Errors);
        }
    }
}

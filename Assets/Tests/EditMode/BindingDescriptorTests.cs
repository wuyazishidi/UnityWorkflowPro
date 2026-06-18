using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// BindingDescriptorBuilder 纯逻辑覆盖：对内存 GameObject 树断言可绑元素表
    /// （key 驼峰/兜底、path、kind、text、装饰与交互内部子节点不入表）。EditMode 可建 UI 组件。
    /// </summary>
    public class BindingDescriptorTests
    {
        GameObject _root;

        [TearDown]
        public void Cleanup()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
        }

        static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject Button(Transform parent, string name, string label)
        {
            var go = Child(parent, name);
            go.AddComponent<Image>();
            go.AddComponent<Button>();
            var lab = Child(go.transform, "Label");
            lab.AddComponent<TextMeshProUGUI>().text = label;
            return go;
        }

        static GameObject Input(Transform parent, string name, string placeholder)
        {
            var go = Child(parent, name);
            go.AddComponent<Image>();
            go.AddComponent<TMP_InputField>();
            var area = Child(go.transform, "Text Area");
            var ph = Child(area.transform, "Placeholder");
            ph.AddComponent<TextMeshProUGUI>().text = placeholder;
            return go;
        }

        BindDoc BuildSample()
        {
            _root = new GameObject("TaskDetailPanel", typeof(RectTransform));
            var container = Child(_root.transform, "Container");

            var title = Child(container.transform, "Title");
            title.AddComponent<TextMeshProUGUI>().text = "#1 桌面小工具";

            Button(container.transform, "BtnStart", "领取并开始任务");   // ASCII 名 → 驼峰 key
            Button(container.transform, "返回列表", "返回列表");          // 中文名 → 兜底 button1

            Input(_root.transform, "InputUser", "请输入用户名");           // InputField

            Child(_root.transform, "Decoration").AddComponent<Image>();    // 装饰 → 不入表

            return BindingDescriptorBuilder.Build(_root, "TaskDetailPanel");
        }

        BindElement Key(BindDoc d, string k) => d.elements.FirstOrDefault(e => e.key == k);

        [Test]
        public void Build_EmitsOnlyBindableElements()
        {
            var doc = BuildSample();
            // 4 个可绑：title / btnStart / button1(返回列表) / inputUser；装饰 Image 与 Button 内 Label 不入表
            Assert.AreEqual(4, doc.elements.Count, "应只列可绑元素，排除装饰与交互内部子节点");
            Assert.IsFalse(doc.elements.Any(e => e.path.EndsWith("/Label")), "Button 的 Label 子节点不得作为 Text 入表");
            Assert.IsFalse(doc.elements.Any(e => e.path.Contains("Decoration")), "装饰 Image 不入表");
        }

        [Test]
        public void Build_AsciiName_CamelizedKey_WithPathAndText()
        {
            var doc = BuildSample();
            var btn = Key(doc, "btnStart");
            Assert.IsNotNull(btn, "ASCII 名 BtnStart 应驼峰化为 btnStart");
            Assert.AreEqual(BindKind.Button, btn.kind);
            Assert.AreEqual("Container/BtnStart", btn.path);
            Assert.AreEqual("领取并开始任务", btn.text);
            Assert.IsFalse(btn.keyAuto);
        }

        [Test]
        public void Build_NonAsciiName_FallsBackToAutoKey()
        {
            var doc = BuildSample();
            var back = doc.elements.FirstOrDefault(e => e.path == "Container/返回列表");
            Assert.IsNotNull(back);
            Assert.AreEqual(BindKind.Button, back.kind);
            Assert.AreEqual("button1", back.key, "中文名应兜底为 <type><序号>");
            Assert.IsTrue(back.keyAuto);
        }

        [Test]
        public void Build_InputAndText_Detected()
        {
            var doc = BuildSample();
            var input = Key(doc, "inputUser");
            Assert.IsNotNull(input);
            Assert.AreEqual(BindKind.InputField, input.kind);
            Assert.AreEqual("InputUser", input.path);
            Assert.AreEqual("请输入用户名", input.text);

            var title = Key(doc, "title");
            Assert.IsNotNull(title);
            Assert.AreEqual(BindKind.Text, title.kind);
            Assert.AreEqual("Container/Title", title.path);
        }

        [Test]
        public void Build_SuffixedName_KeyEncodesType_NoMismatch()
        {
            _root = new GameObject("P", typeof(RectTransform));
            Button(_root.transform, "Start_Btn", "开始");           // 新格式：带 _Btn 后缀
            var doc = BindingDescriptorBuilder.Build(_root, "P");
            var b = doc.elements.FirstOrDefault(e => e.path == "Start_Btn");
            Assert.IsNotNull(b);
            Assert.AreEqual(BindKind.Button, b.kind);
            Assert.AreEqual("startBtn", b.key, "Start_Btn → 驼峰含类型后缀 startBtn（跨类型不撞键）");
            Assert.IsFalse(b.keyAuto);
            Assert.IsFalse(b.nameTypeMismatch, "Button 组件 + _Btn 后缀 → 一致");
        }

        [Test]
        public void Build_SuffixMismatch_FlaggedWhenNotRealComponent()
        {
            _root = new GameObject("P", typeof(RectTransform));
            // 名字像按钮(_Btn)但只是个文本(无 Button 组件) → 暴露「交互元素未生成为对应组件」
            Child(_root.transform, "Confirm_Btn").AddComponent<TextMeshProUGUI>().text = "确认";
            var doc = BindingDescriptorBuilder.Build(_root, "P");
            var e = doc.elements.FirstOrDefault(x => x.path == "Confirm_Btn");
            Assert.IsNotNull(e);
            Assert.AreEqual(BindKind.Text, e.kind, "实际是 Text 组件");
            Assert.IsTrue(e.nameTypeMismatch, "_Btn 后缀 vs Text 组件 → 标记不符");
        }

        [Test]
        public void TrySuffixKind_MapsKnownSuffixes()
        {
            Assert.IsTrue(BindingDescriptorBuilder.TrySuffixKind("X_Btn", out var k1)); Assert.AreEqual(BindKind.Button, k1);
            Assert.IsTrue(BindingDescriptorBuilder.TrySuffixKind("X_InputField", out var k2)); Assert.AreEqual(BindKind.InputField, k2);
            Assert.IsTrue(BindingDescriptorBuilder.TrySuffixKind("X_Dropdown", out var k3)); Assert.AreEqual(BindKind.Dropdown, k3);
            Assert.IsTrue(BindingDescriptorBuilder.TrySuffixKind("X_Text", out var k4)); Assert.AreEqual(BindKind.Text, k4);
            Assert.IsFalse(BindingDescriptorBuilder.TrySuffixKind("PlainName", out _));
        }

        [Test]
        public void Build_ListContainer_EmitsListElementWithItemPrefab()
        {
            _root = new GameObject("P", typeof(RectTransform));
            var list = Child(_root.transform, "ItemParent");
            list.AddComponent<UIListBinding>().itemPrefab = "TaskItem_Btn";
            Child(list.transform, "leftover").AddComponent<TextMeshProUGUI>();   // 子节点：自包含不下钻
            var doc = BindingDescriptorBuilder.Build(_root, "P");
            var el = doc.elements.FirstOrDefault(e => e.path == "ItemParent");
            Assert.IsNotNull(el);
            Assert.AreEqual(BindKind.List, el.kind);
            Assert.AreEqual("TaskItem_Btn", el.itemPrefab, "List 元素记录 item prefab 名");
            Assert.IsFalse(doc.elements.Any(e => e.path.Contains("leftover")), "List 容器自包含，内部节点不入表");
        }
    }
}

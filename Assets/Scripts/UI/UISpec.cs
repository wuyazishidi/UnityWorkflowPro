using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// 一个面板的声明式描述（从效果图量出，序列化为 JSON）。
    /// 字段名与 JSON 键一致（Newtonsoft 按名映射）。坐标单位 = 效果图像素、左上原点。
    /// 详见 specs/002-ui-from-mockup.md。
    /// </summary>
    public class UISpec
    {
        public int schemaVersion = 1;
        public int referenceWidth = 1920;
        public int referenceHeight = 1080;
        public string rootName;
        public UINode root;
    }

    /// <summary>UI 树节点。children 数组顺序 = 兄弟序 = 绘制层级（后者在上）。</summary>
    public class UINode
    {
        public string name;

        /// <summary>Container | Image | RawImage | Text | Button</summary>
        public string type = "Container";

        /// <summary>效果图绝对像素矩形（左上原点）。</summary>
        public UIRect rect = new UIRect();

        /// <summary>#RRGGBB 或 #RRGGBBAA；null 表示用默认（白）。</summary>
        public string color;

        /// <summary>精灵资源路径，如 Assets/UI/Icons/foo.png；null 表示纯色块。</summary>
        public string sprite;

        /// <summary>Simple | Sliced | Tiled | Filled（仅 Image）。</summary>
        public string imageType = "Simple";

        /// <summary>9-slice 边框（像素），配合 imageType=Sliced。</summary>
        public UIBorder border;

        public bool raycastTarget = true;

        /// <summary>null = 左上像素映射（默认）；"stretch-full" = 拉伸填满父级。</summary>
        public string anchorPreset;

        /// <summary>type=Text 必填；type=Button 时作为按钮文字。</summary>
        public UIText text;

        public List<UINode> children = new List<UINode>();
    }

    public class UIRect
    {
        public float x, y, w, h;

        public UIRect() { }
        public UIRect(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
    }

    public class UIBorder
    {
        public float l, t, r, b;
    }

    public class UIText
    {
        public string content = "";
        public string fontAsset;       // TMP_FontAsset 资源路径；null = TMP 默认字体
        public float fontSize = 36;
        public string color = "#FFFFFF";
        public string alignment = "Center";
        public UITextStyle style;
    }

    public class UITextStyle
    {
        public bool bold;
        public bool italic;
    }
}

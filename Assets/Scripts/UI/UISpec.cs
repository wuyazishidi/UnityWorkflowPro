using System.Collections.Generic;

namespace Game.UI
{
    /// <summary>
    /// 一个面板的声明式描述（由 Figma 生成，序列化为 JSON）。
    /// 字段名与 JSON 键一致（Newtonsoft 按名映射）。坐标单位 = 设计像素、左上原点。
    /// 构建引擎见 specs/002；Figma 入口见 specs/004。
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

        /// <summary>设计绝对像素矩形（左上原点）。</summary>
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

        // ===== v2 无损字段（spec 004 Phase 2）：承接 Figma 原语，缺省=null/1 即按 v1 老行为 =====

        /// <summary>节点整体不透明度 0-1（区别于 color 的 alpha）；&lt;1 时 builder 加 CanvasGroup。</summary>
        public float opacity = 1f;

        /// <summary>描边：替代"另起一个镂空环节点"的做法，由 builder 用环精灵实现。</summary>
        public UIStroke stroke;

        /// <summary>渐变填充：替代纯色近似，由 builder 用顶点色网格修饰器实现（v2 先支持线性）。</summary>
        public UIGradient gradient;

        /// <summary>混合模式：Normal/Multiply/Screen…（v2 先只认 Normal，其余忽略并保留）。</summary>
        public string blend;

        /// <summary>锚定意图：承接不丢；Phase 4 才真正驱动 RectTransform。</summary>
        public UIConstraints constraints;

        // ===== 语义组件（spec 004 Phase 2.5）：type=InputField 时生效 =====

        /// <summary>type=InputField 的占位符（文本+色+字号+对齐）。</summary>
        public UIText placeholder;

        /// <summary>type=InputField 内容类型：Standard | Password。</summary>
        public string contentType;

        /// <summary>type=InputField 输入文字颜色（null=亮色默认）。</summary>
        public string textColor;

        /// <summary>type=InputField 文本区内边距（null=默认）。</summary>
        public UIBorder padding;

        /// <summary>type=InputField 的密码显隐切换图标（眼睛）；null=无。</summary>
        public UIPasswordToggle passwordToggle;

        public List<UINode> children = new List<UINode>();
    }

    /// <summary>密码框显隐切换图标（spec 004 Phase 2.5）。运行期点击切换 InputField 的 Standard/Password。</summary>
    public class UIPasswordToggle
    {
        public string sprite;            // 眼睛图标精灵路径
        public string color = "#FFFFFF";
        public UIRect rect = new UIRect(); // 相对面板的绝对像素矩形（左上原点）
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

    /// <summary>描边（v2）。builder 用 sprite（镂空环 9-slice，如 ring12）叠在节点之上染 color 实现。</summary>
    public class UIStroke
    {
        public string color = "#FFFFFF";   // #RRGGBB(AA)
        public float weight = 1f;          // 像素（记录用；当前由环精灵纹理决定实际描边宽度）
        public string align = "Inside";    // Inside | Center | Outside（记录用）
        public string sprite;              // 镂空环精灵路径（如 Assets/UI/Common/ring12.png）
        public UIBorder border;            // 环精灵的 9-slice 边框
    }

    /// <summary>渐变填充（v2，先支持线性两端色近似）。</summary>
    public class UIGradient
    {
        public string type = "Linear";     // Linear（v2 仅此）
        public float angle = 0f;           // 度，0=从左到右，90=从上到下
        public List<UIGradientStop> stops = new List<UIGradientStop>();
    }

    public class UIGradientStop
    {
        public string color = "#FFFFFF";
        public float pos = 0f;             // 0-1
    }

    /// <summary>锚定意图（v2 承接；Phase 4 驱动）。</summary>
    public class UIConstraints
    {
        public string horizontal = "Left"; // Left|Right|Center|Scale|Stretch
        public string vertical = "Top";    // Top|Bottom|Center|Scale|Stretch
    }
}

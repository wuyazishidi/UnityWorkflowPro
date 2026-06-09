using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 计算结果：把效果图像素矩形映射成 RectTransform 的锚点/轴心/尺寸/位置。
    /// 纯数据结构，便于 EditMode 断言。
    /// </summary>
    public struct RectLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public Vector2 anchoredPosition;
    }

    /// <summary>
    /// 纯函数：效果图像素坐标（左上原点，y 向下）→ UGUI RectTransform 布局。
    /// 约定见 specs/002-ui-from-mockup.md。导入器仅把结果套到真实 RectTransform。
    /// </summary>
    public static class UISpecMath
    {
        /// <summary>
        /// 左上锚定。节点绝对矩形 (x,y,w,h)，父节点绝对左上 (parentX,parentY)。
        /// anchorMin=anchorMax=(0,1)、pivot=(0,1)，位置相对父级左上偏移、y 取负。
        /// </summary>
        public static RectLayout TopLeft(float x, float y, float w, float h, float parentX, float parentY)
        {
            return new RectLayout
            {
                anchorMin = new Vector2(0f, 1f),
                anchorMax = new Vector2(0f, 1f),
                pivot = new Vector2(0f, 1f),
                sizeDelta = new Vector2(w, h),
                anchoredPosition = new Vector2(x - parentX, -(y - parentY)),
            };
        }

        /// <summary>
        /// 固定尺寸、居中于父级（子面板根的推荐方式）。
        /// 面板是固定大小、锚定在屏幕中心的元素；不随画布分辨率拉伸，
        /// 这样无论挂到多大的 Canvas 下，内部子元素都按设计尺寸正确排布、不会挤到角落。
        /// </summary>
        public static RectLayout CenterFixed(float w, float h)
        {
            return new RectLayout
            {
                anchorMin = new Vector2(0.5f, 0.5f),
                anchorMax = new Vector2(0.5f, 0.5f),
                pivot = new Vector2(0.5f, 0.5f),
                sizeDelta = new Vector2(w, h),
                anchoredPosition = Vector2.zero,
            };
        }

        /// <summary>拉伸填满父级（全屏背景/根面板）。</summary>
        public static RectLayout StretchFull()
        {
            return new RectLayout
            {
                anchorMin = new Vector2(0f, 0f),
                anchorMax = new Vector2(1f, 1f),
                pivot = new Vector2(0.5f, 0.5f),
                sizeDelta = Vector2.zero,
                anchoredPosition = Vector2.zero,
            };
        }

        /// <summary>把布局结果套到真实 RectTransform（导入器/测试共用，避免重复设值出错）。</summary>
        public static void Apply(RectTransform rt, RectLayout layout)
        {
            rt.anchorMin = layout.anchorMin;
            rt.anchorMax = layout.anchorMax;
            rt.pivot = layout.pivot;
            rt.sizeDelta = layout.sizeDelta;
            rt.anchoredPosition = layout.anchoredPosition;
        }
    }
}

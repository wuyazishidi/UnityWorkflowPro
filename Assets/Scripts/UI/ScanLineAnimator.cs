using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 扫码采集面板的发光指示条：在父容器(扫描取景框)高度范围内匀速上下往返移动。
    /// 挂载节点须用左上锚定(pivot=(0,1))布局(builder 的 TopLeft 布局即满足)，
    /// 往返范围按父容器 RectTransform.rect.height 实时计算，面板尺寸变化也不必调参数。
    /// </summary>
    [DisallowMultipleComponent]
    public class ScanLineAnimator : MonoBehaviour
    {
        [Tooltip("往返一次所需时间（秒）")]
        public float duration = 1.8f;

        [Tooltip("离父容器上下边缘的留白（像素）")]
        public float edgeMargin = 4f;

        private RectTransform _rt;
        private RectTransform _parent;
        private float _startTime;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _parent = transform.parent as RectTransform;
        }

        private void OnEnable()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            if (_parent == null || duration <= 0f) return;
            // pivot=(0,1) 左上锚定：anchoredPosition.y=0 是父级顶边，越向下越负，下边界是 -(父高-自身高)。
            float travel = _parent.rect.height - _rt.rect.height - edgeMargin * 2f;
            if (travel <= 0f) return;
            float topY = -edgeMargin;
            float bottomY = topY - travel;

            float t = (Time.time - _startTime) / duration;
            float pp = Mathf.PingPong(t, 1f);
            float eased = Mathf.SmoothStep(0f, 1f, pp);   // 首尾柔化，避免匀速折返显得生硬
            float y = Mathf.Lerp(topY, bottomY, eased);

            var pos = _rt.anchoredPosition;
            pos.y = y;
            _rt.anchoredPosition = pos;
        }
    }
}

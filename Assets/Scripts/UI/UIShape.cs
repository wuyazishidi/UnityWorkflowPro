using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// SDF 圆角矩形 + 描边的 UI 图形（spec 004 Phase 3）。零纹理、矢量级清晰。
    /// 参数走顶点数据 + 进程级共享材质(UI/ShapeSDF) → 所有 UIShape 合批，加形状不增 DC。
    /// 渐变：叠 UIVertexGradient(修改顶点色 = 填充色)即可。
    /// 注意：正式构建需把 "UI/ShapeSDF" 加入 Project Settings → Always Included Shaders。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIShape : MaskableGraphic
    {
        [Min(0)] public float cornerRadius = 0f;
        [Min(0)] public float borderWidth = 0f;
        public Color borderColor = new Color(1, 1, 1, 0);

        private static Material s_shared;

        // UGUI 默认只上传 TexCoord0/color/position；uv1(尺寸/半径/描边)、uv2(描边色) 必须显式开启，
        // 否则会被剥离 → shader 收到 0 → 形状塌成 0 尺寸。每个 UIShape 在所属 Canvas 上补开通道。
        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannels();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            EnsureCanvasChannels();
        }

        private void EnsureCanvasChannels()
        {
            var c = canvas;
            if (c != null)
                c.additionalShaderChannels |=
                    AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
        }

        private static Material SharedMaterial()
        {
            if (s_shared == null)
            {
                var sh = Shader.Find("UI/ShapeSDF");
                if (sh != null) s_shared = new Material(sh) { name = "UIShapeSDF (shared)" };
            }
            return s_shared;
        }

        public override Material material
        {
            get
            {
                if (m_Material != null && m_Material != defaultMaterial) return m_Material;
                var m = SharedMaterial();
                return m != null ? m : base.material;
            }
            set { base.material = value; }
        }

        // 无贴图
        public override Texture mainTexture => Texture2D.whiteTexture;

        public void Set(Color fill, float radius, float bWidth, Color bColor)
        {
            color = fill;
            cornerRadius = radius;
            borderWidth = bWidth;
            borderColor = bColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float w = r.width, h = r.height;
            float radius = Mathf.Min(cornerRadius, Mathf.Min(w, h) * 0.5f);
            var info = new Vector4(w, h, radius, borderWidth);
            var bc = new Vector4(borderColor.r, borderColor.g, borderColor.b, borderColor.a);
            Color32 fill = color;

            AddVert(vh, new Vector3(r.xMin, r.yMin), new Vector4(0, 0), info, bc, fill);
            AddVert(vh, new Vector3(r.xMin, r.yMax), new Vector4(0, 1), info, bc, fill);
            AddVert(vh, new Vector3(r.xMax, r.yMax), new Vector4(1, 1), info, bc, fill);
            AddVert(vh, new Vector3(r.xMax, r.yMin), new Vector4(1, 0), info, bc, fill);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private static void AddVert(VertexHelper vh, Vector3 pos, Vector4 uv0, Vector4 uv1, Vector4 uv2, Color32 c)
        {
            var v = UIVertex.simpleVert;
            v.position = pos;
            v.color = c;
            v.uv0 = uv0;
            v.uv1 = uv1;
            v.uv2 = uv2;
            vh.AddVert(v);
        }
    }
}

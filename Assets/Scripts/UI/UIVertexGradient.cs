using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 顶点色线性渐变修饰器（spec 004 Phase 2 的渐变"近似"实现，无需 shader）。
    /// 把两端色按 angle 方向插值后乘进图形顶点色——白色精灵(round*)套上即得线性渐变填充。
    /// SDF 渐变(更精确、支持多段)留 Phase 3。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIVertexGradient : BaseMeshEffect
    {
        public Color colorA = Color.white;
        public Color colorB = Color.white;

        /// <summary>角度（度）：0=从左到右，90=从上到下。</summary>
        public float angle = 0f;

        /// <summary>从 UISpec 的 UIGradient 配置（取首/末色作两端，v2 线性近似）。</summary>
        public void Configure(UIGradient g)
        {
            if (g == null || g.stops == null || g.stops.Count < 2) return;
            angle = g.angle;
            colorA = ColorUtil.ParseHexOr(g.stops[0].color, Color.white);
            colorB = ColorUtil.ParseHexOr(g.stops[g.stops.Count - 1].color, Color.white);
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            var verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);
            if (verts.Count == 0) return;

            float rad = angle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)); // y 向下：90° → 从上到下

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < verts.Count; i++)
            {
                float p = Vector2.Dot((Vector2)verts[i].position, dir);
                if (p < min) min = p;
                if (p > max) max = p;
            }
            float range = Mathf.Max(1e-4f, max - min);

            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                float t = (Vector2.Dot((Vector2)v.position, dir) - min) / range;
                v.color *= Color.Lerp(colorA, colorB, Mathf.Clamp01(t));
                verts[i] = v;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}

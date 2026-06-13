using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor.MCP
{
    public class GenRoundedSpriteParams : YIUIMCPBaseParams
    {
        /// <summary>输出 PNG 路径（.png，项目相对）。</summary>
        public string outputPath;
        /// <summary>纹理边长（像素，默认 64）。</summary>
        public int size = 64;
        /// <summary>圆角半径（像素，默认 20）。会作为 9-slice 边框写入导入设置。</summary>
        public int radius = 20;
        /// <summary>描边宽度（像素，默认 0=实心填充）。>0 时生成"镂空描边环"（中间透明，只留圆角描边），
        /// 用于半透明面板：填充用实心圆角(染 Figma 原始半透色) + 上层叠这张描边环(染描边色) → 颜色与 Figma 一致、背景可透出。</summary>
        public float strokeWidth = 0;
    }

    /// <summary>
    /// 程序化生成一张白色圆角矩形 PNG（带抗锯齿 alpha），并设为 Sprite + 9-slice 边框=radius。
    /// 任意 UI 元素套上它(Image Sliced + 用 color 染色)即得圆角。补 UGUI 画不了 border-radius 的短板。
    /// </summary>
    [YIUIMCPTools("GenRoundedSprite", "生成白色圆角矩形 9-slice 精灵")]
    public class YIUIMCPTools_GenRoundedSprite : YIUIMCPBaseExecutor<GenRoundedSpriteParams>
    {
        protected override async Task<YIUIMCPResult> Run(GenRoundedSpriteParams data)
        {
            if (string.IsNullOrWhiteSpace(data.outputPath) || !data.outputPath.EndsWith(".png"))
                return YIUIMCPResult.FailureLog("outputPath 必须以 .png 结尾");

            int s = Mathf.Max(8, data.size);
            float r = Mathf.Clamp(data.radius, 1, s / 2f);

            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // 到"内矩形(四边内缩 r)"最近点的距离 = 圆角矩形的 SDF
                float cx = Mathf.Clamp(x + 0.5f, r, s - r);
                float cy = Mathf.Clamp(y + 0.5f, r, s - r);
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                float a;
                if (data.strokeWidth <= 0f)
                {
                    a = Mathf.Clamp01(r - d + 0.5f); // 实心：0.5px 抗锯齿
                }
                else
                {
                    float t = Mathf.Min(data.strokeWidth, r);
                    float outer = Mathf.Clamp01(r - d + 0.5f);       // 外缘内淡出
                    float inner = Mathf.Clamp01(d - (r - t) + 0.5f); // 内缘外淡出 → 只留宽度 t 的环
                    a = Mathf.Min(outer, inner);
                }
                px[y * s + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();

            var dir = Path.GetDirectoryName(data.outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(data.outputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(data.outputPath);

            // 导入为 Sprite + 9-slice 边框
            var ti = AssetImporter.GetAtPath(data.outputPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                int b = Mathf.CeilToInt(r);
                ti.spriteBorder = new Vector4(b, b, b, b);
                ti.SaveAndReimport();
            }

            await Task.CompletedTask;
            return YIUIMCPResult.SuccessLog($"GenRoundedSprite 成功: {data.outputPath} (size={s}, radius={r}, border={Mathf.CeilToInt(r)})");
        }
    }
}

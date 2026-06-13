using UnityEditor;
using UnityEngine;

namespace Game.UI.EditorTools
{
    /// <summary>
    /// 自动把 Assets/UI/Icons/ 下的图片导入为 Sprite，并设像素级保真的默认参数。
    /// 9-slice 边框由 UIBuilder 按 Spec 单独同步（此处只管基础导入设置）。
    /// </summary>
    public class UIIconPostprocessor : AssetPostprocessor
    {
        // 匹配 Assets/UI 下任意 Icons 目录：Assets/UI/Icons/ 或 Assets/UI/<Panel>/Icons/
        private void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.StartsWith("Assets/UI/") || !p.Contains("/Icons/")) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;

            // 导入分流（spec 004 Phase 1）：小图标维持 Uncompressed（保真、内存极小）；
            // Figma 导出的整卡大图改 Compressed + 限尺寸，避免强制 Uncompressed 占内存/卡死主线程。
            int wpx = ReadPngWidth(assetPath);
            bool large = wpx > 1024;
            ti.maxTextureSize = 2048;
            ti.textureCompression = large
                ? TextureImporterCompression.Compressed
                : TextureImporterCompression.Uncompressed;
        }

        /// <summary>只读 PNG 头里的宽度（IHDR 偏移 16，大端 uint32）；非 PNG/读失败返回 0。</summary>
        private static int ReadPngWidth(string path)
        {
            try
            {
                using (var fs = System.IO.File.OpenRead(path))
                {
                    var b = new byte[24];
                    if (fs.Read(b, 0, 24) == 24 && b[1] == 'P' && b[2] == 'N' && b[3] == 'G')
                        return (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                }
            }
            catch { /* ignore */ }
            return 0;
        }
    }
}

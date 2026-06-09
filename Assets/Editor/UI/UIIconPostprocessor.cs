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
            ti.textureCompression = TextureImporterCompression.Uncompressed; // v1 优先保真
        }
    }
}

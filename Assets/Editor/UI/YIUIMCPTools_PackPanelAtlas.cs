using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace YIUIFramework.Editor.MCP
{
    public class PackPanelAtlasParams : YIUIMCPBaseParams
    {
        /// <summary>该面板的精灵目录（如 Assets/UI/SettingsForm/Icons）。整个目录会被打进一张图集。</summary>
        public string spriteFolder;

        /// <summary>输出图集路径（.spriteatlas，如 Assets/UI/SettingsForm/SettingsForm.spriteatlas）。</summary>
        public string outputAtlasPath;

        /// <summary>图集内边距（默认 4，避免采样溢出/相邻渗色）。</summary>
        public int padding = 4;

        /// <summary>图集最大尺寸（默认 2048）。</summary>
        public int maxTextureSize = 2048;
    }

    /// <summary>
    /// 为单个面板把它的 Icons 目录打成一张独立 SpriteAtlas，让该面板所有精灵共用一张纹理，
    /// UGUI 即可对这些 Image 合批 —— 面板里再加精灵也不会增加额外 draw call。
    /// 需 Sprite Packer 开启（本工程已设为 Sprite Atlas V1 - Always Enabled）。
    /// </summary>
    [YIUIMCPTools("PackPanelAtlas", "为单个面板的 Icons 目录生成并打包独立 SpriteAtlas")]
    public class YIUIMCPTools_PackPanelAtlas : YIUIMCPBaseExecutor<PackPanelAtlasParams>
    {
        protected override async Task<YIUIMCPResult> Run(PackPanelAtlasParams data)
        {
            if (string.IsNullOrWhiteSpace(data.spriteFolder) || !AssetDatabase.IsValidFolder(data.spriteFolder))
                return YIUIMCPResult.FailureLog($"精灵目录无效: {data.spriteFolder}");
            if (string.IsNullOrWhiteSpace(data.outputAtlasPath) || !data.outputAtlasPath.EndsWith(".spriteatlas"))
                return YIUIMCPResult.FailureLog($"输出路径必须以 .spriteatlas 结尾: {data.outputAtlasPath}");

            // 覆盖式创建
            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(data.outputAtlasPath) != null)
                AssetDatabase.DeleteAsset(data.outputAtlasPath);

            var atlas = new SpriteAtlas();

            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,      // UI 不旋转
                enableTightPacking = false,  // 矩形打包，避免 9-slice/带边距图渗色
                padding = data.padding,
            });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            });
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                maxTextureSize = data.maxTextureSize,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Uncompressed, // v1 优先保真
            });

            // 把整个目录作为可打包对象加入（目录内所有 Sprite 都进图集）
            var folderObj = AssetDatabase.LoadAssetAtPath<Object>(data.spriteFolder);
            if (folderObj == null)
                return YIUIMCPResult.FailureLog($"无法加载目录对象: {data.spriteFolder}");
            atlas.Add(new Object[] { folderObj });

            var dir = Path.GetDirectoryName(data.outputAtlasPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(atlas, data.outputAtlasPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(data.outputAtlasPath);

            // 不在此同步 PackAtlases —— 该调用很慢会卡死 HTTP 连接(ECONNRESET)。
            // 工程 Sprite Packer = V1 Always Enabled，会在进入 Play / 构建 / 刷新时自动打包并让精灵绑定到图集。
            int packableCount = Directory.Exists(data.spriteFolder)
                ? Directory.GetFiles(data.spriteFolder, "*.png", SearchOption.TopDirectoryOnly).Length
                : 0;

            await Task.CompletedTask;
            return YIUIMCPResult.SuccessLog(
                $"PackPanelAtlas 成功: {data.outputAtlasPath} (收录目录 {data.spriteFolder}，约 {packableCount} 张图，进入 Play/构建时自动打包合批)");
        }
    }
}

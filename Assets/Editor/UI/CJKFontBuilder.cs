using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace Game.UI.EditorTools
{
    /// <summary>
    /// 从工程内的 MiSans 字体生成 Dynamic SDF 的 TMP 字体资源，并设为 TMP 默认字体。
    /// 优先使用工程已有字体（MiSans，含中文），不外部下载。
    /// Dynamic 模式：任意中文字符在编辑/运行时按需栅格化，无需预烘全字集。
    /// 通过 ExecuteMenu "YIUIMCP/UI/Create MiSans Font Asset" 触发（headless 可用）。
    /// </summary>
    public static class CJKFontBuilder
    {
        private const string TtfPath = "Assets/Fonts/MiSans/ttf/MiSans-Medium.ttf";
        private const string OutPath = "Assets/Fonts/MiSans Medium SDF.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("YIUIMCP/UI/Create MiSans Font Asset")]
        public static void CreateMenu()
        {
            AssetDatabase.Refresh();

            var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                Debug.LogError($"[CJKFont] 找不到字体源文件: {TtfPath}");
                return;
            }

            // 90 采样点、9 padding、SDFAA、1024x1024 图集、Dynamic 多图集
            var asset = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            if (asset == null)
            {
                Debug.LogError("[CJKFont] TMP_FontAsset.CreateFontAsset 返回 null");
                return;
            }

            asset.name = Path.GetFileNameWithoutExtension(OutPath);

            // 覆盖式创建主资源
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath) != null)
                AssetDatabase.DeleteAsset(OutPath);
            AssetDatabase.CreateAsset(asset, OutPath);

            // 把材质与图集纹理作为子资源写入同一 .asset
            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    var tex = asset.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = $"{asset.name} Atlas {i}";
                    AssetDatabase.AddObjectToAsset(tex, asset);
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OutPath);

            SetAsDefault(asset);

            Debug.Log($"[CJKFont] 已生成 Dynamic SDF 字体并设为 TMP 默认: {OutPath}");
        }

        private static void SetAsDefault(TMP_FontAsset asset)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"[CJKFont] 找不到 TMP Settings: {TmpSettingsPath}，未能设为默认字体");
                return;
            }
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop == null)
            {
                Debug.LogWarning("[CJKFont] TMP Settings 无 m_defaultFontAsset 字段");
                return;
            }
            prop.objectReferenceValue = asset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}

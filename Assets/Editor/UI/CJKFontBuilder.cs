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
            var asset = BuildOne(TtfPath, OutPath);
            if (asset != null) SetAsDefault(asset);
        }

        /// <summary>
        /// 生成全部字重 SDF：按字重映射真实字体(figma_sync.weight_font 用 Regular/Medium/Semibold/Bold)。
        /// 仅生成缺失的(Medium/Bold 已手工存在则跳过，避免改 GUID 断引用)。Dynamic 模式按需栅格化。
        /// </summary>
        [MenuItem("YIUIMCP/UI/Create MiSans Weight Assets")]
        public static void CreateWeightAssets()
        {
            AssetDatabase.Refresh();
            var weights = new[]
            {
                ("MiSans-Regular.ttf",  "MiSans Regular SDF.asset"),
                ("MiSans-Semibold.ttf", "MiSans Semibold SDF.asset"),
            };
            foreach (var (ttf, outName) in weights)
            {
                var outp = "Assets/Fonts/" + outName;
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outp) != null)
                {
                    Debug.Log($"[CJKFont] 已存在，跳过: {outp}");
                    continue;
                }
                BuildOne("Assets/Fonts/MiSans/ttf/" + ttf, outp);
            }
            Debug.Log("[CJKFont] 字重 SDF 生成完成 (Regular/Semibold)");
        }

        /// <summary>从 ttf 生成一个 Dynamic SDF TMP 字体资源(覆盖式)，返回资源(失败 null)。</summary>
        private static TMP_FontAsset BuildOne(string ttfPath, string outPath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[CJKFont] 找不到字体源文件: {ttfPath}");
                return null;
            }

            // 90 采样点、9 padding、SDFAA、1024x1024 图集、Dynamic 多图集
            var asset = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            if (asset == null)
            {
                Debug.LogError($"[CJKFont] CreateFontAsset 返回 null: {ttfPath}");
                return null;
            }

            asset.name = Path.GetFileNameWithoutExtension(outPath);

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath) != null)
                AssetDatabase.DeleteAsset(outPath);
            AssetDatabase.CreateAsset(asset, outPath);

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
            AssetDatabase.ImportAsset(outPath);
            Debug.Log($"[CJKFont] 已生成 Dynamic SDF: {outPath}");
            return asset;
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

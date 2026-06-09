using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;
using Game.UI;

namespace Game.UI.EditorTools
{
    /// <summary>导入器结果。</summary>
    public class UIBuildResult
    {
        public bool Ok;
        public string PrefabPath;
        public List<string> Errors = new List<string>();
    }

    /// <summary>
    /// 编辑器导入器：读 JSON Spec → 用 AssetDatabase 解析精灵/字体 → 建树 → 存为 prefab（无 Canvas，可复用）。
    /// 资源 I/O 在此（Editor），建树/数学/校验在 Game 纯核心。
    /// </summary>
    public static class UIBuilder
    {
        public static UIBuildResult Build(string specPath, string outputPrefabPath)
        {
            var result = new UIBuildResult();

            if (string.IsNullOrWhiteSpace(specPath) || !File.Exists(specPath))
            {
                result.Errors.Add($"Spec 文件不存在: {specPath}");
                return result;
            }
            if (string.IsNullOrWhiteSpace(outputPrefabPath) || !outputPrefabPath.EndsWith(".prefab"))
            {
                result.Errors.Add($"输出路径必须以 .prefab 结尾: {outputPrefabPath}");
                return result;
            }

            var parse = UISpecJson.Parse(File.ReadAllText(specPath));
            if (!parse.Ok)
            {
                result.Errors.AddRange(parse.Errors);
                return result;
            }

            // 9-slice 边框同步（Spec 为单一真源）
            SyncSpriteBorders(parse.Spec.root, result);

            var root = UIHierarchyBuilder.Build(parse.Spec, new AssetDatabaseResolver());
            if (root == null)
            {
                result.Errors.Add("建树失败（root 为 null）");
                return result;
            }

            try
            {
                var dir = Path.GetDirectoryName(outputPrefabPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var saved = PrefabUtility.SaveAsPrefabAsset(root, outputPrefabPath, out bool ok);
                if (!ok || saved == null)
                {
                    result.Errors.Add($"保存 prefab 失败: {outputPrefabPath}");
                    return result;
                }
                result.Ok = true;
                result.PrefabPath = outputPrefabPath;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            return result;
        }

        /// <summary>遍历 Spec，把 Sliced 节点的 border 写回对应精灵的导入设置。</summary>
        private static void SyncSpriteBorders(UINode node, UIBuildResult result)
        {
            if (node == null) return;

            if (!string.IsNullOrWhiteSpace(node.sprite) && node.imageType == "Sliced" && node.border != null)
            {
                var importer = AssetImporter.GetAtPath(node.sprite) as TextureImporter;
                if (importer != null)
                {
                    // Unity spriteBorder 顺序: (left, bottom, right, top)
                    var want = new Vector4(node.border.l, node.border.b, node.border.r, node.border.t);
                    if (importer.spriteBorder != want)
                    {
                        importer.spriteBorder = want;
                        importer.SaveAndReimport();
                    }
                }
            }

            if (node.children != null)
                foreach (var c in node.children) SyncSpriteBorders(c, result);
        }

        private class AssetDatabaseResolver : IUIAssetResolver
        {
            public Sprite ResolveSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
            public TMP_FontAsset ResolveFont(string path) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
    }
}

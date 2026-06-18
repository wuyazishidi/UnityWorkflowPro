using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor.MCP
{
    public class ExportUiPackageParams : YIUIMCPBaseParams
    {
        /// <summary>面板名（如 TaskDetailPanel）；资产位于 Assets/UI/&lt;panel&gt;/。</summary>
        public string panel;

        /// <summary>输出 .unitypackage 路径（项目相对，如 delivery/TaskDetailPanel.unitypackage）。</summary>
        public string outputPath;
    }

    /// <summary>
    /// 把一个面板打成可移植交付包（prefab + binding.* + Icons + atlas + 依赖：字体/Common 公共精灵）。
    /// 用 AssetDatabase.ExportPackage(IncludeDependencies|Recurse) 连 .meta 一起导出 → GUID 跨工程保真，
    /// YC-Ego 直接 Import 即可（与本工程零代码耦合）。
    /// </summary>
    [YIUIMCPTools("ExportUiPackage", "把面板 prefab+binding+依赖打成可移植 .unitypackage")]
    public class YIUIMCPTools_ExportUiPackage : YIUIMCPBaseExecutor<ExportUiPackageParams>
    {
        protected override async Task<YIUIMCPResult> Run(ExportUiPackageParams data)
        {
            await Task.CompletedTask;
            if (string.IsNullOrWhiteSpace(data.panel))
                return YIUIMCPResult.FailureLog("ExportUiPackage 失败: panel 为空");

            string dir = $"Assets/UI/{data.panel}";
            string prefab = $"{dir}/{data.panel}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab) == null)
                return YIUIMCPResult.FailureLog($"ExportUiPackage 失败: 找不到 prefab {prefab}");

            // 显式纳入的根资产；其余（Icons/字体/Common 圆角描边精灵）由 IncludeDependencies 自动带上。
            var roots = new List<string> { prefab };
            foreach (var ext in new[] { ".binding.json", ".binding.md", ".spriteatlas" })
            {
                string p = $"{dir}/{data.panel}{ext}";
                if (File.Exists(p)) roots.Add(p);
            }
            // Icons 目录整体纳入（Recurse 会带 .meta），避免个别未被 prefab 直接引用的图标漏掉。
            if (Directory.Exists($"{dir}/Icons")) roots.Add($"{dir}/Icons");

            string outPath = string.IsNullOrWhiteSpace(data.outputPath)
                ? $"delivery/{data.panel}.unitypackage" : data.outputPath;
            string outDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

            try
            {
                AssetDatabase.ExportPackage(
                    roots.ToArray(), outPath,
                    ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);
            }
            catch (System.Exception e)
            {
                return YIUIMCPResult.FailureLog($"ExportUiPackage 失败: {e.Message}");
            }

            if (!File.Exists(outPath))
                return YIUIMCPResult.FailureLog($"ExportUiPackage 失败: 未生成 {outPath}");

            long kb = new FileInfo(outPath).Length / 1024;
            return YIUIMCPResult.SuccessLog(
                $"ExportUiPackage 成功: {outPath} ({kb} KB, 根资产 {roots.Count} 项, 含依赖)");
        }
    }
}

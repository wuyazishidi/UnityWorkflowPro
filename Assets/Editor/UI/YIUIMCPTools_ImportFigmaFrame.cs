using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using Game.UI;

namespace YIUIFramework.Editor.MCP
{
    public class ImportFigmaFrameParams : YIUIMCPBaseParams
    {
        /// <summary>Figma 文件 JSON 路径（GET /v1/files/:key 的返回，存到本地）。</summary>
        public string figmaJsonPath;

        /// <summary>要转换的 Frame 名。</summary>
        public string frameName;

        /// <summary>面板名（决定输出目录 Assets/UI/&lt;panelName&gt;/）。</summary>
        public string panelName;
    }

    /// <summary>
    /// 把一个 Figma Frame 转成本工程的 UISpec，写到 Assets/UI/&lt;panelName&gt;/&lt;panelName&gt;.json。
    /// 取真实文件：先 `curl -H "X-Figma-Token: TOKEN" https://api.figma.com/v1/files/KEY -o x.json`，
    /// 再用本工具转换；随后照常 BuildUIFromSpec。
    /// </summary>
    [YIUIMCPTools("ImportFigmaFrame", "Figma 文件 JSON 的指定 Frame → UISpec")]
    public class YIUIMCPTools_ImportFigmaFrame : YIUIMCPBaseExecutor<ImportFigmaFrameParams>
    {
        protected override async Task<YIUIMCPResult> Run(ImportFigmaFrameParams data)
        {
            if (string.IsNullOrWhiteSpace(data.figmaJsonPath) || !File.Exists(data.figmaJsonPath))
                return YIUIMCPResult.FailureLog($"Figma JSON 不存在: {data.figmaJsonPath}");
            if (string.IsNullOrWhiteSpace(data.frameName))
                return YIUIMCPResult.FailureLog("frameName 不能为空");
            if (string.IsNullOrWhiteSpace(data.panelName))
                return YIUIMCPResult.FailureLog("panelName 不能为空");

            var json = File.ReadAllText(data.figmaJsonPath);
            var conv = FigmaToUISpec.Convert(json, data.frameName);
            if (!conv.Ok)
                return YIUIMCPResult.FailureLog("Figma 转换失败:\n - " + string.Join("\n - ", conv.Errors));

            var dir = $"Assets/UI/{data.panelName}";
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory($"{dir}/Icons");
            var specPath = $"{dir}/{data.panelName}.json";
            File.WriteAllText(specPath, Newtonsoft.Json.JsonConvert.SerializeObject(conv.Spec, Newtonsoft.Json.Formatting.Indented));
            AssetDatabase.ImportAsset(specPath);

            await Task.CompletedTask;
            return YIUIMCPResult.SuccessLog(
                $"ImportFigmaFrame 成功: {specPath}（{conv.Spec.referenceWidth}x{conv.Spec.referenceHeight}，根下 {conv.Spec.root.children.Count} 个子节点）");
        }
    }
}

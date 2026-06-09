using System.Threading.Tasks;
using Game.UI.EditorTools;

namespace YIUIFramework.Editor.MCP
{
    public class BuildUIFromSpecParams : YIUIMCPBaseParams
    {
        /// <summary>JSON Spec 文件路径（项目相对，如 Assets/UI/Specs/Login.json）。</summary>
        public string specPath;

        /// <summary>输出 prefab 路径（项目相对，.prefab 结尾，如 Assets/UI/Prefabs/Login.prefab）。</summary>
        public string outputPrefabPath;
    }

    /// <summary>
    /// 从 JSON Spec 生成/更新 UI 预制体（无 Canvas，可复用）。
    /// 成功返回 prefab 路径；失败返回校验/构建错误列表。
    /// </summary>
    [YIUIMCPTools("BuildUIFromSpec", "从 JSON UI-Spec 生成 UGUI 预制体")]
    public class YIUIMCPTools_BuildUIFromSpec : YIUIMCPBaseExecutor<BuildUIFromSpecParams>
    {
        protected override async Task<YIUIMCPResult> Run(BuildUIFromSpecParams data)
        {
            var r = UIBuilder.Build(data.specPath, data.outputPrefabPath);
            await Task.CompletedTask;

            if (r.Ok)
                return YIUIMCPResult.SuccessLog($"BuildUIFromSpec 成功: {r.PrefabPath}");

            return YIUIMCPResult.FailureLog("BuildUIFromSpec 失败:\n - " + string.Join("\n - ", r.Errors));
        }
    }
}

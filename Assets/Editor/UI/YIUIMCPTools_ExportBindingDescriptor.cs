using System.Threading.Tasks;
using Game.UI.EditorTools;

namespace YIUIFramework.Editor.MCP
{
    public class ExportBindingDescriptorParams : YIUIMCPBaseParams
    {
        /// <summary>已构建的面板 prefab 路径（项目相对，.prefab 结尾，如 Assets/UI/TaskDetailPanel/TaskDetailPanel.prefab）。</summary>
        public string prefabPath;
    }

    /// <summary>
    /// 走已构建 prefab 真实树 → 产 &lt;Panel&gt;.binding.json + .md（消费方解耦交付的描述）。
    /// 成功返回输出路径与元素数；失败返回错误列表。
    /// </summary>
    [YIUIMCPTools("ExportBindingDescriptor", "走已构建 prefab 树生成 UI 绑定描述 json+md")]
    public class YIUIMCPTools_ExportBindingDescriptor : YIUIMCPBaseExecutor<ExportBindingDescriptorParams>
    {
        protected override async Task<YIUIMCPResult> Run(ExportBindingDescriptorParams data)
        {
            var r = BindingDescriptorExporter.Export(data.prefabPath);
            await Task.CompletedTask;

            if (r.Ok)
                return YIUIMCPResult.SuccessLog(
                    $"ExportBindingDescriptor 成功: {r.JsonPath} ({r.ElementCount} 元素, {r.AutoKeyCount} 个自动兜底键)\n - {r.MdPath}");

            return YIUIMCPResult.FailureLog("ExportBindingDescriptor 失败:\n - " + string.Join("\n - ", r.Errors));
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// 让 EditMode 测试可通过 YIUIMCP 的 ExecuteMenu 触发并由控制台读取结果，
    /// 从而把"自动化测试"接入 CLI-first 工作流（编辑器开着也能跑，不占项目锁）。
    /// 用法（Unity 须打开）：
    ///   invoke-uto-tool.ps1 -Tool 'ExecuteMenu' -ParamsBase64 (menuPath=YIUIMCP/Run EditMode Tests)
    /// 然后 get_console_log.ps1 读取以 [YIUIMCP-TESTS] 开头的结果行。
    /// </summary>
    public static class YIUIMCPTestRunner
    {
        public const string ResultMarker = "[YIUIMCP-TESTS]";
        private static readonly string ResultFile =
            Path.Combine(Directory.GetCurrentDirectory(), "Logs", "EditMode-test-results.txt");

        [MenuItem("YIUIMCP/Run EditMode Tests")]
        public static void RunEditModeTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultLogger());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
            Debug.Log($"{ResultMarker} status=STARTED (EditMode)");
        }

        private class ResultLogger : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                string verdict = result.FailCount == 0 ? "PASS" : "FAIL";
                string line =
                    $"{ResultMarker} result={verdict} passed={result.PassCount} " +
                    $"failed={result.FailCount} skipped={result.SkipCount} " +
                    $"duration={result.Duration:F2}s";

                if (result.FailCount == 0) Debug.Log(line);
                else Debug.LogError(line);

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ResultFile));
                    File.WriteAllText(ResultFile, line + "\n");
                }
                catch { /* 写文件失败不影响控制台结果 */ }
            }
        }
    }
}

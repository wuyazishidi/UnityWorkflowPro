using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 经 YIUIMCP ExecuteMenu 触发截图，产出给开发者审核的可视证据。
    /// 用法：ExecuteMenu "YIUIMCP/Capture Screenshot" → 读 Logs/screenshot.png。
    /// 在 Play 模式下截 Game 视图最有效。
    /// </summary>
    public static class ScreenshotMenu
    {
        [MenuItem("YIUIMCP/Capture Screenshot")]
        public static void Capture()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "screenshot.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[YIUIMCP-SHOT] 截图已请求: {path}（Play 模式下最有效；文件在下一帧写出）");
        }
    }
}

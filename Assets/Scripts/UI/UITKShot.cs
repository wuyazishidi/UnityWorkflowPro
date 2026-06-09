using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// Play 模式下让 UITK 面板渲染到屏幕(game view)，用 ScreenCapture 截图存 PNG，然后退出 Play。
    /// 屏幕面板会被运行时正常驱动（targetTexture 离屏面板在编辑器里不刷新，故走屏幕路线）。
    /// </summary>
    public class UITKShot : MonoBehaviour
    {
        public PanelSettings panel;
        public UIDocument document;
        public StyleSheet style;
        public string outputPath = "Logs/uitk_login.png";

        private IEnumerator Start()
        {
            if (panel != null) panel.targetTexture = null; // 渲染到屏幕

            if (document != null && document.rootVisualElement != null && style != null
                && !document.rootVisualElement.styleSheets.Contains(style))
                document.rootVisualElement.styleSheets.Add(style);

            // 等几帧完成布局与绘制
            for (int i = 0; i < 6; i++) yield return null;

            if (document != null && document.rootVisualElement != null)
            {
                var root = document.rootVisualElement;
                var screen = root.childCount > 0 ? root[0] : null;
                var card = screen != null && screen.childCount > 0 ? screen[0] : null;
                Debug.Log($"[UITK-SHOT] rootSize={root.resolvedStyle.width}x{root.resolvedStyle.height} " +
                          $"screenChildren={(screen != null ? screen.childCount : -1)} " +
                          $"cardSize={(card != null ? card.resolvedStyle.width + "x" + card.resolvedStyle.height : "null")} " +
                          $"cardBg={(card != null ? card.resolvedStyle.backgroundColor.ToString() : "-")}");
            }

            var full = Path.Combine(Directory.GetCurrentDirectory(), outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            if (File.Exists(full)) File.Delete(full);

            ScreenCapture.CaptureScreenshot(outputPath);

            // 文件下一帧写出，等它出现
            for (int i = 0; i < 120 && !File.Exists(full); i++) yield return new WaitForEndOfFrame();
            Debug.Log($"[UITK-SHOT] screenshot exists={File.Exists(full)} {outputPath}");

            yield return new WaitForEndOfFrame();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

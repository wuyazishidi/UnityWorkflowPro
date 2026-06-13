using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;

namespace YIUIFramework.Editor.MCP
{
    public class RenderCanvasToPngParams : YIUIMCPBaseParams
    {
        /// <summary>要渲染的面板 prefab 路径（项目相对）。</summary>
        public string prefabPath;

        /// <summary>输出 PNG 路径（可项目外，如 Logs/render.png）。</summary>
        public string outputPngPath;

        /// <summary>渲染宽（默认 1920）。</summary>
        public int width = 0;

        /// <summary>渲染高（默认 1080）。</summary>
        public int height = 0;

        /// <summary>背景色 #RRGGBB[AA]，留空 = 透明。</summary>
        public string backgroundColor;
    }

    /// <summary>
    /// 把面板 prefab 渲染成精确分辨率 PNG（专用正交相机 + RenderTexture），用于与 Figma 真值核对（可选 QA，见 004 -Verify）。
    /// 不依赖游戏视图尺寸，输出尺寸恒定。临时对象渲染后销毁，不污染场景与 prefab。
    /// </summary>
    [YIUIMCPTools("RenderCanvasToPng", "把 UI 预制体渲染为精确分辨率 PNG（供与 Figma 真值核对）")]
    public class YIUIMCPTools_RenderCanvasToPng : YIUIMCPBaseExecutor<RenderCanvasToPngParams>
    {
        protected override async Task<YIUIMCPResult> Run(RenderCanvasToPngParams data)
        {
            int w = data.width > 0 ? data.width : 1920;
            int h = data.height > 0 ? data.height : 1080;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPath);
            if (prefab == null)
                return YIUIMCPResult.FailureLog($"找不到 prefab: {data.prefabPath}");
            if (string.IsNullOrWhiteSpace(data.outputPngPath))
                return YIUIMCPResult.FailureLog("outputPngPath 不能为空");

            GameObject canvasGo = null, camGo = null, panel = null;
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                // 临时 Canvas（ScreenSpaceCamera 才能渲到 RenderTexture）
                canvasGo = new GameObject("__RenderCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(w, h);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                camGo = new GameObject("__RenderCam", typeof(Camera));
                var cam = camGo.GetComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = ColorUtil.ParseHexOr(data.backgroundColor, new Color(0, 0, 0, 0));
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 1000f;
                canvas.worldCamera = cam;
                canvas.planeDistance = 100f;

                panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                panel.transform.SetParent(canvasGo.transform, false);

                // 强制布局与 TMP 字形生成（Dynamic SDF 按需栅格化，渲染前必须）
                Canvas.ForceUpdateCanvases();
                foreach (var t in panel.GetComponentsInChildren<TMP_Text>(true))
                    t.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();

                rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                var dir = Path.GetDirectoryName(data.outputPngPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(data.outputPngPath, tex.EncodeToPNG());

                await Task.CompletedTask;
                return YIUIMCPResult.SuccessLog($"RenderCanvasToPng 成功: {data.outputPngPath} ({w}x{h})");
            }
            finally
            {
                if (tex != null) Object.DestroyImmediate(tex);
                if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                if (panel != null) Object.DestroyImmediate(panel);
                if (camGo != null) Object.DestroyImmediate(camGo);
                if (canvasGo != null) Object.DestroyImmediate(canvasGo);
            }
        }
    }
}

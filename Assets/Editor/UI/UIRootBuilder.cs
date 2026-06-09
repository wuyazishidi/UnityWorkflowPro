using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.EditorTools
{
    /// <summary>
    /// 生成共享根 UIRoot.prefab：Canvas + CanvasScaler(1920x1080) + GraphicRaycaster。
    /// 各面板 prefab（不带 Canvas）在它下面实例化，统一参考分辨率/像素坐标系。
    /// </summary>
    public static class UIRootBuilder
    {
        public const string DefaultPath = "Assets/UI/Prefabs/UIRoot.prefab";
        public const int RefWidth = 1920;
        public const int RefHeight = 1080;

        [MenuItem("YIUIMCP/UI/Create UIRoot Prefab")]
        public static void CreateMenu()
        {
            var path = Build(DefaultPath);
            Debug.Log($"[UIRoot] 已生成: {path}");
        }

        public static string Build(string outputPath)
        {
            var go = new GameObject("UIRoot", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            PrefabUtility.SaveAsPrefabAsset(go, outputPath);
            Object.DestroyImmediate(go);
            return outputPath;
        }
    }
}

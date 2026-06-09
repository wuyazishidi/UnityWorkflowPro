using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Game.UI;

namespace Game.UI.EditorTools
{
    /// <summary>
    /// 搭好"渲染 UITK 登录页到屏幕并截图"的场景：PanelSettings(屏幕) + UIDocument(LoginPanel.uxml) + UITKShot。
    /// 跑法：执行菜单 → 进入 Play（UITKShot 截图后自动退出）→ 读 Logs/uitk_login.png。
    /// </summary>
    public static class SetupUITKShot
    {
        [MenuItem("YIUIMCP/UITK/Setup Login Shot Scene")]
        public static void Setup()
        {
            const string uxmlPath  = "Assets/UI/UITK/LoginPanel.uxml";
            const string tssPath   = "Assets/UI/UITK/DefaultRuntimeTheme.tss";
            const string ussPath   = "Assets/UI/UITK/IndustrialTheme.uss";
            const string psPath    = "Assets/UI/UITK/LoginPanelSettings.asset";
            const string scenePath = "Assets/UI/UITK/UITKShot.unity";

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var tss = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(tssPath);
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (uxml == null || tss == null || uss == null)
            { Debug.LogError("[UITK] 资源缺失 (uxml/tss/uss)"); return; }

            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, psPath);
            }
            ps.themeStyleSheet = tss;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(810, 962);
            ps.match = 0.5f;
            ps.targetTexture = null;
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var docGo = new GameObject("UIDocument", typeof(UIDocument));
            var doc = docGo.GetComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = uxml;

            var shotGo = new GameObject("Shot", typeof(UITKShot));
            var shot = shotGo.GetComponent<UITKShot>();
            shot.panel = ps;
            shot.document = doc;
            shot.style = uss;
            shot.outputPath = "Logs/uitk_login.png";

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[UITK] 场景就绪 {scenePath}（进入 Play 即截图）。");
        }
    }
}

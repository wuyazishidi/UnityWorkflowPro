using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 让 UPM 包解析可经 YIUIMCP 触发：编辑 manifest.json 后，AssetDatabase.Refresh 不会触发解析，
    /// 需显式 Client.Resolve()。用法：ExecuteMenu "YIUIMCP/Resolve Packages"，随后轮询 Library/PackageCache。
    /// 放在 Assets/Editor（预定义 Assembly-CSharp-Editor），无需 asmdef、不依赖任何新包，故能先编译注册。
    /// </summary>
    public static class PackageResolveMenu
    {
        [MenuItem("YIUIMCP/Resolve Packages")]
        public static void ResolvePackages()
        {
            Client.Resolve();
            Debug.Log("[YIUIMCP-PKG] Client.Resolve() 已触发，UPM 将在后台解析/克隆 manifest 中的包");
        }
    }
}

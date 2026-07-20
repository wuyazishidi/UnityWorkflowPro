using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace YIUIFramework.Editor.MCP
{
    /// <summary>
    /// MCP Server 启动模式
    /// </summary>
    public enum EYIUIMCPStartMode
    {
        /// <summary>
        /// 关闭YIUIMCP功能 （默认行为）
        /// 首次安装后修改 防止美术,策划不需要这个功能还要手动关闭,所以默认关闭更友好
        /// </summary>
        [LabelText("关闭启动")]
        Close = 0,

        /// <summary>
        /// 始终自动启动
        /// </summary>
        [LabelText("自动启动")]
        Auto = 1,

        /// <summary>
        /// 手动启动（任何时候都需要手动启动）(基本上不用)
        /// </summary>
        [LabelText("手动启动")]
        Manual = 2,
    }

    /// <summary>
    /// MCP Server 配置管理
    /// </summary>
    public static class YIUIMCPServerConfig
    {
        private const string KEY_START_MODE = "YIUIMCP_StartMode";

        private static EYIUIMCPStartMode m_StartMode;

        private const int DefaultPort = 3212;
        private static int _port = -1;

        /// <summary>
        /// 已配置好的静态端口列表。多个 Unity 工程并行时，启动会按此顺序自动选用第一个空闲端口，
        /// 避免默认端口(3212)被其他工程抢占后本工程无端口可用。
        /// 间隔 10 预留 UTO HTTP 端口(=MCP+1)，避免相邻工程互相串口。
        /// </summary>
        public static readonly int[] CandidatePorts = { 3212, 3222, 3232, 3242, 3252, 3262 };

        /// <summary>
        /// 该端口当前是否空闲（系统范围内没有任何监听者占用）。检测失败时保守地按“被占用”处理。
        /// </summary>
        public static bool IsPortFree(int port)
        {
            try
            {
                return !YIUIMCPPortRecovery.IsPortInUse(port);
            }
            catch (Exception e)
            {
                YIUIMCPLog.LogError($"检测端口占用失败({port}): {e.Message}");
                return false;
            }
        }

        // MCP 端口 + 其 UTO HTTP 端口(=port+1) 都空闲才算可用
        private static bool IsPortPairFree(int port) => IsPortFree(port) && IsPortFree(port + 1);

        /// <summary>
        /// 启动时解析实际监听端口：
        /// 1) 若已保存端口仍空闲 → 沿用（粘性，含手动改过的非列表端口）；
        /// 2) 否则按静态列表顺序选第一个空闲端口；
        /// 3) 全被占用 → 回退默认端口。
        /// 选定后写回 .port，保证 ps1 脚本与编辑器读到同一端口。
        /// </summary>
        public static int ResolveStartupPort()
        {
            var saved = LoadPortFromFile();
            var chosen = saved;

            if (!IsPortPairFree(saved))
            {
                chosen = -1;
                foreach (var p in CandidatePorts)
                {
                    if (IsPortPairFree(p))
                    {
                        chosen = p;
                        break;
                    }
                }

                if (chosen <= 0)
                {
                    YIUIMCPLog.LogError($"静态端口列表 [{string.Join(",", CandidatePorts)}] 全部被占用，回退默认 {DefaultPort}");
                    chosen = DefaultPort;
                }
            }

            _port = chosen;
            if (chosen != saved)
            {
                YIUIMCPLog.Log($"端口 {saved} 被占用，自动选用静态列表中的空闲端口: {chosen}");
                SavePortToFile(chosen);
            }

            return chosen;
        }

        public static int Port
        {
            get
            {
                if (_port <= 0)
                {
                    _port = LoadPortFromFile();
                }

                return _port;
            }
        }

        public static void Initialize()
        {
            m_StartMode = (EYIUIMCPStartMode)EditorPrefs.GetInt(KEY_START_MODE, (int)EYIUIMCPStartMode.Auto);
        }

        /// <summary>
        /// 启动模式
        /// </summary>
        public static EYIUIMCPStartMode StartMode
        {
            get => m_StartMode;
            set
            {
                m_StartMode = value;
                EditorPrefs.SetInt(KEY_START_MODE, (int)value);

                //任何改变都强制编译 这样才能重启域
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        private static string GetPortFilePath()
        {
            var packagePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/cn.etetet.yiuimcp"));
            return Path.Combine(packagePath, "UTO/.port");
        }

        private static int LoadPortFromFile()
        {
            try
            {
                var portFilePath = GetPortFilePath();
                if (File.Exists(portFilePath))
                {
                    var content = File.ReadAllText(portFilePath).Trim();
                    if (int.TryParse(content, out int port) && port > 0 && port <= 65535)
                    {
                        return port;
                    }
                }
            }
            catch (Exception e)
            {
                YIUIMCPLog.LogError($"读取端口配置失败: {e.Message}");
            }

            return DefaultPort;
        }

        public static void SavePortToFile(int port)
        {
            try
            {
                var portFilePath = GetPortFilePath();
                var dir = Path.GetDirectoryName(portFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(portFilePath, port.ToString());
                YIUIMCPLog.Log($"端口配置已保存: {port}");
            }
            catch (Exception e)
            {
                YIUIMCPLog.LogError($"保存端口配置失败: {e.Message}");
            }
        }

        public static void SetPort(int port)
        {
            if (port <= 0 || port > 65535)
            {
                YIUIMCPLog.LogError($"无效的端口号: {port}");
                return;
            }

            _port = port;
            SavePortToFile(port);
        }
    }
}
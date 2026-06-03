using System.IO;
using UnityEngine;

namespace Game.Save
{
    /// <summary>
    /// 存档序列化与文件读写。序列化/反序列化为纯函数（JsonUtility），可 EditMode 单测。
    /// </summary>
    public static class SaveSystem
    {
        public static string Serialize(GameState state) => JsonUtility.ToJson(state);

        public static GameState Deserialize(string json) =>
            string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<GameState>(json);

        public static void Save(string path, GameState state)
        {
            File.WriteAllText(path, Serialize(state));
        }

        public static GameState Load(string path)
        {
            return File.Exists(path) ? Deserialize(File.ReadAllText(path)) : null;
        }
    }
}

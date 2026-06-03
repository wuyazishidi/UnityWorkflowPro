using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public struct ItemStack
    {
        public string id;
        public int count;
    }

    /// <summary>可序列化的存档数据（JsonUtility 兼容：基本类型 + List + [Serializable] 结构）。</summary>
    [Serializable]
    public class GameState
    {
        public float playerX;
        public float playerY;
        public int hp;
        public int score;
        public List<ItemStack> items = new List<ItemStack>();
    }
}

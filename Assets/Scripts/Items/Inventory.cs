using System.Collections.Generic;

namespace Game.Items
{
    /// <summary>
    /// 纯 C# 背包容器：按物品 id 堆叠，受"不同物品槽位数"容量限制。无 Unity 依赖，可直接单测。
    /// </summary>
    public class Inventory
    {
        private readonly int _capacity;
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        public Inventory(int capacity)
        {
            _capacity = capacity < 0 ? 0 : capacity;
        }

        public int Capacity => _capacity;
        public int SlotCount => _items.Count;
        public bool IsFull => _items.Count >= _capacity;

        public int CountOf(string id) => _items.TryGetValue(id, out var c) ? c : 0;

        public int Total
        {
            get
            {
                int t = 0;
                foreach (var kv in _items) t += kv.Value;
                return t;
            }
        }

        public IReadOnlyDictionary<string, int> Items => _items;

        /// <summary>加入物品。已有则堆叠；新物品占一个槽，满槽时拒绝。</summary>
        public bool TryAdd(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return false;
            if (_items.ContainsKey(id))
            {
                _items[id] += amount;
                return true;
            }
            if (IsFull) return false;
            _items[id] = amount;
            return true;
        }

        /// <summary>清空背包（读档恢复用）。</summary>
        public void Clear() => _items.Clear();

        /// <summary>移除物品；数量减到 0 释放槽位。数量不足则拒绝。</summary>
        public bool Remove(string id, int amount = 1)
        {
            if (amount <= 0 || !_items.TryGetValue(id, out var have) || have < amount) return false;
            int left = have - amount;
            if (left == 0) _items.Remove(id);
            else _items[id] = left;
            return true;
        }
    }
}

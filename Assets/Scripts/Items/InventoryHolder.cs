using System;
using UnityEngine;

namespace Game.Items
{
    /// <summary>挂在玩家上，持有一个背包实例并在变化时广播。</summary>
    public class InventoryHolder : MonoBehaviour
    {
        [SerializeField] private int _capacity = 16;
        public Inventory Inventory { get; private set; }
        public event Action OnChanged;

        private void Awake()
        {
            Inventory = new Inventory(_capacity);
        }

        public bool Add(string id, int amount = 1)
        {
            bool ok = Inventory.TryAdd(id, amount);
            if (ok) OnChanged?.Invoke();
            return ok;
        }
    }
}

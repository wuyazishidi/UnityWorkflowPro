using UnityEngine;

namespace Game.Items
{
    /// <summary>世界拾取物：玩家触碰即加入其背包并销毁自身。</summary>
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private string _itemId = "coin";
        [SerializeField] private int _amount = 1;

        public void Configure(string itemId, int amount)
        {
            _itemId = itemId;
            _amount = amount;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var holder = other.GetComponent<InventoryHolder>();
            if (holder == null) return;
            holder.Add(_itemId, _amount);
            Debug.Log($"[GAME] 拾取 {_itemId} x{_amount}，背包共 {holder.Inventory.Total} 件");
            Destroy(gameObject);
        }
    }
}

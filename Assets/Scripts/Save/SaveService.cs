using System.IO;
using UnityEngine;
using Game.Combat;
using Game.Items;

namespace Game.Save
{
    /// <summary>
    /// 挂在玩家上：F5 存档、F9 读档。收集/应用 玩家位置、HP、背包。
    /// 存档文件位于 Application.persistentDataPath/save.json。
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        private Health _health;
        private InventoryHolder _inv;

        private string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

        private void Awake()
        {
            _health = GetComponent<Health>();
            _inv = GetComponent<InventoryHolder>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
            if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
        }

        public void SaveGame()
        {
            var s = new GameState
            {
                playerX = transform.position.x,
                playerY = transform.position.y,
                hp = _health != null ? _health.Current : 0,
            };
            if (_inv != null)
                foreach (var kv in _inv.Inventory.Items)
                    s.items.Add(new ItemStack { id = kv.Key, count = kv.Value });

            SaveSystem.Save(Path, s);
            Debug.Log($"[GAME] 已存档 → {Path}");
        }

        public void LoadGame()
        {
            var s = SaveSystem.Load(Path);
            if (s == null) { Debug.Log("[GAME] 无存档"); return; }

            transform.position = new Vector3(s.playerX, s.playerY, 0f);
            if (_health != null) _health.SetCurrent(s.hp);
            if (_inv != null)
            {
                _inv.Inventory.Clear();
                foreach (var it in s.items) _inv.Inventory.TryAdd(it.id, it.count);
            }
            Debug.Log("[GAME] 已读档");
        }
    }
}

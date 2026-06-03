using UnityEngine;
using Game.Player;
using Game.CameraRig;

namespace Game
{
    /// <summary>
    /// 运行时组合根：进入 Play 时自动搭建竖切片场景（玩家 + 相机），无需手动布线。
    /// 开发者只需按 Play 即可审核。后续里程碑（地图/敌人/HUD）在此扩展生成。
    /// </summary>
    public static class GameRoot
    {
        private static Sprite _unitSquare;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            // 仅在没有玩家时搭建（避免重复 / 与已有场景冲突）
            if (Object.FindObjectOfType<PlayerMovement2D>() != null) return;

            Game.World.MapBuilder.Build(20, 14, GetUnitSquare());
            var player = SpawnPlayer();
            SpawnEnemy(new Vector2(4f, 3f), player.transform);
            SpawnPickups();
            SetupCamera(player.transform);
        }

        private static void SpawnPickups()
        {
            Vector2[] spots = { new Vector2(-3f, -2f), new Vector2(2f, -3f), new Vector2(-5f, 4f), new Vector2(6f, 2f) };
            foreach (var s in spots)
            {
                var go = new GameObject("Pickup");
                go.transform.position = new Vector3(s.x, s.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetUnitSquare();
                sr.color = new Color(0.95f, 0.85f, 0.2f); // 黄色金币
                sr.sortingOrder = 5;
                go.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                go.AddComponent<Game.Items.Pickup>();
            }
        }

        private static GameObject SpawnPlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero; // 地图中心
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = GetUnitSquare();
            sr.color = new Color(0.3f, 0.85f, 0.4f); // 绿色占位
            sr.sortingOrder = 10;
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            player.AddComponent<BoxCollider2D>();

            var hp = player.AddComponent<Game.Combat.Health>();
            hp.Configure(100);
            player.AddComponent<Game.Combat.Attacker>();
            player.AddComponent<Game.Items.InventoryHolder>();

            player.AddComponent<PlayerMovement2D>();
            player.AddComponent<PlayerLife>();
            player.AddComponent<Game.Save.SaveService>();
            return player;
        }

        private static void SpawnEnemy(Vector2 pos, Transform target)
        {
            var enemy = new GameObject("Enemy");
            enemy.transform.position = new Vector3(pos.x, pos.y, 0f);
            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = GetUnitSquare();
            sr.color = new Color(0.85f, 0.3f, 0.3f); // 红色敌人
            sr.sortingOrder = 10;
            enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            enemy.AddComponent<BoxCollider2D>();

            var hp = enemy.AddComponent<Game.Combat.Health>();
            hp.Configure(50);
            var chase = enemy.AddComponent<Game.Enemy.EnemyChase>();
            chase.SetTarget(target);
            hp.OnDied += () => Object.Destroy(enemy);
        }

        private static void SetupCamera(Transform target)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.15f, 0.16f, 0.2f);
            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();
            follow.SetTarget(target);
        }

        /// <summary>生成 1x1 白色方块精灵（占位美术，零资源导入）。</summary>
        private static Sprite GetUnitSquare()
        {
            if (_unitSquare != null) return _unitSquare;
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _unitSquare = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _unitSquare;
        }
    }
}

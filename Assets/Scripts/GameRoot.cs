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

            var player = SpawnPlayer();
            SetupCamera(player.transform);
        }

        private static GameObject SpawnPlayer()
        {
            var player = new GameObject("Player");
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = GetUnitSquare();
            sr.color = new Color(0.3f, 0.85f, 0.4f); // 绿色占位
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            player.AddComponent<PlayerMovement2D>();
            return player;
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

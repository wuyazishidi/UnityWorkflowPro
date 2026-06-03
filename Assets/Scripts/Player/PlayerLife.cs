using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.Combat;

namespace Game.Player
{
    /// <summary>
    /// 玩家生死：死亡时禁用控制并变灰，延迟后在出生点满血重生。
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerLife : MonoBehaviour
    {
        [SerializeField] private float _respawnDelay = 2f;

        private Health _health;
        private Vector3 _spawn;
        private SpriteRenderer _sr;
        private Color _aliveColor;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _sr = GetComponent<SpriteRenderer>();
            _spawn = transform.position;
            _aliveColor = _sr != null ? _sr.color : Color.white;
            _health.OnDied += HandleDied;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            SetControl(false);
            if (_sr != null) _sr.color = Color.gray;
            Debug.Log("[GAME] 玩家死亡，准备重生…");
            RespawnAfterDelay().Forget();
        }

        private async UniTaskVoid RespawnAfterDelay()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_respawnDelay));
            transform.position = _spawn;
            _health.Configure(_health.Max); // 满血并清除死亡标记
            if (_sr != null) _sr.color = _aliveColor;
            SetControl(true);
            Debug.Log("[GAME] 玩家已重生");
        }

        private void SetControl(bool enabled)
        {
            var move = GetComponent<PlayerMovement2D>();
            if (move != null) move.enabled = enabled;
            var atk = GetComponent<Attacker>();
            if (atk != null) atk.enabled = enabled;
        }
    }
}

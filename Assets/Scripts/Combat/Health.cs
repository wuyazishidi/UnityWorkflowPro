using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 通用生命值组件。伤害结算走 <see cref="CombatMath"/>。死亡时触发 OnDied（只触发一次）。
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 100;
        private int _current;
        private bool _dead;

        public int Max => _maxHp;
        public int Current => _current;
        public event Action<int, int> OnChanged; // (current, max)
        public event Action OnDied;

        public void Configure(int maxHp)
        {
            _maxHp = maxHp;
            _current = maxHp;
            _dead = false;
        }

        private void Awake()
        {
            if (_current == 0 && !_dead) _current = _maxHp;
        }

        public void TakeDamage(int amount)
        {
            if (_dead) return;
            _current = CombatMath.ApplyDamage(_current, amount);
            OnChanged?.Invoke(_current, _maxHp);
            if (CombatMath.IsDead(_current))
            {
                _dead = true;
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (_dead || amount <= 0) return;
            _current = Mathf.Min(_maxHp, _current + amount);
            OnChanged?.Invoke(_current, _maxHp);
        }
    }
}

using UnityEngine;
using Game.Enemy;

namespace Game.Combat
{
    /// <summary>
    /// 玩家近战攻击：按 Fire1/空格，对半径内的敌人造成伤害，受冷却限制。
    /// 命中判定/冷却走 CombatMath。
    /// </summary>
    public class Attacker : MonoBehaviour
    {
        [SerializeField] private int _damage = 25;
        [SerializeField] private float _range = 1.4f;
        [SerializeField] private float _cooldown = 0.4f;

        private float _lastAttack = -999f;

        private void Update()
        {
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
            {
                TryAttack();
            }
        }

        public void TryAttack()
        {
            if (!CombatMath.CanAttack(_lastAttack, Time.time, _cooldown)) return;
            _lastAttack = Time.time;

            var hits = Physics2D.OverlapCircleAll(transform.position, _range);
            foreach (var h in hits)
            {
                if (h.GetComponent<EnemyChase>() != null)
                {
                    var hp = h.GetComponent<Health>();
                    if (hp != null) hp.TakeDamage(_damage);
                }
            }
        }
    }
}

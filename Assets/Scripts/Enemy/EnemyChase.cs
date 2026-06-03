using UnityEngine;
using Game.Combat;

namespace Game.Enemy
{
    /// <summary>
    /// 简单敌人：朝玩家追击；接触玩家时按冷却造成伤害。范围/冷却走 CombatMath。
    /// </summary>
    public class EnemyChase : MonoBehaviour
    {
        [SerializeField] private float _speed = 2.2f;
        [SerializeField] private float _contactRange = 0.9f;
        [SerializeField] private int _contactDamage = 10;
        [SerializeField] private float _attackCooldown = 1.0f;

        private Transform _target;
        private Health _targetHealth;
        private Rigidbody2D _rb;
        private float _lastHit = -999f;

        public void SetTarget(Transform target)
        {
            _target = target;
            _targetHealth = target != null ? target.GetComponent<Health>() : null;
        }

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void FixedUpdate()
        {
            if (_target == null) return;

            Vector2 here = _rb != null ? _rb.position : (Vector2)transform.position;
            Vector2 to = (Vector2)_target.position - here;

            if (!CombatMath.InRange(here, _target.position, _contactRange))
            {
                Vector2 step = to.normalized * (_speed * Time.fixedDeltaTime);
                if (_rb != null) _rb.MovePosition(here + step);
                else transform.Translate(step.x, step.y, 0f, Space.World);
            }
            else if (_targetHealth != null && CombatMath.CanAttack(_lastHit, Time.time, _attackCooldown))
            {
                _lastHit = Time.time;
                _targetHealth.TakeDamage(_contactDamage);
            }
        }
    }
}

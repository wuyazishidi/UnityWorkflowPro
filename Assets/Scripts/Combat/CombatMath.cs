using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// 战斗纯逻辑：伤害结算、攻击范围、冷却判定。无副作用、无 Unity 依赖（除 Vector2 数学），可 EditMode 单测。
    /// </summary>
    public static class CombatMath
    {
        /// <summary>扣血并钳制到 [0, +∞)。负伤害视为 0。</summary>
        public static int ApplyDamage(int currentHp, int damage)
        {
            if (damage < 0) damage = 0;
            int result = currentHp - damage;
            return result < 0 ? 0 : result;
        }

        public static bool IsDead(int hp) => hp <= 0;

        /// <summary>目标是否在攻击半径内（用平方距离，避免开方）。</summary>
        public static bool InRange(Vector2 attacker, Vector2 target, float range)
        {
            return (target - attacker).sqrMagnitude <= range * range;
        }

        /// <summary>距上次攻击是否已过冷却。</summary>
        public static bool CanAttack(float lastAttackTime, float now, float cooldown)
        {
            return now - lastAttackTime >= cooldown;
        }
    }
}

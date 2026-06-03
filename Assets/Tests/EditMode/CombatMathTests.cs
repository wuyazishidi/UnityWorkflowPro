using NUnit.Framework;
using UnityEngine;
using Game.Combat;

namespace Game.Tests.EditMode
{
    /// <summary>M3：战斗纯逻辑单测（spec 006）。</summary>
    public class CombatMathTests
    {
        [Test]
        public void ApplyDamage_ClampsAtZero()
        {
            Assert.AreEqual(75, CombatMath.ApplyDamage(100, 25));
            Assert.AreEqual(0, CombatMath.ApplyDamage(10, 25));
            Assert.AreEqual(0, CombatMath.ApplyDamage(0, 5));
        }

        [Test]
        public void ApplyDamage_NegativeDamage_NoHeal()
        {
            Assert.AreEqual(50, CombatMath.ApplyDamage(50, -10));
        }

        [Test]
        public void IsDead_AtOrBelowZero()
        {
            Assert.IsTrue(CombatMath.IsDead(0));
            Assert.IsFalse(CombatMath.IsDead(1));
        }

        [Test]
        public void InRange_UsesRadius()
        {
            Assert.IsTrue(CombatMath.InRange(Vector2.zero, new Vector2(1f, 0f), 1.5f));
            Assert.IsFalse(CombatMath.InRange(Vector2.zero, new Vector2(2f, 0f), 1.5f));
            Assert.IsTrue(CombatMath.InRange(Vector2.zero, new Vector2(1.5f, 0f), 1.5f)); // 边界含
        }

        [Test]
        public void CanAttack_RespectsCooldown()
        {
            Assert.IsTrue(CombatMath.CanAttack(0f, 0.4f, 0.4f));
            Assert.IsFalse(CombatMath.CanAttack(0f, 0.3f, 0.4f));
        }
    }
}

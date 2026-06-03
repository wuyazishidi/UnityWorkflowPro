using NUnit.Framework;
using UnityEngine;
using Game.Combat;

namespace Game.Tests.EditMode
{
    /// <summary>M4：Health 组件行为单测（EditMode 可创建 GameObject 测试无 Play 依赖的逻辑）。</summary>
    public class HealthTests
    {
        private Health NewHealth(int max)
        {
            var go = new GameObject("hp_test");
            var h = go.AddComponent<Health>();
            h.Configure(max);
            return h;
        }

        [Test]
        public void Configure_SetsFull()
        {
            var h = NewHealth(80);
            Assert.AreEqual(80, h.Max);
            Assert.AreEqual(80, h.Current);
            Object.DestroyImmediate(h.gameObject);
        }

        [Test]
        public void TakeDamage_ReducesAndClamps()
        {
            var h = NewHealth(100);
            h.TakeDamage(30);
            Assert.AreEqual(70, h.Current);
            h.TakeDamage(1000);
            Assert.AreEqual(0, h.Current);
            Object.DestroyImmediate(h.gameObject);
        }

        [Test]
        public void OnDied_FiresOnceAtZero()
        {
            var h = NewHealth(20);
            int died = 0;
            h.OnDied += () => died++;
            h.TakeDamage(20);
            h.TakeDamage(5); // 已死，不应再触发
            Assert.AreEqual(1, died);
            Object.DestroyImmediate(h.gameObject);
        }

        [Test]
        public void Heal_ClampsToMax_AndNotAfterDeath()
        {
            var h = NewHealth(50);
            h.TakeDamage(20);
            h.Heal(5);
            Assert.AreEqual(35, h.Current);
            h.Heal(1000);
            Assert.AreEqual(50, h.Current);
            Object.DestroyImmediate(h.gameObject);
        }
    }
}

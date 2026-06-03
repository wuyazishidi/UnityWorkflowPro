using NUnit.Framework;
using Game.Items;

namespace Game.Tests.EditMode
{
    /// <summary>M5：背包纯逻辑单测（spec 008）。</summary>
    public class InventoryTests
    {
        [Test]
        public void TryAdd_NewAndStack()
        {
            var inv = new Inventory(8);
            Assert.IsTrue(inv.TryAdd("coin", 3));
            Assert.IsTrue(inv.TryAdd("coin", 2));
            Assert.AreEqual(5, inv.CountOf("coin"));
            Assert.AreEqual(1, inv.SlotCount);
            Assert.AreEqual(5, inv.Total);
        }

        [Test]
        public void Capacity_RejectsNewSlot_ButAllowsStack()
        {
            var inv = new Inventory(2);
            Assert.IsTrue(inv.TryAdd("a"));
            Assert.IsTrue(inv.TryAdd("b"));
            Assert.IsTrue(inv.IsFull);
            Assert.IsFalse(inv.TryAdd("c"), "满槽应拒绝新物品");
            Assert.IsTrue(inv.TryAdd("a", 5), "已有物品仍可堆叠");
            Assert.AreEqual(6, inv.CountOf("a"));
        }

        [Test]
        public void Remove_DecrementsAndFreesSlot()
        {
            var inv = new Inventory(4);
            inv.TryAdd("potion", 3);
            Assert.IsTrue(inv.Remove("potion", 2));
            Assert.AreEqual(1, inv.CountOf("potion"));
            Assert.IsTrue(inv.Remove("potion", 1));
            Assert.AreEqual(0, inv.SlotCount, "数量归零应释放槽位");
            Assert.IsFalse(inv.Remove("potion", 1), "无库存不可移除");
        }

        [Test]
        public void TryAdd_RejectsInvalid()
        {
            var inv = new Inventory(4);
            Assert.IsFalse(inv.TryAdd("", 1));
            Assert.IsFalse(inv.TryAdd("x", 0));
            Assert.IsFalse(inv.TryAdd("x", -3));
        }
    }
}

using NUnit.Framework;
using Game.Save;

namespace Game.Tests.EditMode
{
    /// <summary>M6：存档序列化往返单测（spec 009）。</summary>
    public class SaveSystemTests
    {
        [Test]
        public void RoundTrip_PreservesScalarFields()
        {
            var s = new GameState { playerX = 1.5f, playerY = -2.25f, hp = 73, score = 42 };
            var back = SaveSystem.Deserialize(SaveSystem.Serialize(s));
            Assert.That(back.playerX, Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That(back.playerY, Is.EqualTo(-2.25f).Within(1e-4f));
            Assert.AreEqual(73, back.hp);
            Assert.AreEqual(42, back.score);
        }

        [Test]
        public void RoundTrip_PreservesItems()
        {
            var s = new GameState { hp = 100 };
            s.items.Add(new ItemStack { id = "coin", count = 7 });
            s.items.Add(new ItemStack { id = "potion", count = 2 });

            var back = SaveSystem.Deserialize(SaveSystem.Serialize(s));
            Assert.AreEqual(2, back.items.Count);
            Assert.AreEqual("coin", back.items[0].id);
            Assert.AreEqual(7, back.items[0].count);
            Assert.AreEqual("potion", back.items[1].id);
            Assert.AreEqual(2, back.items[1].count);
        }

        [Test]
        public void Deserialize_EmptyOrNull_ReturnsNull()
        {
            Assert.IsNull(SaveSystem.Deserialize(null));
            Assert.IsNull(SaveSystem.Deserialize(""));
        }
    }
}

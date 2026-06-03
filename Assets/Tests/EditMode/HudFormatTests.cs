using NUnit.Framework;
using Game.UI;

namespace Game.Tests.EditMode
{
    /// <summary>M8：HUD 格式化纯逻辑单测（spec 011）。</summary>
    public class HudFormatTests
    {
        [Test]
        public void HealthText_Formats()
        {
            Assert.AreEqual("HP 70/100", HudFormat.HealthText(70, 100));
        }

        [Test]
        public void HealthFraction_Clamps01()
        {
            Assert.That(HudFormat.HealthFraction(50, 100), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(HudFormat.HealthFraction(200, 100), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(HudFormat.HealthFraction(-5, 100), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void HealthFraction_ZeroMax_IsZero()
        {
            Assert.That(HudFormat.HealthFraction(10, 0), Is.EqualTo(0f).Within(1e-4f));
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Game.Player;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// PlayerController.ComputeDisplacement 的 EditMode 单元测试。
    /// 对应 specs/001-player-movement.md 的验收标准（移动数学正确、斜向不加速、零输入不动）。
    /// </summary>
    public class PlayerControllerTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void ZeroInput_ProducesNoMovement()
        {
            Vector3 d = PlayerController.ComputeDisplacement(0f, 0f, 5f, 0.1f);
            Assert.That(d.magnitude, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void SingleAxis_ScalesBySpeedAndDeltaTime()
        {
            // 纯前进：位移 = speed * dt = 5 * 0.1 = 0.5，且沿 +Z
            Vector3 d = PlayerController.ComputeDisplacement(0f, 1f, 5f, 0.1f);
            Assert.That(d.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(d.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(d.z, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void DiagonalInput_IsNormalized_NoSpeedBoost()
        {
            // 斜向 (1,1) 输入归一化后位移长度应等于 speed*dt，而不是 sqrt(2) 倍
            Vector3 d = PlayerController.ComputeDisplacement(1f, 1f, 5f, 0.1f);
            Assert.That(d.magnitude, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void SubUnitInput_IsNotScaledUp()
        {
            // 小于单位长度的输入（如手柄轻推）不应被放大到 1
            Vector3 d = PlayerController.ComputeDisplacement(0.3f, 0f, 10f, 1f);
            Assert.That(d.magnitude, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void Movement_IsConstrainedToXZPlane()
        {
            Vector3 d = PlayerController.ComputeDisplacement(0.7f, -0.7f, 3f, 0.2f);
            Assert.That(d.y, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}

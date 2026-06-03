using NUnit.Framework;
using UnityEngine;
using Game.Player;
using Game.CameraRig;

namespace Game.Tests.EditMode
{
    /// <summary>M1：2D 移动与相机跟随的纯逻辑单测（spec 004）。</summary>
    public class PlayerMovement2DTests
    {
        private const float Tol = 1e-4f;

        [Test]
        public void ZeroInput_NoMovement()
        {
            Vector2 d = PlayerMovement2D.ComputeFrameMove(Vector2.zero, 6f, 0.1f);
            Assert.That(d.magnitude, Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void SingleAxis_ScalesBySpeedAndDt()
        {
            Vector2 d = PlayerMovement2D.ComputeFrameMove(new Vector2(1f, 0f), 6f, 0.5f);
            Assert.That(d.x, Is.EqualTo(3f).Within(Tol));
            Assert.That(d.y, Is.EqualTo(0f).Within(Tol));
        }

        [Test]
        public void Diagonal_IsNormalized_NoBoost()
        {
            Vector2 d = PlayerMovement2D.ComputeFrameMove(new Vector2(1f, 1f), 6f, 0.5f);
            Assert.That(d.magnitude, Is.EqualTo(3f).Within(Tol));
        }

        [Test]
        public void SubUnitInput_NotScaledUp()
        {
            Vector2 d = PlayerMovement2D.ComputeFrameMove(new Vector2(0.5f, 0f), 10f, 1f);
            Assert.That(d.magnitude, Is.EqualTo(5f).Within(Tol));
        }

        [Test]
        public void Camera_LocksZDepth_AndTracksXY()
        {
            Vector3 p = CameraFollow2D.ComputeFollowPosition(new Vector3(3f, -2f, 50f), -10f);
            Assert.That(p.x, Is.EqualTo(3f).Within(Tol));
            Assert.That(p.y, Is.EqualTo(-2f).Within(Tol));
            Assert.That(p.z, Is.EqualTo(-10f).Within(Tol));
        }
    }
}

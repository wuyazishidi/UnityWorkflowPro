using System;
using NUnit.Framework;
using Game.Resource;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// ResourceModule 的纯逻辑覆盖。不触碰 YooAsset 运行时（初始化/加载需要资源管线驱动，
    /// 应在 Play 模式由 ResourceBootstrap 审核），这里只钉住可纯函数化的约定。
    /// </summary>
    public class ResourceModuleTests
    {
        [Test]
        public void DefaultPackageName_Is_DefaultPackage()
        {
            Assert.AreEqual("DefaultPackage", ResourceModule.DefaultPackageName);
        }

        [Test]
        public void ValidateLocation_TrimsSurroundingWhitespace()
        {
            Assert.AreEqual("ui/Login", ResourceModule.ValidateLocation("  ui/Login  "));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ValidateLocation_NullOrBlank_Throws(string bad)
        {
            Assert.Throws<ArgumentException>(() => ResourceModule.ValidateLocation(bad));
        }
    }
}

using NUnit.Framework;
using Game.Services;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// GreetingService 的 EditMode 单元测试。
    /// 关键点：可注入的纯逻辑服务无需场景、无需 DI 容器即可直接 new 出来测试——
    /// 这正是引入 VContainer/UniTask 后"可测试架构"的收益（对应 specs/002）。
    /// </summary>
    public class GreetingServiceTests
    {
        private readonly IGreetingService _service = new GreetingService();

        [Test]
        public void Compose_UsesGivenName()
        {
            Assert.AreEqual("Hello, Alice!", _service.Compose("Alice"));
        }

        [Test]
        public void Compose_BlankName_FallsBackToWorld()
        {
            Assert.AreEqual("Hello, World!", _service.Compose("   "));
            Assert.AreEqual("Hello, World!", _service.Compose(null));
            Assert.AreEqual("Hello, World!", _service.Compose(""));
        }

        [Test]
        public void Compose_TrimsSurroundingWhitespace()
        {
            Assert.AreEqual("Hello, Bob!", _service.Compose("  Bob "));
        }
    }
}

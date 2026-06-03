using NUnit.Framework;
using Game.Net;

namespace Game.Tests.EditMode
{
    /// <summary>M7：后端客户端 JSON 构造的纯逻辑单测（spec 010）。</summary>
    public class BackendClientTests
    {
        [Test]
        public void BuildScoreJson_FormatsNameAndScore()
        {
            Assert.AreEqual("{\"name\":\"Bob\",\"score\":42}", BackendClient.BuildScoreJson("Bob", 42));
        }

        [Test]
        public void BuildScoreJson_EscapesQuotesAndBackslash()
        {
            string json = BackendClient.BuildScoreJson("a\"b\\c", 1);
            Assert.AreEqual("{\"name\":\"a\\\"b\\\\c\",\"score\":1}", json);
        }

        [Test]
        public void BuildScoreJson_NullName_BecomesEmptyString()
        {
            Assert.AreEqual("{\"name\":\"\",\"score\":0}", BackendClient.BuildScoreJson(null, 0));
        }
    }
}

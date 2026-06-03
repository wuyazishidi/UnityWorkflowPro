using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode 冒烟测试：验证 PlayMode 测试脚手架可用（能进 Play、走帧、操作 GameObject）。
    /// 后续游戏功能的"行为/集成"验证在此程序集补充。
    /// </summary>
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator GameObject_SurvivesOneFrame()
        {
            var go = new GameObject("smoke");
            yield return null; // 等一帧
            Assert.IsNotNull(go);
            Object.Destroy(go);
        }
    }
}

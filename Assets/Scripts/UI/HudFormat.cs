using UnityEngine;

namespace Game.UI
{
    /// <summary>HUD 文本/数值格式化（纯函数，可 EditMode 单测）。</summary>
    public static class HudFormat
    {
        public static string HealthText(int current, int max) => $"HP {current}/{max}";

        public static float HealthFraction(int current, int max)
        {
            if (max <= 0) return 0f;
            return Mathf.Clamp01((float)current / max);
        }
    }
}

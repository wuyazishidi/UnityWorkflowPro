using System;
using System.Collections.Generic;
using TMPro;

namespace Game.UI
{
    /// <summary>纯函数：对齐字串 → TMP TextAlignmentOptions。大小写不敏感。</summary>
    public static class AlignmentMap
    {
        private static readonly Dictionary<string, TextAlignmentOptions> Map =
            new Dictionary<string, TextAlignmentOptions>(StringComparer.OrdinalIgnoreCase)
            {
                { "Center", TextAlignmentOptions.Center },
                { "Left", TextAlignmentOptions.Left },
                { "Right", TextAlignmentOptions.Right },
                { "Top", TextAlignmentOptions.Top },
                { "Bottom", TextAlignmentOptions.Bottom },
                { "TopLeft", TextAlignmentOptions.TopLeft },
                { "TopRight", TextAlignmentOptions.TopRight },
                { "BottomLeft", TextAlignmentOptions.BottomLeft },
                { "BottomRight", TextAlignmentOptions.BottomRight },
                { "MidlineLeft", TextAlignmentOptions.MidlineLeft },
                { "MidlineRight", TextAlignmentOptions.MidlineRight },
                { "Midline", TextAlignmentOptions.Midline },
                { "Justified", TextAlignmentOptions.Justified },
            };

        public static bool TryGet(string alignment, out TextAlignmentOptions value)
        {
            if (!string.IsNullOrWhiteSpace(alignment))
                return Map.TryGetValue(alignment.Trim(), out value);
            value = TextAlignmentOptions.Center;
            return false;
        }

        /// <summary>未知/空对齐返回 Center，便于导入器容错。</summary>
        public static TextAlignmentOptions GetOr(string alignment, TextAlignmentOptions fallback = TextAlignmentOptions.Center)
            => TryGet(alignment, out var v) ? v : fallback;
    }
}

using UnityEngine;

namespace Game.UI
{
    /// <summary>纯函数：十六进制颜色解析。支持 #RRGGBB / #RRGGBBAA（# 可省略，大小写不敏感）。</summary>
    public static class ColorUtil
    {
        public static bool TryParseHex(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(hex)) return false;

            var s = hex.Trim();
            if (s[0] == '#') s = s.Substring(1);
            if (s.Length != 6 && s.Length != 8) return false;

            if (!TryByte(s, 0, out var r)) return false;
            if (!TryByte(s, 2, out var g)) return false;
            if (!TryByte(s, 4, out var b)) return false;
            byte a = 255;
            if (s.Length == 8 && !TryByte(s, 6, out a)) return false;

            color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }

        /// <summary>解析失败时返回 fallback（不抛异常），便于导入器容错。</summary>
        public static Color ParseHexOr(string hex, Color fallback)
            => TryParseHex(hex, out var c) ? c : fallback;

        private static bool TryByte(string s, int i, out byte value)
        {
            value = 0;
            if (!TryHexDigit(s[i], out var hi)) return false;
            if (!TryHexDigit(s[i + 1], out var lo)) return false;
            value = (byte)((hi << 4) | lo);
            return true;
        }

        private static bool TryHexDigit(char c, out int v)
        {
            if (c >= '0' && c <= '9') { v = c - '0'; return true; }
            if (c >= 'a' && c <= 'f') { v = c - 'a' + 10; return true; }
            if (c >= 'A' && c <= 'F') { v = c - 'A' + 10; return true; }
            v = 0;
            return false;
        }
    }
}

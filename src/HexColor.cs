using System.Globalization;
using UnityEngine;

namespace AdofaiPro
{
    /// <summary>
    /// 解析 ADOFAI 颜色字段用的十六进制字符串（"RRGGBB" 或 "RRGGBBAA"，可带 '#'）。
    /// 自己实现以避免依赖游戏内部的 HexToColor 扩展方法。
    /// </summary>
    internal static class HexColor
    {
        internal static Color Parse(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return fallback;
            }

            string s = hex.Trim();
            if (s.StartsWith("#"))
            {
                s = s.Substring(1);
            }

            if (s.Length != 6 && s.Length != 8)
            {
                return fallback;
            }

            int r, g, b, a = 255;
            if (!TryHex(s.Substring(0, 2), out r)) return fallback;
            if (!TryHex(s.Substring(2, 2), out g)) return fallback;
            if (!TryHex(s.Substring(4, 2), out b)) return fallback;
            if (s.Length == 8 && !TryHex(s.Substring(6, 2), out a)) return fallback;

            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        private static bool TryHex(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
    }
}

using System.Drawing;
using System.Globalization;

namespace discord_web_hook_logger.Extensions
{
    public static class ColorExtension
    {
        public static int ToRgb(this Color color)
        {
            string hex = color.ToHexValue();

            return int.Parse(hex, NumberStyles.HexNumber);
        }

        private static string ToHexValue(this Color color)
        {
            return color.R.ToString("X2") +
                   color.G.ToString("X2") +
                   color.B.ToString("X2");
        }
    }
}
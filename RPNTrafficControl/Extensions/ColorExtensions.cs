using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPNTrafficControl.Extensions
{
    internal static class ColorExtensions
    {
        public static Color Lighten(this Color color)
        {
            int r;
            int g;
            int b;

            if (color.R == 0 && color.G == 0 && color.B == 0)
            {
                r = color.R + 43;
                g = color.G + 43;
                b = color.B + 43;
            }
            else
            {
                r = color.R - 17;
                g = color.G - 17;
                b = color.B - 17;
            }

            return Color.FromArgb(r, g, b);
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;

namespace Mandelbrot
{
    class Colorset
    {
        public Color StartColor { get; private set; }

        public Color EndColor { get; private set; }

        int deltaRed, deltaGreen, deltaBlue;

        public Colorset(Color startColor, Color endColor)
        {
            StartColor = startColor;
            EndColor = endColor;
            deltaRed =   (endColor.R - startColor.R);
            deltaGreen = (endColor.G - startColor.G);
            deltaBlue =  (endColor.B - startColor.B);
        }

        public Color GetColor(double percentage)
        {
            if (percentage < 0 || percentage > 1)
                throw new ArgumentException("Percentage must be between 0 and 1");            

            return Color.FromArgb(StartColor.R + (int)(deltaRed * percentage),
                                  StartColor.G + (int)(deltaGreen * percentage),
                                  StartColor.B + (int)(deltaBlue * percentage));
        }
    }
}

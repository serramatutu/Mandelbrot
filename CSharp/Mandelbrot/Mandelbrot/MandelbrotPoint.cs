using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbrot
{
    class MandelbrotPoint
    {
        public double X { get; private set; }

        public double Y { get; private set; }

        public double Real { get; set; }

        public double Complex { get; set; }

        public MandelbrotPoint(double x, double y)
        {
            X = Real = x;
            Y = Complex = y;
        }

        private bool escaped = false;

        public bool Tick()
        {
            if (escaped)
                return false;

            double aux = Real * Real - Complex * Complex + X;
            Complex = 2 * Real * Complex + Y;
            Real = aux;

            if (Real*Real + Real*Real >= 4)
            {
                escaped = true;
                return true;
            }

            return false;
        }
    }
}

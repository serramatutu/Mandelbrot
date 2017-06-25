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

        public bool Escaped { get; private set; } = false;

        public bool Tick()
        {
            if (Escaped)
                return false;

            double aux = Real * Real - Complex * Complex + X;
            Complex = 2 * Real * Complex + Y;
            Real = aux;

            if (Real * Real + Real * Real >= 4)
            {
                Escaped = true;
                return true;
            }

            return false;
        }

        public int EscapeIteration { get; private set; } = -1;

        public bool Iterate(int iterations)
        {
            for (int i=0; i<iterations; i++)
            {
                double aux = Real * Real - Complex * Complex + X;
                Complex = 2 * Real * Complex + Y;
                Real = aux;

                if (Real * Real + Real * Real >= 4)
                {
                    Escaped = true;
                    EscapeIteration = i;
                    return true;
                }
            }
            return false;
        }
    }
}

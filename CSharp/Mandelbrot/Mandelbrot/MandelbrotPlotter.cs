using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mandelbrot
{
    class MandelbrotPlotter
    {
        public static readonly int THREAD_COUNT = Environment.ProcessorCount;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Iterations { get; private set; }

        public double Zoom { get; private set; }

        public PointD Coords { get; private set; }

        public Colorset Colors { get; private set; }

        private Bitmap plotImg;

        public MandelbrotPlotter(int width, int height, int iterations, double zoom, PointD coords, Colorset colorset)
        {
            if (width * height % THREAD_COUNT != 0)
                throw new ArgumentException("Dimensions must be divisible by "+THREAD_COUNT);

            Width = width;
            Height = height;
            Iterations = iterations;
            Zoom = zoom;
            Coords = coords;
            Colors = colorset;
        }

        public Bitmap Plot() {
            plotImg = new Bitmap(Width, Height);

            MandelbrotPoint[] points = new MandelbrotPoint[Width * Height];

            // Inicia o vetor de pontos
            for (int i=0; i< points.Length; i++)
            {
                double x = ((i % Width) * 3 / Zoom) / Width  - 2 / Zoom + Coords.X;
                double y = ((i / Width) * 2 / Zoom) / Height - 1 / Zoom + Coords.Y;

                points[i] = new MandelbrotPoint(x, y);
            }

            int threadLength = Width * Height / THREAD_COUNT;

            Action[]tasks = new Action[THREAD_COUNT];

            // Obtém o buffer de pixels
            BitmapData data = plotImg.LockBits(new Rectangle(0, 0, plotImg.Width, plotImg.Height), ImageLockMode.ReadWrite, plotImg.PixelFormat);
            int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; // Tamanho de cada pixel em memória, em bytes

            byte[] buffer = new byte[data.Width * data.Height * depth]; // Cria o buffer para se trabalhar na imagem
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length); // Copia as informações da imagem no buffer

            for (int i=0; i < Width * Height; i++) // Inicializa a imagem com todos os pontos escapados na ultima iteração
            {
                int offset = i * depth;

                // É invertido :/
                buffer[offset] = Colors.EndColor.B;
                buffer[offset + 1] = Colors.EndColor.G; 
                buffer[offset + 2] = Colors.EndColor.R;
                buffer[offset + 3] = 255; // Alpha = 255;
            }

            // Inicializa todas as tasks
            for (int taskNumber = 0; taskNumber < THREAD_COUNT; taskNumber++)
            {
                int t = taskNumber;
                tasks[taskNumber] = new Action(() =>
                    {
                        PlotPoints(buffer, t * threadLength, threadLength, depth, points);
                    });
            }

            Parallel.Invoke(tasks);


            // Espera o final de todas as tasks
            //Task.WaitAll(tasks);

            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length); // Copia as informações no buffer de volta à imagem
            plotImg.UnlockBits(data); // Libera a imagem

            return new Bitmap(plotImg); // Clona o retorno
        }

        /// <summary>
        /// Desenha os pontos para uma iteração
        /// </summary>
        /// <param name="iterationColor">Cor desta iteração</param>
        /// <param name="percentage">Porcentagem de completude</param>
        /// <param name="start">Início no vetor</param>
        /// <param name="length">Fim no vetor</param>
        private void PlotPoints(byte[] buffer, int start, int length, int byteDepth, MandelbrotPoint[] points)
        {
            int byteStart = start * byteDepth;

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                Color iterationColor = Colors.GetColor(iteration / (double)Iterations);

                byte red = iterationColor.R;
                byte green = iterationColor.G;
                byte blue = iterationColor.B;

                for (int i = 0; i < length; i++) // Para cada ponto no vetor
                {
                    if (points[i + start].Tick())
                    {
                        var offset = i * byteDepth + byteStart;

                        buffer[offset] = blue;
                        buffer[offset + 1] = green;
                        buffer[offset + 2] = red;
                    }
                }
            } 
        }
    }
}

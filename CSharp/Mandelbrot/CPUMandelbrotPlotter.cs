using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mandelbrot
{
    /// <summary>
    /// Plots the mandelbrot set using all CPU cores
    /// </summary>
    class CPUMandelbrotPlotter : MandelbrotPlotter
    {
        public static new int ThreadCount = Environment.ProcessorCount;

        private Bitmap plotImg;

        public CPUMandelbrotPlotter(int width, int height, int iterations, double zoom, PointD coords, Colorset colorset) : base()
        {
            if (width * height % ThreadCount != 0)
                throw new ArgumentException("Dimensions must be divisible by "+ ThreadCount);

            Width = width;
            Height = height;
            Iterations = iterations;
            Zoom = zoom;
            Coords = coords;
            Colors = colorset;
        }

        public override Bitmap Plot() {
            plotImg = new Bitmap(Width, Height);

            MandelbrotPoint[] points = new MandelbrotPoint[Width * Height];

            // Inicia o vetor de pontos
            for (int i=0; i < points.Length; i++)
            {
                double x = ((i % Width) * 3 / Zoom) / Width  - 2 / Zoom + Coords.X;
                double y = ((i / Width) * 2 / Zoom) / Height - 1 / Zoom + Coords.Y;

                points[i] = new MandelbrotPoint(x, y);
            }

            int threadLength = Width * Height / ThreadCount;

            Action[]tasks = new Action[ThreadCount];

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

            // Reportador de progresso tira a média dos progressos de cada task
            progresses = new float[ThreadCount];
            Progress<ProgressReport> progressReporter = new Progress<ProgressReport>((report) =>
            {
                progresses[report.TaskId] = report.Progress;

                float avg = 0f;
                foreach (float p in progresses)
                    avg += p;

                Progress = avg / progresses.Length;
            });

            // Inicializa todas as tasks
            for (int taskId = 0; taskId < ThreadCount; taskId++)
            {
                int t = taskId;
                tasks[taskId] = new Action(() =>
                    {
                        PlotPoints(t, progressReporter, buffer, t * threadLength, threadLength, depth, points);
                    });
            }

            Parallel.Invoke(tasks);


            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length); // Copia as informações no buffer de volta à imagem
            plotImg.UnlockBits(data); // Libera a imagem

            return new Bitmap(plotImg); // Clona o retorno
        }

        private class ProgressReport
        {
            public int TaskId { get; private set; }

            public float Progress { get; private set; }

            public ProgressReport(int taskId, float progress)
            {
                TaskId = taskId;
                Progress = progress;
            }
        }

        private float[] progresses;

        private void PlotPoints(int taskId, IProgress<ProgressReport> progress, byte[] buffer, int start, int length, int byteDepth, MandelbrotPoint[] points)
        {
            int byteStart = start * byteDepth;
            int escapedPoints = 0;

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                if (progress != null)
                    progress.Report(new ProgressReport(taskId, iteration/(float)Iterations));

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

                        escapedPoints++;
                    }
                }

                if (escapedPoints >= length)
                    return;
            } 
        }
    }
}

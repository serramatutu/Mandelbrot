using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Timers;

namespace Mandelbrot
{
    class Teste
    {
        static string filePath;
        static int iteracoes = 100;
        static double zoom = 1;
        static double coordX = 0;
        static double coordY = 0;
        static int width = 1080;
        static int height = 720;
        static Color startColor = Color.Black,
                     endColor = Color.Blue;

        static bool GPURendering = true;

        static readonly string[] ALLOWED_EXTENSIONS = { ".jpeg", ".jpg", ".png", ".bmp" };

        static MandelbrotPlotter plotter;

        public static void Main(string[] args)
        {
            Initialize();

            Console.CursorVisible = false;
            Console.Clear();
            Console.WriteLine("-- INICIANDO... ");
            Console.WriteLine("   Iteracoes          : " + iteracoes);
            Console.WriteLine("   Width              : " + width);
            Console.WriteLine("   Height             : " + height);
            Console.WriteLine("   Imagem             : ( " + coordX+" ; "+coordY+" )");
            Console.WriteLine("   Zoom               : " + zoom);
            Console.WriteLine("   StartColor (R,G,B) : " + startColor.R+" "+startColor.G+" "+startColor.B);
            Console.WriteLine("   EndColor (R,G,B)   : " + endColor.R + " " + endColor.G + " " + endColor.B);
            //Console.WriteLine("   Threads            : " + CPUMandelbrotPlotter.ThreadCount);
            Console.WriteLine("   Calculando em      : " + (GPURendering ? "GPU" : "CPU"));

            if (GPURendering)
            {
                plotter = new GPUMandelbrotPlotter(width, 
                                                   height, 
                                                   iteracoes, 
                                                   zoom,
                                                   new PointD(coordX, coordY),
                                                   new Colorset(startColor, endColor));
            }
            else
            {
                plotter = new CPUMandelbrotPlotter(width, height,
                                                   iteracoes, // Iterações
                                                   zoom,
                                                   new PointD(coordX, coordY),
                                                   new Colorset(startColor, endColor));
            }

            Stopwatch sw = new Stopwatch();

            // Timer de monitoramento de progresso
            Console.WriteLine();
            System.Timers.Timer timer = new System.Timers.Timer(300);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            // Inicia a plottagem
            sw.Start();
            Bitmap bmp = plotter.Plot();
            sw.Stop();
            timer.Stop();

            // Mensagem de tempo
            Console.WriteLine();
            Console.WriteLine("-- Calculo pronto!");
            Console.WriteLine("   Tempo: " + sw.ElapsedMilliseconds / 1000f + "s");

            // Salva a imagem
            Console.WriteLine();
            Console.WriteLine("-- Salvando em: " + filePath);
            bmp.Save(filePath, GetImageFormat(filePath));

            // Bye bye :)
            Console.WriteLine();
            Console.WriteLine("-- PRONTO!");
            Console.WriteLine("Qualquer tecla para finalizar.");
            Console.ReadKey();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            int p = (int)(plotter.Progress * 100);

            if (p < 0)
                Console.Write("\r Inicializando");
            else
                Console.Write("\r" + p + "% completos     ");
        }

        public static void Initialize()
        {
            Console.WriteLine("--- Mandelbrot Fractal Plotter ---");

            // Filepath
            while (true)
            {
                Console.Write("Filepath: ");
                filePath = Console.ReadLine();

                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    {
                        Console.WriteLine("Diretorio inexistente.");
                        continue;
                    }
                }
                catch
                {
                    Console.WriteLine("Caminho inválido.");
                    continue;
                }
                

                string ext = Path.GetExtension(filePath);
                if (Array.IndexOf(ALLOWED_EXTENSIONS, ext) < 0) // Extensão não permitida
                {
                    Console.WriteLine("Extensão invalida.");
                    continue;
                }

                if (File.Exists(filePath))
                {
                    Console.Write("Ja existe um arquivo com este nome. Sobrescrever? [S/N] ");
                    if (Console.ReadLine().ToUpper() != "S")
                    {
                        continue;
                    }
                }

                break;
            }

            // Iteracoes
            iteracoes = AskInt("Iteracoes", iteracoes);
            coordX = AskDouble("Im(R)", coordX);
            coordY = AskDouble("Im(C)", coordY);
            zoom = AskDouble("Zoom", zoom);
            width = AskInt("Width", width);
            height = AskInt("Height", height);
            startColor = AskColor("Start color", startColor);
            endColor = AskColor("End color", endColor);
            GPURendering = AskInt("GPU (1) - CPU (2) : ", 1) == 1 ? true : false;
        }

        private static double AskDouble(string msg, double def)
        {
            while (true)
            {
                Console.Write(msg + " (default: " + def + ") : ");

                try
                {
                    string input = Console.ReadLine().Trim();
                    if (String.IsNullOrEmpty(input))
                    {
                        return def;
                    }


                    return Convert.ToDouble(input);
                }
                catch
                {
                    Console.WriteLine("Numero invalido.");
                }
            }
        }

        private static int AskInt(string msg, int def)
        {
            while (true)
            {
                Console.Write(msg + " (default: "+def+") : ");

                try
                {
                    string input = Console.ReadLine().Trim();
                    if (String.IsNullOrEmpty(input))
                    {
                        return def;
                    }

                    return Convert.ToInt32(input);
                }
                catch
                {
                    Console.WriteLine("Numero invalido.");
                }
            }
        }

        private static Color AskColor(string msg, Color def)
        {
            while (true)
            {
                Console.Write(msg + " (default: " + def + ") : ");

                try
                {
                    string input = Console.ReadLine().Trim();

                    if (String.IsNullOrEmpty(input))
                    {
                        return def;
                    }
                        

                    string[] rgb = input.Split(' ');

                    if (rgb.Length != 3)
                    {
                        Console.WriteLine("Padrão incorreto. Formato: RRR, GGG, BBB");
                        continue;
                    }

                    byte r = Convert.ToByte(rgb[0]),
                         g = Convert.ToByte(rgb[1]),
                         b = Convert.ToByte(rgb[2]);

                    return Color.FromArgb(r, g, b);
                }
                catch
                {
                    Console.WriteLine("Canal invalido.");
                }
            }
        }

        private static ImageFormat GetImageFormat(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException(
                    string.Format("Unable to determine file extension for fileName: {0}", fileName));

            switch (extension.ToLower())
            {
                case @".bmp":
                    return ImageFormat.Bmp;

                case @".gif":
                    return ImageFormat.Gif;

                case @".ico":
                    return ImageFormat.Icon;

                case @".jpg":
                case @".jpeg":
                    return ImageFormat.Jpeg;

                case @".png":
                    return ImageFormat.Png;

                case @".tif":
                case @".tiff":
                    return ImageFormat.Tiff;

                case @".wmf":
                    return ImageFormat.Wmf;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}

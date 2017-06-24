using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

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

        static readonly string[] ALLOWED_EXTENSIONS = { ".jpeg", ".jpg", ".png", ".bmp" };

        public static void Main(string[] args)
        {
            Initialize();

            Console.Clear();
            Console.WriteLine("-- INICIANDO... ");
            Console.WriteLine("   Iteracoes          : " + iteracoes);
            Console.WriteLine("   Width              : " + width);
            Console.WriteLine("   Height             : " + height);
            Console.WriteLine("   Imagem             : ( " + coordX+" ; "+coordY+" )");
            Console.WriteLine("   Zoom               : " + zoom);
            Console.WriteLine("   StartColor (R,G,B) : " + startColor.R+" "+startColor.G+" "+startColor.B);
            Console.WriteLine("   EndColor (R,G,B)   : " + endColor.R + " " + endColor.G + " " + endColor.B);
            Console.WriteLine("   Threads            : " + MandelbrotPlotter.THREAD_COUNT);

            MandelbrotPlotter m = new MandelbrotPlotter(width, height,
                                                        iteracoes, // Iterações
                                                        zoom, 
                                                        new PointD(coordX, coordY), 
                                                        new Colorset(startColor, endColor));

            Stopwatch sw = new Stopwatch();

            sw.Start();
            Bitmap bmp = m.Plot();
            sw.Stop();

            Console.WriteLine();
            Console.WriteLine("-- Calculo pronto!");
            Console.WriteLine("   Tempo: " + sw.ElapsedMilliseconds / 1000f + "s");

            Console.WriteLine();
            Console.WriteLine("-- Salvando em: " + filePath);

            bmp.Save(filePath);

            Console.WriteLine();
            Console.WriteLine("-- PRONTO!");
            Console.WriteLine("Qualquer tecla para finalizar.");

            Console.ReadKey();
        }

        public static void Initialize()
        {
            Console.WriteLine("--- Mandelbrot Fractal Plotter ---");

            // Filepath
            while (true)
            {
                Console.Write("Filepath: ");
                filePath = Console.ReadLine();

                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                {
                    Console.WriteLine("Diretorio inexistente.");
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
    }
}

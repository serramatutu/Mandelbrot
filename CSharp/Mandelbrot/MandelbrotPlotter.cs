using System.Drawing;

namespace Mandelbrot
{
    abstract class MandelbrotPlotter
    {
        /// <summary>
        /// Quantidade de threads sendo utilizadas para o cálculo
        /// </summary>
        public static int ThreadCount { get; protected set; }

        /// <summary>
        /// Largura da imagem a ser calculada
        /// </summary>
        public int Width { get; protected set; }

        /// <summary>
        /// Altura da imagem a ser calculada
        /// </summary>
        public int Height { get; protected set; }

        /// <summary>
        /// Zoom da imagem a ser calculada
        /// </summary>
        public double Zoom { get; protected set; }

        /// <summary>
        /// Coordenadas da imagem a ser calculada
        /// </summary>
        public PointD Coords { get; protected set; }

        /// <summary>
        /// Quantidade de iterações do cálculo
        /// </summary>
        public int Iterations { get; protected set; }

        /// <summary>
        /// Cores da plottagem
        /// </summary>
        public Colorset Colors { get; protected set; }

        /// <summary>
        /// Indicação de progresso do cálculo
        /// </summary>
        public float Progress { get; protected set; } = -1;

        /// <summary>
        /// Calcula a imagem a partir das especificações
        /// </summary>
        /// <returns>Imagem calculada.</returns>
        public abstract Bitmap Plot();
    }
}

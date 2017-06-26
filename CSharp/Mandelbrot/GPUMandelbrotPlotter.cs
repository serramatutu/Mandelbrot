using System;
using System.Drawing;
using OpenCL.Net;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace Mandelbrot
{
    class GPUMandelbrotPlotter : MandelbrotPlotter
    {
        /// <summary>
        /// Context to run calculations
        /// </summary>
        private Context context;

        private Device device;

        private static string PROGRAM_PATH = "../../MandelbrotGPUKernel.cl";

        /// <summary>
        /// Setup OpenCL for calculation. Chooses device to calculate and creates context
        /// </summary>
        private void Setup()
        {
            ErrorCode error;
            Platform[] platforms = Cl.GetPlatformIDs(out error);
            List<Device> devicesList = new List<Device>();

            CheckErr(error, "Cl.GetPlatformIDs");

            foreach (Platform platform in platforms)
            {
                string platformName = Cl.GetPlatformInfo(platform, PlatformInfo.Name, out error).ToString();
                Console.WriteLine("Platform: " + platformName);
                CheckErr(error, "Cl.GetPlatformInfo");
                
                //We will be looking only for GPU devices
                foreach (Device device in Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error))
                {
                    CheckErr(error, "Cl.GetDeviceIDs");
                    Console.WriteLine("Device: " + device.ToString());
                    devicesList.Add(device);
                }
            }

            if (devicesList.Count <= 0)
            {
                throw new OpenCLException("No devices found.");
            }

            device = devicesList[0];

            if (Cl.GetDeviceInfo(device, DeviceInfo.ImageSupport, out error).CastTo<Bool>() == Bool.False)
            {
                throw new OpenCLException("No image support");
            }

            context = Cl.CreateContext(null, 1, new[] { device }, ContextNotify, IntPtr.Zero, out error);    //Second parameter is amount of devices
            CheckErr(error, "Cl.CreateContext");
        }

        public GPUMandelbrotPlotter(int width, int height, int iterations, double zoom, PointD coords, Colorset colorset)
            : base(width, height, iterations, zoom, coords, colorset)
        {
            Setup();
        }


        public override Bitmap Plot()
        {
            ErrorCode error;

            using (Kernel kernel = CompileKernel("mandelbrot"))
            {
                Bitmap plotImg = new Bitmap(Width, Height);

                int intPtrSize = Marshal.SizeOf(typeof(IntPtr));
                int uint4size = Marshal.SizeOf(typeof(uint4));

                // Buffer do OpenCL para manter os dados da imagem
                OpenCL.Net.ImageFormat clImageFormat = new OpenCL.Net.ImageFormat(ChannelOrder.RGBA, ChannelType.Unsigned_Int8);

                // Obtém o buffer de pixels
                BitmapData data = plotImg.LockBits(new Rectangle(0, 0, plotImg.Width, plotImg.Height), ImageLockMode.ReadWrite, plotImg.PixelFormat);
                int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; // Tamanho de cada pixel em memória, em bytes

                byte[] buffer = new byte[data.Width * data.Height * depth]; // Cria o buffer para se trabalhar na imagem
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length); // Copia as informações da imagem no buffer

                for (int i = 0; i < Width * Height; i++) // Inicializa a imagem com todos os pontos escapados na ultima iteração
                {
                    int offset = i * depth;

                    // É invertido :/
                    buffer[offset] = Colors.EndColor.B;
                    buffer[offset + 1] = Colors.EndColor.G;
                    buffer[offset + 2] = Colors.EndColor.R;
                    buffer[offset + 3] = 255; // Alpha = 255;
                }

                // Cria o buffer do OpenCL para a imagem
                Mem image2dbuffer = (Mem)Cl.CreateImage2D(context, MemFlags.UseHostPtr | MemFlags.WriteOnly, clImageFormat,
                                                         (IntPtr)data.Width, (IntPtr)data.Height,
                                                         (IntPtr)0, buffer, out error);
                CheckErr(error, "Cl.CreateImage2D");

                // Passa os parametros para o kernel
                error = Cl.SetKernelArg(kernel, 0, (IntPtr)intPtrSize, image2dbuffer);
                CheckErr(error, "Cl.SetKernelArg imageBuffer");

                uint4 startColorUi = new uint4(Colors.StartColor.B, Colors.StartColor.G, Colors.StartColor.R, Colors.StartColor.A);
                error = Cl.SetKernelArg(kernel, 1, (IntPtr)uint4size, startColorUi);
                CheckErr(error, "Cl.SetKernelArg startColor");

                uint4 endColorUi = new uint4(Colors.EndColor.B, Colors.EndColor.G, Colors.EndColor.R, Colors.EndColor.A);
                error = Cl.SetKernelArg(kernel, 2, (IntPtr)uint4size, endColorUi);
                CheckErr(error, "Cl.SetKernelArg endColor");

                error = Cl.SetKernelArg(kernel, 3, (IntPtr)sizeof(int), Iterations);
                CheckErr(error, "Cl.SetKernelArg iterations");


                // Cria uma fila de comandos, com todos os comandos a serem executados pelo kernel
                CommandQueue cmdQueue = Cl.CreateCommandQueue(context, device, 0, out error);
                CheckErr(error, "Cl.CreateCommandQueue");

                // Copia a imagem para a GPU
                Event clevent;
                IntPtr[] imgOriginPtr = new IntPtr[] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };    //x, y, z
                IntPtr[] imgRegionPtr = new IntPtr[] { (IntPtr)plotImg.Width, (IntPtr)plotImg.Height, (IntPtr)1 };    //x, y, z
                error = Cl.EnqueueWriteImage(cmdQueue, image2dbuffer, Bool.True, imgOriginPtr, imgRegionPtr, (IntPtr)0, (IntPtr)0, buffer, 0, null, out clevent);
                CheckErr(error, "Cl.EnqueueWriteImage");

                // Executa o Kernel carregado pelo OpenCL (com múltiplos processadores :D)
                IntPtr[] workGroupSizePtr = new IntPtr[] { (IntPtr)plotImg.Width, (IntPtr)plotImg.Height, (IntPtr)1 }; // x, y, z
                error = Cl.EnqueueNDRangeKernel(cmdQueue, kernel, 2, null, workGroupSizePtr, null, 0, null, out clevent);
                CheckErr(error, "Cl.EnqueueNDRangeKernel");

                // Espera terminar a execução
                error = Cl.Finish(cmdQueue);
                CheckErr(error, "Cl.Finish");

                //Read the processed image from GPU to raw RGBA data byte[] array
                error = Cl.EnqueueReadImage(cmdQueue, image2dbuffer, Bool.True, imgOriginPtr, imgRegionPtr,
                                            (IntPtr)0, (IntPtr)0, buffer, 0, null, out clevent);
                CheckErr(error, "Cl.clEnqueueReadImage");

                // Limpa a memória
                Cl.ReleaseKernel(kernel);
                Cl.ReleaseCommandQueue(cmdQueue);
                Cl.ReleaseMemObject(image2dbuffer);

                // Get a pointer to our unmanaged output byte[] array
                //GCHandle pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                //IntPtr bmpPointer = pinnedBuffer.AddrOfPinnedObject();

                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length); // Copia as informações no buffer de volta à imagem
                plotImg.UnlockBits(data); // Libera a imagem

                //pinnedOutputArray.Free();

                return plotImg;
            }
        }

        private Kernel CompileKernel(string kernelName)
        {
            ErrorCode error;

            if (!File.Exists(PROGRAM_PATH))
                throw new IOException("Program does not exist at path.");

            string programSource = File.ReadAllText(PROGRAM_PATH);

            using (Program program = Cl.CreateProgramWithSource(context, 1, new[] { programSource }, null, out error))
            {
                CheckErr(error, "Cl.CreateProgramWithSource");
                
                //Compile kernel source
                error = Cl.BuildProgram(program, 1, new[] { device }, "-Werror", null, IntPtr.Zero);
                InfoBuffer log = Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Log, out error);
                Console.WriteLine(log);
                CheckErr(error, "Cl.BuildProgram");

                //Check for any compilation errors
                if (Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Status, out error).CastTo<BuildStatus>() != BuildStatus.Success)
                {
                    CheckErr(error, "Cl.GetProgramBuildInfo");
                }

                //Create the required kernel (entry function)
                Kernel kernel = Cl.CreateKernel(program, kernelName, out error);
                CheckErr(error, "Cl.CreateKernel");

                return kernel;
            }        
        }

        private void CheckErr(ErrorCode err, string name)
        {
            if (err != ErrorCode.Success)
            {
                throw new OpenCLException("OpenCL error: " + name + "(" + err.ToString() + ")");
            }
        }

        private void ContextNotify(string errInfo, byte[] data, IntPtr cb, IntPtr userData)
        {
            Console.WriteLine("OpenCL Notification: " + errInfo);
        }
    }

    class OpenCLException : Exception
    {
        public OpenCLException() : base()
        { }

        public OpenCLException(string msg) : base(msg)
        { }
    }
}

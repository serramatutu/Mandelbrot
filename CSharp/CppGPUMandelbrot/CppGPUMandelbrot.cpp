// CppGPUMandelbrot.cpp : Defines the entry point for the console application.
//

#include <iostream>
#include <fstream>
using namespace std;

#include "stdafx.h"
#include <CL/opencl.h>


#define KERNEL_PATH "mandelbrot_gpu_kernel.cl"

void loadProgram();



int main()
{
    return 0;
}

void loadProgram() {
	// Lê o arquivo do kernel
	fstream kernelFile;
	kernelFile.open(KERNEL_PATH, fstream::out);
	char* kernelCode;
	kernelFile >> kernelCode;
	kernelFile.close();


}
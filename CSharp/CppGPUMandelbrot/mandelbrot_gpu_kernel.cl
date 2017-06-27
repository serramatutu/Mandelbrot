__kernel void mandelbrot(__write_only image2d_t img, uint4 startColor, uint4 endColor, int iterations) {
	const int width = get_global_size(0);
	const int2 coord = (int2)(get_global_id(0), get_global_id(1));

	float real = (float)coord.x;
	float complex = (float)coord.y;

	uint4 color = endColor;

	for (int i=0; i<iterations; i++) {
		float aux = real * real - complex * complex + coord.x;
        complex = 2 * real * complex + coord.y;
        real = aux;

        if (real * real + complex * complex >= 4) // Se escapou do conjunto
        {
			float percentage = i/(float)iterations;

			// printf("Alpha: %f", percentage);

			color = (uint4)( 
							 (startColor.w + (endColor.w - startColor.w) * percentage), // Red
							 (startColor.x + (endColor.x - startColor.x) * percentage), // Green
							 (startColor.y + (endColor.y - startColor.y) * percentage), // Blue
							 (startColor.z + (endColor.z - startColor.z) * percentage)  // Alpha
							);
			break;
        }
	}

	write_imageui(img, coord, color); //ui por conta do uint4
}
/*
* This code is taken more or less entirely from the NVIDIA CUDA SDK.
* This software contains source code provided by NVIDIA Corporation.
*
*/
//https://github.com/kunzmi/managedCuda/wiki/Setup-a-managedCuda-project

//Includes for IntelliSense 
#define _SIZE_T_DEFINED
#ifndef __CUDACC__
#define __CUDACC__
#endif
#ifndef __cplusplus
#define __cplusplus
#endif

#include <cuda.h>
#include <device_launch_parameters.h>
#include <texture_fetch_functions.h>
#include "float.h"
#include <builtin_types.h>
#include <vector_functions.h>


extern "C" {
	// Device code
	/*IDE will complain about __constant__ and other cuda keywords*/
	__constant__ int maxVal;
	__constant__ int n;
	__constant__ int n2;
	__constant__ int n3;

	__device__ void gainGrainPositive(int changedDimension, int coordNeighbour, int coord, const int* origin, int* delta)
	{
		if (changedDimension < n) {
			if (coordNeighbour < n3 && origin[coordNeighbour] >= maxVal) {
				delta[coord]++;
			}
		}
	}

	__device__ void gainGrainNegative(int changedDimension, int coordNeighbour, int coord, const int* origin, int* delta)
	{
		if (changedDimension >= 0) {
			if (coordNeighbour >= 0 && origin[coordNeighbour] >= maxVal) {
				delta[coord]++;
			}
		}
	}

	__global__ void CalculateSandpilesDelta(const int* origin, int* delta, int* nextIteration)
	{
		int x = blockDim.x * blockIdx.x + threadIdx.x;
		int y = blockDim.y * blockIdx.y + threadIdx.y;
		int z = blockDim.z * blockIdx.z + threadIdx.z;
		int coord = x * n * n + y * n + z;

		if (origin[coord] >= maxVal)
		{
			delta[coord] -= maxVal;
		}
		/*Bug which should be tried out visually: Let coordinates individual x,y,z component be negative and continue with flow */

		int xN = x - 1;
		int xP = x + 1;
		int yN = y - 1;
		int yP = y + 1;
		int zN = z - 1;
		int zP = z + 1;

		int coordL = xN * n2 + y * n + z;
		int coordR = xP * n2 + y * n + z; //these calculated unecessarily if inbounds is false, possible optimization
		int coordD = x * n2 + yN * n + z;
		int coordU = x * n2 + yP * n + z;
		int coordB = x * n2 + y * n + zN;
		int coordF = x * n2 + y * n + zP;

		gainGrainNegative(xN, coordL, coord, origin, delta);
		gainGrainPositive(xP, coordR, coord, origin, delta);
		gainGrainNegative(yN, coordD, coord, origin, delta);
		gainGrainPositive(yP, coordU, coord, origin, delta);
		gainGrainNegative(zN, coordB, coord, origin, delta);
		gainGrainPositive(zP, coordF, coord, origin, delta);

		nextIteration[coord] = origin[coord] + delta[coord];


		//if (xP >= 0) { // this more effiecient i think, but scrapped for readability
		//  int coordR = xP * n2 + y * n + z;
		//	if (coordR >= 0 && origin[coordR] >= maxVal) {
		//		delta[coord]++;
		//	}
		//}
		/*if (xN < n) {
		if (coordL < n3 && origin[coordL] >= maxVal) {
		delta[coord]++;
		}
		}

		if (coordU < n3 && origin[coordU] >= maxVal) {
		delta[coord]++;
		}
		if (coordD >= 0 && origin[coordD] >= maxVal) {
		delta[coord]++;
		}
		if (coordB >= 0 && origin[coordB] >= maxVal) {
		delta[coord]++;
		}
		if (coordF < n3 && origin[coordF] >= maxVal) {
		delta[coord]++;
		}*/

	}

	__global__ void CalculateSandpilesDeltaThreadPerZColumn(const int* origin, int* delta, int* nextIteration)
	{
		int x = blockDim.x * blockIdx.x + threadIdx.x;
		int y = blockDim.y * blockIdx.y + threadIdx.y;
		int z = 0;
		while (z < n) {
			int coord = x * n * n + y * n + z;

			if (origin[coord] >= maxVal)
			{
				delta[coord] -= maxVal;
			}
			int xN = x - 1;
			int xP = x + 1;
			int yN = y - 1;
			int yP = y + 1;
			int zN = z - 1;
			int zP = z + 1;

			int coordL = xN * n2 + y * n + z;
			int coordR = xP * n2 + y * n + z; //these calculated unecessarily if inbounds is false, possible optimization
			int coordD = x * n2 + yN * n + z;
			int coordU = x * n2 + yP * n + z;
			int coordB = x * n2 + y * n + zN;
			int coordF = x * n2 + y * n + zP;

			gainGrainNegative(xN, coordL, coord, origin, delta);
			gainGrainPositive(xP, coordR, coord, origin, delta);
			gainGrainNegative(yN, coordD, coord, origin, delta);
			gainGrainPositive(yP, coordU, coord, origin, delta);
			gainGrainNegative(zN, coordB, coord, origin, delta);
			gainGrainPositive(zP, coordF, coord, origin, delta);

			nextIteration[coord] = origin[coord] + delta[coord];

			z++;
		}
	}

	__global__ void CalculateSandpilesDeltaThreadPerZColumnOptimized(const int* origin, int* delta, int* nextIteration)
	{
		int x = blockDim.x * blockIdx.x + threadIdx.x;
		int y = blockDim.y * blockIdx.y + threadIdx.y;
		int z = 0;
		while (z < n) {
			int coord = x * n2 + y * n + z;

			if (origin[coord] >= maxVal)
			{
				delta[coord] -= maxVal;
			}
			int xN = x - 1;
			int xP = x + 1;
			int yN = y - 1;
			int yP = y + 1;
			int zN = z - 1;
			int zP = z + 1;

			if (xN >= 0) { // possible optimization as the X term is the biggest we do not need to check it 
				int coordL = xN * n2 + y * n + z;
				if (coordL >= 0 && origin[coordL] >= maxVal) {
					delta[coord]++;
				}
			}
			if (xP < n) {
				int coordR = xP * n2 + y * n + z;
				if (coordR < n3 && origin[coordR] >= maxVal) {
					delta[coord]++;
				}
			}
			if (yN >= 0) {
				int coordD = x * n2 + yN * n + z;
				if (coordD >= 0 && origin[coordD] >= maxVal) {
					delta[coord]++;
				}
			}
			if (yP < n) {
				int coordU = x * n2 + yP * n + z;
				if (coordU < n3 && origin[coordU] >= maxVal) {
					delta[coord]++;
				}
			}
			if (zN >= 0) {
				int coordB = x * n2 + y * n + zN;
				if (coordB >= 0 && origin[coordB] >= maxVal) {
					delta[coord]++;
				}
			}
			if (zP < n) {
				int coordF = x * n2 + y * n + zP;
				if (coordF < n3 && origin[coordF] >= maxVal) {
					delta[coord]++;
				}
			}
			nextIteration[coord] = origin[coord] + delta[coord];

			z++;
		}
	}

}
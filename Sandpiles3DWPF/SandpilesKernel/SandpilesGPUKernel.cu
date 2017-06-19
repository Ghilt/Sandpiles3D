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
	__constant__ int side;
	__constant__ int depth;
	__constant__ int sideTimesDepth;
	__constant__ int size;

	__global__ void CalculateSandpilesDeltaThreadPerZColumnOptimized(const int* origin, int* delta, int* nextIteration)
	{
		int x = blockDim.x * blockIdx.x + threadIdx.x;
		int y = blockDim.y * blockIdx.y + threadIdx.y;
		int coordPart = x * sideTimesDepth + y * depth;
		int xN = x - 1;
		int xP = x + 1;
		int yN = y - 1;
		int yP = y + 1;
		bool xNInBounds = xN >= 0;
		bool xPInBounds = xP < side;
		bool yNInBounds = yN >= 0;
		bool yPInBounds = yP < side;
		int xNCoordPart = xN * sideTimesDepth + y * depth;
		int xPCoordPart = xP * sideTimesDepth + y * depth;
		int yNCoordPart = x * sideTimesDepth + yN * depth;
		int yPCoordPart = x * sideTimesDepth + yP * depth;
		int z = 0;
		while (z < depth) {
			int coord = coordPart + z;
			int zN = z - 1;
			int zP = z + 1;

			if (origin[coord] >= maxVal)
			{
				delta[coord] -= maxVal;
			}

			if (xNInBounds) { // possible optimization as the X term is the biggest we do not need to check it 
				int coordL = xNCoordPart + z;
				if (coordL >= 0 && origin[coordL] >= maxVal) {
					delta[coord]++;
				}
			}
			if (xPInBounds) {
				int coordR = xPCoordPart + z;
				if (coordR < size && origin[coordR] >= maxVal) {
					delta[coord]++;
				}
			}
			if (yNInBounds) {
				int coordD = yNCoordPart + z;
				if (coordD >= 0 && origin[coordD] >= maxVal) {
					delta[coord]++;
				}
			}
			if (yPInBounds) {
				int coordU = yPCoordPart + z;
				if (coordU < size && origin[coordU] >= maxVal) {
					delta[coord]++;
				}
			}
			if (zN >= 0) {
				int coordB = coordPart + zN;
				if (coordB >= 0 && origin[coordB] >= maxVal) {
					delta[coord]++;
				}
			}
			if (zP < depth) {
				int coordF = coordPart + zP;
				if (coordF < size && origin[coordF] >= maxVal) {
					delta[coord]++;
				}
			}
			nextIteration[coord] = origin[coord] + delta[coord];

			z++;
		}
	}

	/*
	Comment block below was other prettier versions for experimenting with speeds/optimization
	They have NOT been updated to accomodate a different depth for the z parameter
	*/
	//__device__ void gainGrainPositive(int changedDimension, int coordNeighbour, int coord, const int* origin, int* delta)
	//{
	//	if (changedDimension < side) {
	//		if (coordNeighbour < size && origin[coordNeighbour] >= maxVal) {
	//			delta[coord]++;
	//		}
	//	}
	//}

	//__device__ void gainGrainNegative(int changedDimension, int coordNeighbour, int coord, const int* origin, int* delta)
	//{
	//	if (changedDimension >= 0) {
	//		if (coordNeighbour >= 0 && origin[coordNeighbour] >= maxVal) {
	//			delta[coord]++;
	//		}
	//	}
	//}

	//__global__ void CalculateSandpilesDelta(const int* origin, int* delta, int* nextIteration)
	//{
	//	int x = blockDim.x * blockIdx.x + threadIdx.x;
	//	int y = blockDim.y * blockIdx.y + threadIdx.y;
	//	int z = blockDim.z * blockIdx.z + threadIdx.z;
	//	int coord = x * side * side + y * side + z;

	//	if (origin[coord] >= maxVal)
	//	{
	//		delta[coord] -= maxVal;
	//	}
	//	/*Bug which should be tried out visually: Let coordinates individual x,y,z component be negative and continue with flow */

	//	int xN = x - 1;
	//	int xP = x + 1;
	//	int yN = y - 1;
	//	int yP = y + 1;
	//	int zN = z - 1;
	//	int zP = z + 1;

	//	int coordL = xN * sideTimesDepth + y * side + z;
	//	int coordR = xP * sideTimesDepth + y * side + z; //these calculated unecessarily if inbounds is false, possible optimization
	//	int coordD = x * sideTimesDepth + yN * side + z;
	//	int coordU = x * sideTimesDepth + yP * side + z;
	//	int coordB = x * sideTimesDepth + y * side + zN;
	//	int coordF = x * sideTimesDepth + y * side + zP;

	//	gainGrainNegative(xN, coordL, coord, origin, delta);
	//	gainGrainPositive(xP, coordR, coord, origin, delta);
	//	gainGrainNegative(yN, coordD, coord, origin, delta);
	//	gainGrainPositive(yP, coordU, coord, origin, delta);
	//	gainGrainNegative(zN, coordB, coord, origin, delta);
	//	gainGrainPositive(zP, coordF, coord, origin, delta);

	//	nextIteration[coord] = origin[coord] + delta[coord];


	//	//if (xP >= 0) { // this more effiecient i think, but scrapped for readability
	//	//  int coordR = xP * n2 + y * n + z;
	//	//	if (coordR >= 0 && origin[coordR] >= maxVal) {
	//	//		delta[coord]++;
	//	//	}
	//	//}
	//	/*if (xN < n) {
	//	if (coordL < n3 && origin[coordL] >= maxVal) {
	//	delta[coord]++;
	//	}
	//	}

	//	if (coordU < n3 && origin[coordU] >= maxVal) {
	//	delta[coord]++;
	//	}
	//	if (coordD >= 0 && origin[coordD] >= maxVal) {
	//	delta[coord]++;
	//	}
	//	if (coordB >= 0 && origin[coordB] >= maxVal) {
	//	delta[coord]++;
	//	}
	//	if (coordF < n3 && origin[coordF] >= maxVal) {
	//	delta[coord]++;
	//	}*/

	//}

	//__global__ void CalculateSandpilesDeltaThreadPerZColumn(const int* origin, int* delta, int* nextIteration)
	//{
	//	int x = blockDim.x * blockIdx.x + threadIdx.x;
	//	int y = blockDim.y * blockIdx.y + threadIdx.y;
	//	int z = 0;
	//	while (z < side) {
	//		int coord = x * side * side + y * side + z;

	//		if (origin[coord] >= maxVal)
	//		{
	//			delta[coord] -= maxVal;
	//		}
	//		int xN = x - 1;
	//		int xP = x + 1;
	//		int yN = y - 1;
	//		int yP = y + 1;
	//		int zN = z - 1;
	//		int zP = z + 1;

	//		int coordL = xN * sideTimesDepth + y * side + z;
	//		int coordR = xP * sideTimesDepth + y * side + z; //these calculated unecessarily if inbounds is false, possible optimization
	//		int coordD = x * sideTimesDepth + yN * side + z;
	//		int coordU = x * sideTimesDepth + yP * side + z;
	//		int coordB = x * sideTimesDepth + y * side + zN;
	//		int coordF = x * sideTimesDepth + y * side + zP;

	//		gainGrainNegative(xN, coordL, coord, origin, delta);
	//		gainGrainPositive(xP, coordR, coord, origin, delta);
	//		gainGrainNegative(yN, coordD, coord, origin, delta);
	//		gainGrainPositive(yP, coordU, coord, origin, delta);
	//		gainGrainNegative(zN, coordB, coord, origin, delta);
	//		gainGrainPositive(zP, coordF, coord, origin, delta);

	//		nextIteration[coord] = origin[coord] + delta[coord];

	//		z++;
	//	}
	//}

}
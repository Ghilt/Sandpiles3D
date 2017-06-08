using ManagedCuda;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sandpiles3DWPF.Model.Cuda
{
    class SandpilesCalculatorCuda : SandpilesCalculator
    {
        public static readonly int[] AVAILABLE_CUDA_DIMENSIONS = new int[] { 32, 64, 128, 256 };

        private const string KERNEL_LOCATION = "Sandpiles3DWPF.SandpilesGPUKernel.ptx";
        private const string KERNEL_METHOD_NORMAL = "CalculateSandpilesDeltaThreadPerZColumn";

        private const string KERNEL_PARAMTER_MAX_VALUE = "maxVal";
        private const string KERNEL_PARAMTER_SIZE = "n";
        private const string KERNEL_PARAMTER_SIZE2 = "n2";
        private const string KERNEL_PARAMTER_SIZE3 = "n3";

        private CudaContext ctx;
        private CudaKernel kernel;
        private Stream stream;
        private CudaDeviceVariable<int> d_origin;
        private CudaDeviceVariable<int> d_delta;
        private CudaDeviceVariable<int> d_nextIteration;
        private int n;
        private int n2;
        private int n3;

        public SandpilesCalculatorCuda(PropertyChangedEventHandler propertyChangedListener, int width, int height, int depth) : base(propertyChangedListener, width, height, depth)
        {
            stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(KERNEL_LOCATION);
            if (stream == null) throw new ArgumentException("Kernel not found in resources. " + KERNEL_LOCATION);

            n = 256;
            n2 = n * n;
            n3 = n2 * n;
        }

        internal void loadKernel()
        {
            ctx = new CudaContext(CudaContext.GetMaxGflopsDeviceId());
            kernel = ctx.LoadKernelPTX(stream, KERNEL_METHOD_NORMAL);

            ManagedCuda.VectorTypes.dim3 threadsPerBlock = new ManagedCuda.VectorTypes.dim3(4, 4);
            kernel.BlockDimensions = threadsPerBlock;
            kernel.GridDimensions = new ManagedCuda.VectorTypes.dim3(64, 64);
            kernel.SetConstantVariable(KERNEL_PARAMTER_MAX_VALUE, MAX_AMOUNT);
            kernel.SetConstantVariable(KERNEL_PARAMTER_SIZE, n);
            kernel.SetConstantVariable(KERNEL_PARAMTER_SIZE2, n2);
            kernel.SetConstantVariable(KERNEL_PARAMTER_SIZE3, n3);
            // 1024 * x * x = n^2 
            // n^2/threads = x^2

        }

        internal void disposeKernel()
        {
            ctx.Dispose();
        }

        public override void Iterate()
        {
            int[] h_origin = space;
            int[] h_delta;
            int[] h_nextIteration;
            d_origin = h_origin;
            d_delta = new CudaDeviceVariable<int>(n3);
            d_nextIteration = new CudaDeviceVariable<int>(n3);
            float elapsed = kernel.Run(d_origin.DevicePointer, d_delta.DevicePointer, d_nextIteration.DevicePointer);

            h_delta = d_delta;
            h_nextIteration = d_nextIteration;

            space = h_nextIteration;
            delta = h_delta;

            d_origin.Dispose();
            d_delta.Dispose();
            d_nextIteration.Dispose();
        }
    }
}

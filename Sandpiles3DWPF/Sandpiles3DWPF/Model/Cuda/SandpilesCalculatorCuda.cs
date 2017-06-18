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
    class SandpilesCalculatorCuda : AbstractSandpilesCalculator
    {
        public static readonly CudaKernelDimensions[] AVAILABLE_CUDA_DIMENSIONS = new CudaKernelDimensions[] {
            new CudaKernelDimensions(32, 2, 16),
            new CudaKernelDimensions(64, 2, 32),
            new CudaKernelDimensions(128, 4, 32),
            new CudaKernelDimensions(256, 4, 64)
        };

        private const string KERNEL_LOCATION = "Sandpiles3DWPF.SandpilesGPUKernel.ptx";
        private const string KERNEL_METHOD_NORMAL = "CalculateSandpilesDeltaThreadPerZColumnOptimized";

        private const string KERNEL_PARAMTER_MAX_VALUE = "maxVal";
        private const string KERNEL_PARAMTER_SIDE = "side";
        private const string KERNEL_PARAMTER_DEPTH = "depth";
        private const string KERNEL_PARAMTER_SIDE_TIMES_DEPTH = "sideTimesDepth";
        private const string KERNEL_PARAMTER_SIZE = "size";

        private CudaContext ctx;
        private CudaKernel kernel;
        private Stream stream;
        private CudaDeviceVariable<int> d_origin;
        private CudaDeviceVariable<int> d_delta;
        private CudaDeviceVariable<int> d_nextIteration;

        int size;
        int side;

        public SandpilesCalculatorCuda(PropertyChangedEventHandler propertyChangedListener, int width, int height, int depth) : base(propertyChangedListener, width, height, depth)
        {
            stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(KERNEL_LOCATION);
            if (stream == null) throw new ArgumentException("Cuda kernel not found in resources. " + KERNEL_LOCATION);
            if (width != height) throw new ArgumentException("Cuda kernel only supports width == height, width:" + width + ", height: " + height);
            size = width * height * depth;
            side = width;
        }

        public override void PreIterate()
        {
            ctx = new CudaContext(CudaContext.GetMaxGflopsDeviceId());
            kernel = ctx.LoadKernelPTX(stream, KERNEL_METHOD_NORMAL);

            CudaKernelDimensions ckd = AVAILABLE_CUDA_DIMENSIONS.First(x => x.dataDimensions == side);

            ManagedCuda.VectorTypes.dim3 threadsPerBlock = new ManagedCuda.VectorTypes.dim3(ckd.threadsPerBlock, ckd.threadsPerBlock);
            kernel.BlockDimensions = threadsPerBlock;
            kernel.GridDimensions = new ManagedCuda.VectorTypes.dim3(ckd.gridDimensions, ckd.gridDimensions);
            kernel.SetConstantVariable(KERNEL_PARAMTER_MAX_VALUE, MAX_AMOUNT);
            kernel.SetConstantVariable(KERNEL_PARAMTER_SIDE, side);
            kernel.SetConstantVariable(KERNEL_PARAMTER_DEPTH, depth);
            kernel.SetConstantVariable(KERNEL_PARAMTER_SIDE_TIMES_DEPTH, side * depth);
            kernel.SetConstantVariable(KERNEL_PARAMTER_SIZE, side * height * depth);

        }

        public override void PostIterate()
        {
            ctx.Dispose();
        }

        public override IterationResult PerformIterate(int[] space, int[] delta)
        {
            int[] h_origin = space;
            int[] h_delta;
            int[] h_nextIteration;
            d_origin = h_origin;
            d_delta = new CudaDeviceVariable<int>(size);
            d_nextIteration = new CudaDeviceVariable<int>(size);
            float elapsed = kernel.Run(d_origin.DevicePointer, d_delta.DevicePointer, d_nextIteration.DevicePointer);

            h_nextIteration = d_nextIteration;
            h_delta = d_delta;

            IterationResult result = new IterationResult(h_nextIteration, h_delta);

            d_origin.Dispose();
            d_delta.Dispose();
            d_nextIteration.Dispose();
            return result;
        }

        public bool CheckCudaAvailable()
        {
            try
            {
                ctx = new CudaContext(CudaContext.GetMaxGflopsDeviceId());
                ctx.Dispose();

            } catch (Exception)
            {
                return false;
            }
            return true;
        }

    }
}

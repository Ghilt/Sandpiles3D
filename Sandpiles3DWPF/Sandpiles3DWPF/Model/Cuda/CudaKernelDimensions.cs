using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandpiles3DWPF.Model.Cuda
{
    class CudaKernelDimensions { 
        public int dataDimensions { get; private set; }
        public int threadsPerBlock { get; private set; }
        public int gridDimensions { get; private set; }

        public CudaKernelDimensions(int dataDimensions, int threadsPerBlock, int gridDimensions)
        {
            this.dataDimensions = dataDimensions;
            this.threadsPerBlock = threadsPerBlock;
            this.gridDimensions = gridDimensions;
        }

        public override string ToString()
        {
            return dataDimensions + "";
        }
    }
}

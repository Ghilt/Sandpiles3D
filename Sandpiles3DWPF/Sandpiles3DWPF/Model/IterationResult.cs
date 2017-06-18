using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandpiles3DWPF.Model
{
    public class IterationResult
    {
        public int[] Data { get; private set; }
        public int[] Delta { get; private set; }

        public IterationResult(int[] data, int[] delta)
        {
            this.Data = data;
            this.Delta = delta;
        }
    }
}

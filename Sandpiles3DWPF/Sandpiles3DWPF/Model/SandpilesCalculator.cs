using Sandpiles3DWPF.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Sandpiles3DWPF.Extensions;

namespace Sandpiles3DWPF.Model
{
    public class SandpilesCalculator : AbstractSandpilesCalculator
    {

        public SandpilesCalculator(PropertyChangedEventHandler propertyChangedListener, int width, int height, int depth) : base(propertyChangedListener, width, height, depth)
        {

        }

        public override IterationResult PerformIterate(int[] space, int[] delta) // now using single dimension array and no method calls from inner loop 4x faster
        {
            int hd = height * depth;
            int whd = width * height * depth;
            delta = new int[whd];
            var nextIteration = new int[whd];
            for (int x = width - 1; x >= 0; x--)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int z = depth - 1; z >= 0; z--)
                    {
                        int coord = x * hd + y * depth + z;
                        if (space[coord] >= MAX_AMOUNT)
                        {
                            delta[coord] -= MAX_AMOUNT;
                        }
                        int xN = x - 1;
                        int xP = x + 1;
                        int yN = y - 1;
                        int yP = y + 1;
                        int zN = z - 1;
                        int zP = z + 1;

                        if (xN >= 0)
                        { // possible optimization as the X term is the biggest we do not need to check it 
                            int coordL = xN * hd + y * depth + z;
                            if (coordL >= 0 && space[coordL] >= MAX_AMOUNT)
                            {
                                delta[coord]++;
                            }
                        }
                        if (xP < width)
                        {
                            int coordR = xP * hd + y * depth + z;
                            if (coordR < whd && space[coordR] >= MAX_AMOUNT)
                            {
                                delta[coord]++;
                            }
                        }
                        if (yN >= 0)
                        {
                            int coordD = x * hd + yN * depth + z;
                            if (coordD >= 0 && space[coordD] >= MAX_AMOUNT)
                            {
                                delta[coord]++;
                            }
                        }
                        if (yP < height)
                        {
                            int coordU = x * hd + yP * depth + z;
                            if (coordU < whd && space[coordU] >= MAX_AMOUNT)
                            {
                                delta[coord]++;
                            }
                        }
                        if (zN >= 0)
                        {
                            int coordB = x * hd + y * depth + zN;
                            if (coordB >= 0 && space[coordB] >= MAX_AMOUNT)
                            {
                                delta[coord]++;
                            }
                        }
                        if (zP < depth)
                        {
                            int coordF = x * hd + y * depth + zP;
                            if (coordF < whd && space[coordF] >= MAX_AMOUNT)
                            {
                                delta[coord]++;
                            }
                        }
                        nextIteration[coord] = space[coord] + delta[coord];
                    }
                }
            }
            for (int i = 0; i < whd; i++)
            {
                space[i] = nextIteration[i];
            }
            return new IterationResult(space, delta);
        }

        public override void PreIterate()
        {
            //No setup   
        }

        public override void PostIterate()
        {
            //No teardown   
        }
    }
}

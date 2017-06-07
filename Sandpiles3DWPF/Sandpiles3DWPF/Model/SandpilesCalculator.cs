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
    public class SandpilesCalculator : INotifyPropertyChanged
    {
        internal const int MAX_AMOUNT = 6;
        public event PropertyChangedEventHandler PropertyChanged;

        public int[] space { get; set; } // So ugly i wanna cry
        public int[] delta { get; set; } // refactor everything so i do not have to give these out
        private float[,] multipliers;

        public int width { get; private set; }
        public int height { get; private set; }
        public int depth { get; private set; }
        public int iterationCounter { get; private set; }

        public SandpilesCalculator(PropertyChangedEventHandler propertyChangedListener, int width, int height, int depth)
        {
            iterationCounter = 0;
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.space = new int[width * height * depth];
            this.delta = new int[width * height * depth];

            float depthF = depth - 1;
            float midPoint = depthF / 2;
            multipliers = new float[depth, 3];
            for (int z = 0; z < depth; z++)
            {
                multipliers[z, 0] = z / depthF;
                multipliers[z, 1] = (depthF - z) / depthF;
                multipliers[z, 2] = 1 - Math.Abs((1 - z / midPoint));
            }
            PropertyChanged += propertyChangedListener;
            AllPropertiesChanged();
        }

        public void AllPropertiesChanged()
        {
            OnPropertyChanged();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Fill(int value)
        {
            space.Fill(value);
        }

        public void FillMax()
        {
            space.Fill(MAX_AMOUNT);
        }

        internal void SetPosition(int x, int y, int z, int value)
        {
            space[x * height * depth + y * depth + z] = value;
        }

        public virtual void Iterate() // now using single dimension array and no method calls from inner loop 4x faster
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
            iterationCounter++;
        }

        // Redo this
        private static readonly Dictionary<int, Color> heightMap = new Dictionary<int, Color>{
            { 0, Colors.SlateGray},
            { 1, Colors.Coral},
            { 2, Colors.AliceBlue},
            { 3, Colors.DarkSeaGreen},
            { 4, Colors.Red},
            { 5, Colors.Violet},
            { 6, Colors.Olive},
            { 7, Colors.White}
        };

        internal SandpilesIterationData GetBinary3DRepresentation()
        {
            return new SandpilesIterationData(iterationCounter, delta);
        }

        internal SandpilesIterationData GetCrossSection(int position, bool xDim, bool yDim, bool zDim) //Posted http://stackoverflow.com/questions/43272390/how-to-get-cross-section-of-3-dimensional-array-c-sharp
        {
            if ((xDim ? 1 : 0) + (yDim ? 1 : 0) + (zDim ? 1 : 0) > 1)
            {
                throw new Exception("Only 2 dimensional cross sections allowed");
            }
            else if (xDim)
            {
                throw new NotImplementedException();
            }
            else if (yDim)
            {
                throw new NotImplementedException();
            }
            else if (zDim)
            {
                Color[,] crossSection = new Color[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        var val = space[x * height * depth + y * depth + position];
                        val = val < 0 ? 0 : val; // values are not negative unless model values are changed(decreased) in the middle of an iteration, 
                        crossSection[x, y] = val < heightMap.Count ? heightMap[val] : heightMap[heightMap.Count - 1];
                    }
                }
                return new SandpilesIterationData(iterationCounter, crossSection);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal SandpilesIterationData Get2DProjection() // move method to helper class
        {
            int dims = 3;

            float[,,] flatten = new float[width, height, dims];

            float[] biggestValue = new float[dims];

            for (int x = width - 1; x >= 0; x--)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    int lastValue = space[x * height * depth + y * depth + 0];
                    for (int z = depth - 1; z >= 0; z--) //difference counted in one direction, minor source of assymetry
                    {
                        int difference = Math.Abs(lastValue - space[x * height * depth + y * depth + z]);
                        for (int d = 0; d < dims; d++)
                        {
                            flatten[x, y, d] += difference * multipliers[z, d];
                        }
                        lastValue = space[x * height * depth + y * depth + z];
                    }

                    for (int d = 0; d < dims; d++)
                    {
                        if (flatten[x, y, d] > biggestValue[d])
                        {
                            biggestValue[d] = flatten[x, y, d];
                        }
                    }
                }
            }

            if (biggestValue.Max() == 0)
            {
                return NoDataColorMatrix();
            }
            Color[,] projection = new Color[width, height];
            float normalize = biggestValue.Max();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int d = 0; d < dims; d++)
                    {
                        //flatten[x, y, d] = (flatten[x, y, d] / biggestValue[d]) * 255;
                        flatten[x, y, d] = (flatten[x, y, d] / normalize) * 255;
                    }
                    projection[x, y] = Color.FromRgb((byte)flatten[x, y, 0], (byte)flatten[x, y, 1], (byte)flatten[x, y, 2]);
                }
            }
            return new SandpilesIterationData(iterationCounter, projection);
        }

        internal bool IsStable()
        {
            for (int x = width - 1; x >= 0; x--)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int z = depth - 1; z >= 0; z--)
                    {
                        if (space[x * height * depth + y * depth + z] >= MAX_AMOUNT)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        internal int getMidX()
        {
            return width / 2;
        }

        internal int getMidY()
        {
            return height / 2;
        }

        internal int getMidZ()
        {
            return depth / 2;
        }

        private SandpilesIterationData NoDataColorMatrix()
        {
            Color[,] projection = new Color[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    projection[x, y] = Color.FromRgb((byte)((30 + x) % 250), (byte)(y % 250), (byte)((x + y) % 250));
                }
            }
            return new SandpilesIterationData(iterationCounter, projection);
        }

        internal bool IsValidCoordinate(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0)
            {
                return false;
            }
            else if (x >= width || y >= height || z >= depth)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        internal void FillValues(bool[] dimEnabled, int[] coords, int value)
        {
            int[,] dimensionIterationInterval = new int[,] { { 0, width }, { 0, height }, { 0, depth } };
            for (int i = 0; i < dimEnabled.Length; i++)
            {
                if (dimEnabled[i])
                {
                    dimensionIterationInterval[i, 0] = coords[i];
                    dimensionIterationInterval[i, 1] = coords[i] + 1;
                }
            }

            for (int x = dimensionIterationInterval[0, 0]; x < dimensionIterationInterval[0, 1]; x++)
            {
                for (int y = dimensionIterationInterval[1, 0]; y < dimensionIterationInterval[1, 1]; y++)
                {
                    for (int z = dimensionIterationInterval[2, 0]; z < dimensionIterationInterval[2, 1]; z++)
                    {
                        space[x * height * depth + y * depth + z] = value;
                    }
                }
            }
        }
    }

}

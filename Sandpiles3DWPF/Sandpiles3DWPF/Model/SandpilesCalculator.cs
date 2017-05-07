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

namespace Sandpiles3DWPF.Model
{
    public class SandpilesCalculator : INotifyPropertyChanged
    {
        private const int MAX_AMOUNT = 6;
        public event PropertyChangedEventHandler PropertyChanged;

        private int[,,] space;
        private int[,,] delta;
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
            this.space = new int[width, height, depth];
            this.delta = new int[width, height, depth];

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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") //This system is pretty un-utilized and unnecessary, should do consts instead
        {
            //invoke if not null
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
            space[x, y, z] = value;
        }


        public void Iterate()
        {
            delta = new int[width, height, depth];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        if (space[x, y, z] > MAX_AMOUNT)
                        {
                            Collapse(x, y, z);
                        }
                    }
                }
            }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        space[x, y, z] += delta[x, y, z];
                    }
                }
            }
            iterationCounter++;
        }

        private void Collapse(int x, int y, int z)
        {
            delta[x, y, z] += -MAX_AMOUNT;
            AddDelta(x - 1, y, z);
            AddDelta(x + 1, y, z);
            AddDelta(x, y - 1, z);
            AddDelta(x, y + 1, z);
            AddDelta(x, y, z - 1);
            AddDelta(x, y, z + 1);
        }

        private void AddDelta(int x, int y, int z)
        {
            if (!IsValidCoordinate(x, y, z))
            {
                return;
            }
            delta[x, y, z]++;
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
                        var val = space[x, y, position];
                        val = val < 0 ? 0 : val; // values are not negative unless model values are changed(decreased) in the middle of an iteration, 
                        crossSection[x, y] = val < heightMap.Count ? heightMap[val] : heightMap[heightMap.Count-1];
                    }
                }
                return new SandpilesIterationData(iterationCounter, crossSection);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal SandpilesIterationData Get2DProjection()
        {
            int dims = 3;

            float[,,] flatten = new float[width, height, dims];

            float[] biggestValue = new float[dims];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int lastValue = space[x, y, 0];
                    for (int z = 1; z < depth; z++) //difference counted in one direction, minor source of assymetry
                    {
                        int difference = Math.Abs(lastValue - space[x, y, z]);
                        for (int d = 0; d < dims; d++)
                        {
                            flatten[x, y, d] += difference * multipliers[z, d];
                        }
                        lastValue = space[x, y, z];
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
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        if (space[x, y, z] > MAX_AMOUNT)
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
                        space[x, y, z] = value;
                    }
                }
            }
        }
    }


    public static class ArrayExtensions
    {
        public static void Fill<T>(this T[,,] originalArray, T with) // http://stackoverflow.com/questions/5943850/fastest-way-to-fill-an-array-with-a-single-value
        {
            for (int x = 0; x < originalArray.GetLength(0); x++)
            {
                for (int y = 0; y < originalArray.GetLength(1); y++)
                {
                    for (int z = 0; z < originalArray.GetLength(2); z++)
                    {
                        originalArray[x, y, z] = with;
                    }
                }
            }
        }
    }
}

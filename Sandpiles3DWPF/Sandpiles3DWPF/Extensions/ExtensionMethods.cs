using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Sandpiles3DWPF.Extensions
{
    public static class MyExtensions
    {
        public static void InvokeForAll<T>(this T[,,] array, Action<int, int, int> action)
        {
            for (int x = array.GetLength(0) - 1; x >= 0; x--)
            {
                for (int y = array.GetLength(1) - 1; y >= 0; y--)
                {
                    for (int z = array.GetLength(2) - 1; z >= 0; z--)
                    {
                        action.Invoke(x, y, z);
                    }
                }
            }
        }

        public static void Fill<T>(this T[,,] array, T with) // http://stackoverflow.com/questions/5943850/fastest-way-to-fill-an-array-with-a-single-value
        {
            for (int x = array.GetLength(0) - 1; x >= 0; x--)
            {
                for (int y = array.GetLength(1) - 1; y >= 0; y--)
                {
                    for (int z = array.GetLength(2) - 1; z >= 0; z--)
                    {
                        array[x, y, z] = with;
                    }
                }
            }
        }

        public static void Fill<T>(this T[] array, T with) 
        {
            for (int i = array.Length - 1; i >= 0; i--)
            {
                array[i] = with;
            }
        }

        public static void AddTriangle(this Int32Collection triangles, Point3DCollection availablePoints, Point3D a, Point3D b, Point3D c)
        {
            triangles.Add(availablePoints.IndexOf(a));
            triangles.Add(availablePoints.IndexOf(b));
            triangles.Add(availablePoints.IndexOf(c));
        }

        /* Indices clockwise
         */
        public static void AddCubeFace(this Int32Collection triangles, Point3DCollection availablePoints, Point3D a, Point3D b, Point3D c, Point3D d)
        {
            triangles.Add(availablePoints.IndexOf(a));
            triangles.Add(availablePoints.IndexOf(b));
            triangles.Add(availablePoints.IndexOf(c));
            triangles.Add(availablePoints.IndexOf(c));
            triangles.Add(availablePoints.IndexOf(d));
            triangles.Add(availablePoints.IndexOf(a));
        }
    }
}

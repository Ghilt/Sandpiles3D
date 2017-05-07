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

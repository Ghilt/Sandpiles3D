using Sandpiles3DWPF.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Sandpiles3DWPF.ViewModel
{
    class CubeGrid
    {
        private DiffuseMaterial material;
        private PointCollection textureCoordinates;
        private Vector3DCollection normalVectors;

        public Model3DGroup Grid { private set; get; }
        private GeometryModel3D[,,] cubes;
        private ScaleTransform3D hide;
        private int width;
        private int height;
        private int depth;

        public CubeGrid(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
            Grid = new Model3DGroup();
            cubes = new GeometryModel3D[width, height, depth];
        }

        public void Initiate()
        {
            CreateResources();
            CreateGrid(width, height, depth);
        }

        private void CreateResources()
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint = new Point(1, 0.5);
            brush.GradientStops.Add(new GradientStop(Colors.MediumPurple, 0.0));
            brush.GradientStops.Add(new GradientStop(Colors.Sienna, 0.25));
            brush.GradientStops.Add(new GradientStop(Colors.Blue, 0.75));
            brush.GradientStops.Add(new GradientStop(Colors.LimeGreen, 1.0));
            material = new DiffuseMaterial(brush);

            textureCoordinates = new PointCollection();
            textureCoordinates.Add(new Point(0, 0));
            textureCoordinates.Add(new Point(1, 0));
            textureCoordinates.Add(new Point(1, 1));
            textureCoordinates.Add(new Point(1, 1));
            textureCoordinates.Add(new Point(1, 1));
            textureCoordinates.Add(new Point(0, 1));
            textureCoordinates.Add(new Point(0, 0));

            normalVectors = new Vector3DCollection();
            normalVectors.Add(new Vector3D(0, 0, 1));
            normalVectors.Add(new Vector3D(0, 0, 1));
            normalVectors.Add(new Vector3D(0, 0, 1));
            normalVectors.Add(new Vector3D(0, 0, 1));
            normalVectors.Add(new Vector3D(0, 0, 1));
            normalVectors.Add(new Vector3D(0, 0, 1));

            hide = new ScaleTransform3D(0, 0, 0);
        }

        private void CreateGrid(int width, int height, int depth)
        {
            float dx = 1, dy = 1, dz = 1;
            float spacing = 0.2f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        Grid.Children.Add(CreateCube(x, y, z, dx, dy, dz, spacing));
                    }
                }
            }
        }

        private GeometryModel3D CreateCube(int xPos, int yPos, int zPos, float dx, float dy, float dz, float spacing)
        {
            #region Geometry aid
            /*         G __________ H
             *        /           / |
             *       /           /  | 
             *      C _________ D   | 
             *      |   E       |   |F       
             *      |  /        |  /
             *      | /         | /
             *      A _________ B
             */
            #endregion

            float x = xPos * (dx + spacing);
            float y = yPos * (dy + spacing);
            float z = zPos * (dz + spacing);

            Point3D A = new Point3D(x, y, z);
            Point3D B = new Point3D(x + dx, y, z);
            Point3D C = new Point3D(x, y + dy, z);
            Point3D D = new Point3D(x + dx, y + dy, z);
            Point3D E = new Point3D(x, y, z + dz);
            Point3D F = new Point3D(x + dx, y, z + dz);
            Point3D G = new Point3D(x, y + dy, z + dz);
            Point3D H = new Point3D(x + dx, y + dy, z + dz);

            cubes[xPos, yPos, zPos] = new GeometryModel3D();
            MeshGeometry3D myMeshGeometry3D = new MeshGeometry3D();
            Point3DCollection points = new Point3DCollection();
            myMeshGeometry3D.Positions = points;
            points.Add(A);
            points.Add(B);
            points.Add(C);
            points.Add(D);
            points.Add(E);
            points.Add(F);
            points.Add(G);
            points.Add(H);
            Int32Collection triangleIndices = new Int32Collection();
            triangleIndices.AddCubeFace(points, A, C, D, B);
            triangleIndices.AddCubeFace(points, A, E, G, C);
            triangleIndices.AddCubeFace(points, C, G, H, D);
            triangleIndices.AddCubeFace(points, A, B, F, E);
            triangleIndices.AddCubeFace(points, E, F, H, G);
            triangleIndices.AddCubeFace(points, D, H, F, B);
            myMeshGeometry3D.TriangleIndices = triangleIndices;

            //myMeshGeometry3D.Normals = normalVectors;
            //myMeshGeometry3D.TextureCoordinates = textureCoordinates;
            cubes[xPos, yPos, zPos].Geometry = myMeshGeometry3D;
            cubes[xPos, yPos, zPos].Material = material;
            cubes[xPos, yPos, zPos].Freeze(); // this line incredibly important for speed
            return cubes[xPos, yPos, zPos];
        }

        internal void Freeze()
        {
            Grid.Freeze();
            hide.Freeze();
        }

        internal void Unfreeze()
        {
            if (Grid.IsFrozen)
            {
                Grid = Grid.Clone();
                Model3D[] m = Grid.Children.ToArray();
                int i = 0;
                for (int x = 0; x < width; x++) //re-map
                {
                    for (int y = 0; y < height; y++)
                    {   
                        for (int z = 0; z < depth; z++)
                        {
                            cubes[x, y, z] = m[i] as GeometryModel3D;
                            i++;
                        }
                    }
                }
                hide = hide.Clone();
            }
        }

        internal void Update(int[,,] data3D)
        {
            for (int x = 0; x < data3D.GetLength(0); x++)
            {
                for (int y = 0; y < data3D.GetLength(1); y++)
                {
                    for (int z = 0; z < data3D.GetLength(2); z++)
                    {
                        if (data3D[x,y,z] > 0)
                        {
                            cubes[x, y, z].Transform = null;
                        }
                        else
                        {
                            cubes[x, y, z].Transform = hide;
                        }
                    }
                }
            }
        }
    }
}

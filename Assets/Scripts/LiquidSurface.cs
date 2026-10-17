using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiquidSimulator
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class LiquidSurface : MonoBehaviour
    {
        const int Resolution = 72;
        const float Step = 5f / Resolution, BlobRadius = 0.21f, Iso = 0.52f;
        readonly float[] field = new float[(Resolution + 1) * (Resolution + 1)];
        readonly List<Vector3> vertices = new(30000);
        readonly List<int> indices = new(30000);
        readonly List<Color> colors = new(30000);
        readonly Vector2[] polygon = new Vector2[4];
        Mesh mesh;
        Material material;
        public LiquidSimulation Simulation;
        void Awake()
        {
            mesh = new Mesh { name = "Liquid isosurface", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic(); GetComponent<MeshFilter>().sharedMesh = mesh;
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            GetComponent<MeshRenderer>().sharedMaterial = material;
            GetComponent<MeshRenderer>().sortingOrder = 2;
        }
        void LateUpdate()
        {
            if (Simulation == null || Simulation.Positions == null) return;
            transform.SetPositionAndRotation(Simulation.ContainerPosition, Quaternion.Euler(0, 0, Simulation.ContainerAngle));
            System.Array.Clear(field, 0, field.Length);
            foreach (Vector2 world in Simulation.Positions)
            {
                Vector2 p = Simulation.ToLocal(world);
                int x0 = Mathf.Max(0, Mathf.FloorToInt((p.x - BlobRadius + 2.5f) / Step));
                int x1 = Mathf.Min(Resolution, Mathf.CeilToInt((p.x + BlobRadius + 2.5f) / Step));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((p.y - BlobRadius + 2.5f) / Step));
                int y1 = Mathf.Min(Resolution, Mathf.CeilToInt((p.y + BlobRadius + 2.5f) / Step));
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    float q = 1f - (Point(x, y) - p).sqrMagnitude / (BlobRadius * BlobRadius);
                    if (q > 0) field[y * (Resolution + 1) + x] += q * q;
                }
            }
            vertices.Clear(); indices.Clear(); colors.Clear();
            for (int y = 0; y < Resolution; y++) for (int x = 0; x < Resolution; x++)
            {
                int a = y * (Resolution + 1) + x, b = a + 1, d = a + Resolution + 1, c = d + 1;
                Clip(Point(x,y), Point(x+1,y), Point(x+1,y+1), field[a], field[b], field[c]);
                Clip(Point(x,y), Point(x+1,y+1), Point(x,y+1), field[a], field[c], field[d]);
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
        }
        static Vector2 Point(int x, int y) => new(-2.5f + x * Step, -2.5f + y * Step);
        void Clip(Vector2 a, Vector2 b, Vector2 c, float fa, float fb, float fc)
        {
            int count = 0;
            Edge(c,a,fc,fa,ref count); Edge(a,b,fa,fb,ref count); Edge(b,c,fb,fc,ref count);
            for (int i = 1; i < count - 1; i++)
            {
                Add(polygon[0]); Add(polygon[i+1]); Add(polygon[i]);
            }
        }
        void Edge(Vector2 a, Vector2 b, float fa, float fb, ref int count)
        {
            if ((fa >= Iso) != (fb >= Iso)) polygon[count++] = Vector2.Lerp(a,b,(Iso-fa)/(fb-fa));
            if (fb >= Iso) polygon[count++] = b;
        }
        void Add(Vector2 p)
        {
            indices.Add(vertices.Count); vertices.Add(new Vector3(p.x,p.y,0));
            colors.Add(Color.Lerp(new Color(0.025f,0.30f,0.70f,1), new Color(0.20f,0.88f,1,1), (p.y+2.5f)/5f));
        }
        void OnDestroy() { if (mesh != null) Destroy(mesh); if (material != null) Destroy(material); }
    }
}

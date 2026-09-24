using System;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // The tube an arm draws: a ring of sides around every joint, capped at both ends, rewritten each frame it shows.
    // The mesh belongs to this arm alone.
    public class LianaMesh : IDisposable
    {
        static readonly int sides = 6;
        // A direction shorter than this has none
        static readonly float zeroLengthSquared = 0.000000000001f;
        // The chain thins to this share of its radius at the tip, narrow collars between broader internodes
        static readonly float tipTaper = 0.65f;
        static readonly float collarWidth = 0.76f;
        static readonly float internodeWidth = 1.12f;
        // Every third ring is a collar
        static readonly int collarEvery = 3;

        Mesh _mesh;
        Vector3[] _vertices;
        Vector3[] _normals;

        Transform _container;
        public Transform container { get { return _container; } }

        MeshRenderer _renderer;
        public MeshRenderer renderer { get { return _renderer; } }

        public void Init(Transform parent, Material material, Color colour, int jointCount)
        {
            int segments = jointCount - 1;
            _container = new GameObject("LianaArm").transform;
            _container.SetParent(parent, false);
            _mesh = new Mesh { name = "LianaChain", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _vertices = new Vector3[jointCount * sides + 2];
            _normals = new Vector3[_vertices.Length];
            int[] triangles = new int[segments * sides * 6 + sides * 6];
            int index = 0;
            for (int j = 0; j < segments; j++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int a = j * sides + side;
                    int next = j * sides + (side + 1) % sides;
                    int b = a + sides;
                    int nextB = next + sides;
                    triangles[index++] = a;
                    triangles[index++] = next;
                    triangles[index++] = b;
                    triangles[index++] = next;
                    triangles[index++] = nextB;
                    triangles[index++] = b;
                }
            }

            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                int end = segments * sides;
                triangles[index++] = _vertices.Length - 2;
                triangles[index++] = next;
                triangles[index++] = side;
                triangles[index++] = _vertices.Length - 1;
                triangles[index++] = end + side;
                triangles[index++] = end + next;
            }

            // Every vertex carries a normal from creation: SelectableEntity adds QuickOutline, whose Awake
            // reads one normal per vertex of every child mesh, before the first gesture uploads real normals.
            _mesh.vertices = _vertices;
            _mesh.normals = _normals;
            _mesh.triangles = triangles;
            _container.gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _container.gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(RenderObjects.BaseColorId, colour);
            _renderer.SetPropertyBlock(block);
            _renderer.enabled = false;
        }

        // The rings follow the pose, thinning toward the tip; width scales the whole tube
        public void Write(LianaPose pose, float radius, float width)
        {
            Matrix4x4 worldToLocal = _container.worldToLocalMatrix;
            int last = pose.jointCount - 1;
            for (int j = 0; j <= last; j++)
            {
                Vector3 tangent = pose.Joint(Mathf.Min(j + 1, last)) - pose.Joint(Mathf.Max(j - 1, 0));
                if (tangent.sqrMagnitude < zeroLengthSquared)
                {
                    tangent = Vector3.up;
                }

                tangent.Normalize();
                Vector3 axis = Vector3.right;
                if (Mathf.Abs(tangent.y) < 0.9f)
                {
                    axis = Vector3.up;
                }

                Vector3 u = Vector3.Cross(tangent, axis).normalized;
                Vector3 v = Vector3.Cross(tangent, u);
                float ring = Mathf.Lerp(radius, radius * tipTaper, (float)j / pose.segmentCount) * width;
                // Narrow collars between broader internodes read as a jointed plant arm at gameplay scale
                if (j % collarEvery == 0)
                {
                    ring *= collarWidth;
                }
                else
                {
                    ring *= internodeWidth;
                }

                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    Vector3 normal = u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
                    int index = j * sides + side;
                    _vertices[index] = worldToLocal.MultiplyPoint3x4(pose.Joint(j) + normal * ring);
                    _normals[index] = worldToLocal.MultiplyVector(normal);
                }
            }

            _vertices[_vertices.Length - 2] = worldToLocal.MultiplyPoint3x4(pose.Joint(0));
            _vertices[_vertices.Length - 1] = worldToLocal.MultiplyPoint3x4(pose.tip);
            _normals[_normals.Length - 2] = worldToLocal.MultiplyVector((pose.Joint(0) - pose.Joint(1)).normalized);
            Vector3 tipDirection = (pose.tip - pose.Joint(last - 1)).normalized;
            _normals[_normals.Length - 1] = worldToLocal.MultiplyVector(tipDirection);
            _mesh.vertices = _vertices;
            _mesh.normals = _normals;
            _mesh.RecalculateBounds();
        }

        public void Dispose()
        {
            if (!_container)
            {
                return;
            }

            _container.gameObject.SetActive(false);
            RenderObjects.Release(_container.gameObject);
            RenderObjects.Release(_mesh);
        }
    }
}

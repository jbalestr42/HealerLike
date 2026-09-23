using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Arrays belong to this result; consumers must treat them as immutable.
    public readonly struct StoneMeshData
    {
        public readonly Vector3[] vertices;
        public readonly Vector3[] normals;
        public readonly Vector3[] weldedVertices;
        public readonly int[] indices;
        public readonly int[] weldedIndices;
        public readonly Bounds bounds;

        public StoneMeshData(Vector3[] vertices, Vector3[] normals, int[] indices, Bounds bounds,
            Vector3[] weldedVertices = null, int[] weldedIndices = null)
        {
            this.vertices = vertices;
            this.normals = normals;
            this.indices = indices;
            this.bounds = bounds;
            this.weldedVertices = weldedVertices;
            this.weldedIndices = weldedIndices;
        }
    }
}

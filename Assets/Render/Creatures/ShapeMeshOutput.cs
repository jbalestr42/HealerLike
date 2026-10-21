using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Unity mesh ownership starts here, after topology has been generated without engine objects.
    public static class ShapeMeshOutput
    {
        public static Mesh Create(
            ShapeProfile shape,
            int variant,
            List<Vector3> points,
            List<int> indices,
            bool mineral,
            List<int> facets
        )
        {
            Mesh mesh = new Mesh { name = "Procedural" + shape.kind, hideFlags = HideFlags.DontSave };
            bool flat = mineral || shape.faceted;
            if (!flat)
            {
                mesh.SetVertices(points);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
            }
            else
            {
                Vector3[] smooth = new Vector3[points.Count];
                Vector3[] vertices = new Vector3[indices.Count];
                Vector3[] normals = new Vector3[indices.Count];
                List<Vector3> outline = new List<Vector3>(indices.Count);
                List<int> body = new List<int>();
                List<int> ochre = new List<int>();
                Dictionary<int, Vector3> faceNormals = null;
                if (facets != null)
                {
                    faceNormals = new Dictionary<int, Vector3>();
                    for (int triangle = 0; triangle < facets.Count; triangle++)
                    {
                        Vector3 a = points[indices[triangle * 3]];
                        Vector3 b = points[indices[triangle * 3 + 1]];
                        Vector3 c = points[indices[triangle * 3 + 2]];
                        faceNormals.TryGetValue(facets[triangle], out Vector3 sum);
                        faceNormals[facets[triangle]] = sum + Vector3.Cross(b - a, c - a);
                    }

                    foreach (int facet in new List<int>(faceNormals.Keys))
                    {
                        Vector3 cross = faceNormals[facet];
                        faceNormals[facet] = cross / Mathf.Sqrt(cross.sqrMagnitude);
                    }
                }

                for (int face = 0; face < indices.Count; face += 3)
                {
                    Vector3 a = points[indices[face]];
                    Vector3 b = points[indices[face + 1]];
                    Vector3 c = points[indices[face + 2]];
                    Vector3 cross = Vector3.Cross(b - a, c - a);
                    // A skinny fan triangle amplifies float rounding. The full polygon supplies one stable normal
                    // for the broad physical cut plane, shared by all triangles on that face.
                    Vector3 normal = facets != null ? faceNormals[facets[face / 3]] : cross.normalized;
                    // A cut polygon keeps one material across its entire plane, regardless of triangulation.
                    int facet = facets == null ? face / 6 : facets[face / 3];
                    bool warm = mineral && ((facet + (variant & 7)) % 7 == 1);
                    for (int j = 0; j < 3; j++)
                    {
                        int index = face + j;
                        vertices[index] = points[indices[index]];
                        normals[index] = normal;
                        smooth[indices[index]] += normal;
                        (warm ? ochre : body).Add(index);
                    }
                }

                foreach (int index in indices)
                {
                    outline.Add(smooth[index].normalized);
                }

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.SetUVs(3, outline);
                mesh.subMeshCount = mineral ? 2 : 1;
                mesh.SetTriangles(body, 0);
                if (mineral)
                {
                    mesh.SetTriangles(ochre, 1);
                }
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class StoneImpactLocator
    {
        public static bool TryClosestPoint(in StoneMeshData mesh, in Matrix4x4 localToWorld, Vector3 queryWS,
            out Vector3 pointWS, out Vector3 normalWS)
        {
            pointWS = default;
            normalWS = Vector3.up;
            float best = float.PositiveInfinity;
            if (mesh.indices == null)
            {
                return false;
            }

            for (int i = 0; i < mesh.indices.Length; i += 3)
            {
                Vector3 a = localToWorld.MultiplyPoint3x4(mesh.vertices[mesh.indices[i]]);
                Vector3 b = localToWorld.MultiplyPoint3x4(mesh.vertices[mesh.indices[i + 1]]);
                Vector3 c = localToWorld.MultiplyPoint3x4(mesh.vertices[mesh.indices[i + 2]]);
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (cross.sqrMagnitude <= 0f)
                {
                    continue;
                }

                Vector3 point = Closest(queryWS, a, b, c);
                float distance = (point - queryWS).sqrMagnitude;
                if (distance >= best)
                {
                    continue;
                }

                best = distance;
                pointWS = point;
                normalWS = cross / Mathf.Sqrt(cross.sqrMagnitude);
            }
            return float.IsFinite(best);
        }

        // Voronoi regions of a triangle, evaluated in world space for nonuniform scale.
        static Vector3 Closest(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
            {
                return a;
            }

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
            {
                return b;
            }

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                return a + ab * (d1 / (d1 - d3));
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
            {
                return c;
            }

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                return a + ac * (d2 / (d2 - d6));
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            }

            float inv = 1f / (va + vb + vc);
            return a + ab * (vb * inv) + ac * (vc * inv);
        }
    }
}

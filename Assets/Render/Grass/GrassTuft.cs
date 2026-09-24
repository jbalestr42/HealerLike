using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Where a grass tuft vertex lands: the tuft mesh is FacetedMeshes.CreatePyramid open at its base, x and z in
    // tuft widths and y in tuft heights. Place mirrors HLPlaceGrassBlade in GrassInstancing.hlsl, a rigid
    // transform with no bending.
    public static class GrassTuft
    {
        // Rotates v about the horizontal axis that tips +Y toward lean, by the length of lean in radians
        public static Vector3 Tilt(Vector3 v, Vector2 lean)
        {
            float angle = Mathf.Max(lean.magnitude, 0.00001f);
            Vector3 axis = new Vector3(lean.y, 0f, -lean.x) / angle;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return v * cos + Vector3.Cross(axis, v) * sin + axis * (Vector3.Dot(axis, v) * (1f - cos));
        }

        // Scale by width and height, yaw about the root, tilt about the root by the lean, then move to the root
        public static Vector3 Place(Vector3 positionOS, Vector3 root, float yaw, float width, float height,
                                    Vector2 lean)
        {
            Vector3 scaled = new Vector3(positionOS.x * width, positionOS.y * height, positionOS.z * width);
            return root + Tilt(Yaw(scaled, yaw), lean);
        }

        // The normal takes the inverse of the scale, then the same yaw and tilt as the position
        public static Vector3 PlaceNormal(Vector3 normalOS, float yaw, float width, float height, Vector2 lean)
        {
            Vector3 scaled = new Vector3(normalOS.x / width, normalOS.y / height, normalOS.z / width);
            return Tilt(Yaw(scaled, yaw), lean).normalized;
        }

        // Yaw in radians about +Y, the turn Quaternion.Euler(0, yaw, 0) makes
        static Vector3 Yaw(Vector3 v, float yaw)
        {
            float cos = Mathf.Cos(yaw);
            float sin = Mathf.Sin(yaw);
            return new Vector3(v.x * cos + v.z * sin, v.y, v.z * cos - v.x * sin);
        }
    }
}

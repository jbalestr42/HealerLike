using UnityEngine;

namespace HealerLike.Render.Grass
{
    // The world XZ rectangle the ground textures cover and their texel grid. Texel centres sit at half steps, so
    // uv (0, 0) is the minimum corner of the rectangle and uv (1, 1) the maximum one.
    public struct GroundVolume
    {
        // A grid this wide on either axis at most, whatever the area
        public static readonly int MaxResolution = 512;

        public Rect area;
        public int width;
        public int height;

        public bool isValid
        {
            get { return width > 0 && height > 0 && area.width > 0f && area.height > 0f; }
        }

        // Texels of at most texelSize world units that tile the area exactly, capped at MaxResolution on each axis.
        // Returns an invalid volume and logs when the area or the texel size is not finite and positive.
        public static GroundVolume Create(Rect area, float texelSize)
        {
            bool isAreaValid = float.IsFinite(area.xMin) && float.IsFinite(area.yMin)
                               && RenderMath.IsPositive(area.width) && RenderMath.IsPositive(area.height);
            if (!isAreaValid || !RenderMath.IsPositive(texelSize))
            {
                Debug.LogError($"[GroundVolume] Rejected area {area} with texel size {texelSize}.");
                return new GroundVolume();
            }

            return new GroundVolume
            {
                area = area,
                width = Mathf.Clamp(Mathf.CeilToInt(area.width / texelSize), 1, MaxResolution),
                height = Mathf.Clamp(Mathf.CeilToInt(area.height / texelSize), 1, MaxResolution)
            };
        }

        public Vector2 texelSize
        {
            get { return new Vector2(area.width / width, area.height / height); }
        }

        public Vector2 ToUV(Vector2 world)
        {
            return new Vector2((world.x - area.xMin) / area.width, (world.y - area.yMin) / area.height);
        }

        public Vector2 ToWorld(Vector2 uv)
        {
            return new Vector2(area.xMin + uv.x * area.width, area.yMin + uv.y * area.height);
        }

        // xy the minimum corner, zw the reciprocal size: uv = (world - xy) * zw, the form the shaders read
        public Vector4 ShaderRect()
        {
            return new Vector4(area.xMin, area.yMin, 1f / area.width, 1f / area.height);
        }
    }
}

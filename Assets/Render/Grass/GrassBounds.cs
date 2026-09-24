using UnityEngine;

namespace HealerLike.Render.Grass
{
    public static class GrassBounds
    {
        // How far any tuft or spike vertex can sit from its root: the taller of the tallest tuft, at the top of the
        // scale range under a full heal, and the tallest spike, plus the wider of their half widths. The draw
        // bounds grow by it on every side and the compute keeps a root this far outside the frustum.
        public static float Envelope(float cellSize)
        {
            float tuftHeight = GrassLayout.TuftHeight * GrassLayout.MaxScale * GrassLayout.HealLift * cellSize;
            float tuftHalfWidth = GrassLayout.TuftWidth * GrassLayout.MaxScale * cellSize * 0.5f;
            float height = Mathf.Max(tuftHeight, GrassLayout.SpikeHeight);
            return height + Mathf.Max(tuftHalfWidth, GrassLayout.SpikeHalfWidth);
        }

        // Returns empty bounds and logs when the footprint is not valid
        public static Bounds Calculate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY)
        {
            if (!GrassLayout.IsValid(width, height, cellSize, gridOrigin, surfaceY))
            {
                Debug.LogError($"[GrassBounds] Rejected a {width} x {height} footprint.");
                return new Bounds();
            }

            float envelope = Envelope(cellSize);
            Vector3 center = new Vector3(gridOrigin.x, surfaceY + GrassLayout.RootLift, gridOrigin.z);
            float sizeX = width * cellSize + 2f * envelope;
            float sizeZ = height * cellSize + 2f * envelope;
            return new Bounds(center, new Vector3(sizeX, 2f * envelope, sizeZ));
        }
    }
}

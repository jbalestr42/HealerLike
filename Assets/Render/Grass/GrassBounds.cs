using UnityEngine;

namespace HealerLike.Render.Grass
{
    public static class GrassBounds
    {
        // Returns empty bounds and logs when the footprint or the envelope is not valid
        public static Bounds Calculate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY, float bladeEnvelope = 0.85f)
        {
            bool isFootprintValid = GrassLayout.IsValid(width, height, cellSize, gridOrigin, surfaceY);
            if (!isFootprintValid || !float.IsFinite(bladeEnvelope) || bladeEnvelope < 0.85f)
            {
                Debug.LogError($"[HLGrassBounds] Rejected a {width} x {height} footprint with envelope {bladeEnvelope}.");
                return new Bounds();
            }

            Vector3 center = new Vector3(gridOrigin.x, surfaceY + GrassLayout.RootLift, gridOrigin.z);
            float sizeX = width * cellSize + 2f * bladeEnvelope;
            float sizeZ = height * cellSize + 2f * bladeEnvelope;
            return new Bounds(center, new Vector3(sizeX, 2f * bladeEnvelope, sizeZ));
        }
    }
}

using System;
using UnityEngine;
namespace HealerLike.Render.Grass
{
    public static class HLGrassBounds
    {
        public static Bounds Calculate(int width, int height, float cellSize, Vector3 gridOrigin, float surfaceY, float bladeEnvelope = 0.85f)
        {
            HLGrassLayout.Validate(width, height, cellSize, gridOrigin, surfaceY);
            if (!HLGrassLayout.Finite(bladeEnvelope) || bladeEnvelope < 0.85f) throw new ArgumentOutOfRangeException(nameof(bladeEnvelope));
            return new Bounds(new Vector3(gridOrigin.x, surfaceY + HLGrassLayout.RootLift, gridOrigin.z),
                new Vector3(width * cellSize + 2 * bladeEnvelope, 2 * bladeEnvelope, height * cellSize + 2 * bladeEnvelope));
        }
    }
}

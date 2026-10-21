using UnityEngine;

namespace HealerLike.Render.Environment
{
    // One grass strip of the graded ring. Band 0 is next to the grid, density is in blades per square unit.
    public struct RingStrip
    {
        public Rect rect;
        public int band;
        public float density;
        public int budget;
    }
}

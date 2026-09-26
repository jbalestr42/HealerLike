using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // A bounded presentation timeline. Callers advance it with unscaled time; simulation and rig pivots stay intact.
    public static class CreatureAppearance
    {
        public static readonly float PieceDuration = 0.32f;
        public static readonly float Duration = 0.95f;

        public static float Scale(float elapsed, float delay)
        {
            float progress = Mathf.Clamp01((elapsed - delay) / PieceDuration);
            if (progress >= 1f)
            {
                return 1f;
            }

            if (progress <= 0f)
            {
                return 0.001f;
            }

            // A small settling overshoot, with exact authored scale at the end of every piece.
            float remaining = progress - 1f;
            return Mathf.Max(0.001f, 1f + 1.65f * remaining * remaining * remaining + 0.65f * remaining * remaining);
        }

        public static float PartDelay(PartRole role, float height)
        {
            height = Mathf.Clamp01(height);
            switch (role)
            {
                case PartRole.Limb:
                    return height * 0.08f;
                case PartRole.Body:
                    return 0.12f + height * 0.08f;
                case PartRole.Stem:
                    return 0.17f + height * 0.14f;
                case PartRole.Head:
                    return 0.28f + height * 0.17f;
                case PartRole.Tip:
                    return 0.48f + height * 0.15f;
                default:
                    return 0.36f + height * 0.17f;
            }
        }

        // Root segments appear from their foot inward, with a restrained offset around the root ring.
        public static float RootDelay(int root, int count, int segment, int segments)
        {
            float around = count > 1 ? (float)root / (count - 1) : 0f;
            float inward = segments > 1 ? 1f - (float)segment / (segments - 1) : 0f;
            return around * 0.05f + inward * 0.08f;
        }
    }
}

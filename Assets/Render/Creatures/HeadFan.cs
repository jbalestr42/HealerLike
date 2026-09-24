using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Three or five smaller heads on a branching neck, spread so two neighbours never touch on screen; a stone
    // carries them side by side
    public class HeadFan
    {
        // The id of a fanned neck's branch, which the head measure skips
        public static readonly string BranchId = "Branch";
        // The longest branch of a fanned neck, never shorter than half a unit, in body units
        static readonly float maxBranch = 1.2f;
        static readonly float minBranch = 0.5f;
        // Three heads on a fan are this much smaller than one and this many degrees apart, five heads smaller and
        // closer; a stone's side by side heads sit this far out, in body units
        static readonly float threeHeadScale = 0.72f;
        static readonly float fiveHeadScale = 0.55f;
        static readonly float threeHeadSpread = 40f;
        static readonly float fiveHeadSpread = 28f;
        static readonly float stoneBranch = 0.35f;
        static readonly float branchThickness = 0.12f;
        // A fanned head keeps a fifth of a head clear of its neighbour; the board camera shortens the outer pairs'
        // lean to about this share
        static readonly float headClearance = 1.2f;
        static readonly float foreshortening = 0.85f;

        int _copies;
        float _spread;
        float _length;
        bool _isPlant;

        // Each copy's scale against a single head
        float _copyScale;
        public float copyScale { get { return _copyScale; } }

        public static HeadFan Shape(LookPart[] head, int copies, bool isPlant)
        {
            HeadFan fan = new HeadFan();
            fan._copies = copies;
            fan._isPlant = isPlant;
            fan._copyScale = fiveHeadScale;
            fan._spread = fiveHeadSpread;
            if (copies == 3)
            {
                fan._copyScale = threeHeadScale;
                fan._spread = threeHeadSpread;
            }

            fan._length = stoneBranch;
            if (isPlant)
            {
                fan._copyScale = CopyScale(head, fan._copyScale, fan._spread);
                fan._length = BranchLength(head, fan._copyScale, fan._spread);
            }
            return fan;
        }

        // One branch of the fan, returns where its head sits
        public Vector3 Branch(PartList parts, Vector3 top, int index, float scale, Color colour)
        {
            float angle = (index - (_copies - 1) * 0.5f) * _spread;
            Vector3 direction = Quaternion.Euler(0f, 0f, -angle) * Vector3.up;
            Vector3 end = top + direction * (_length * scale);
            if (_isPlant)
            {
                parts.Link(BranchId, top, end, branchThickness * scale, colour, PartRole.Stem);
            }
            else
            {
                end.y = top.y;
            }
            return end;
        }

        // A fanned head is scaled down when its branches, at their longest, could not keep it clear of its neighbour
        static float CopyScale(LookPart[] head, float copyScale, float spread)
        {
            float fit = maxBranch * Chord(spread) / (HeadWidth(head) * headClearance);
            return Mathf.Min(copyScale, fit);
        }

        // How long a fanned branch must be for two neighbouring heads to clear each other on screen with a fifth of
        // a head to spare
        static float BranchLength(LookPart[] head, float copyScale, float spread)
        {
            float length = HeadWidth(head) * copyScale * headClearance / Chord(spread);
            return Mathf.Clamp(length, minBranch, maxBranch);
        }

        // The screen distance between two neighbouring branch ends per unit of branch length
        static float Chord(float spread)
        {
            return 2f * Mathf.Sin(spread * 0.5f * Mathf.Deg2Rad) * foreshortening;
        }

        // A head's width across the screen at unit scale
        static float HeadWidth(LookPart[] head)
        {
            float width = 0f;
            foreach (LookPart part in head)
            {
                Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(part.euler));
                float half = LookMeasure.Extent(part.size * 0.5f, inverse * Vector3.right);
                width = Mathf.Max(width, 2f * (Mathf.Abs(part.position.x) + half));
            }
            return width;
        }
    }
}

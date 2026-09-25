using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Three or five smaller heads on a branching neck, spread so two neighbours never touch on screen; a stone
    // carries them side by side
    public class HeadFan
    {
        // The id of a fanned neck's branch, which the head measure skips
        public static readonly string BranchId = "Branch";
        LookVocabulary.LayoutEntry _layout;
        ShapeProfile _branchShape;

        int _copies;
        float _spread;
        float _length;
        bool _isPlant;

        // Each copy's scale against a single head
        float _copyScale;
        public float copyScale { get { return _copyScale; } }

        public static HeadFan Shape(LookPart[] head, int copies, bool isPlant,
            LookVocabulary.LayoutEntry layout = null, ShapeProfile branchShape = default)
        {
            HeadFan fan = new HeadFan
            {
                _layout = layout ?? new LookVocabulary.LayoutEntry(),
                _branchShape = branchShape
            };
            fan._copies = copies;
            fan._isPlant = isPlant;
            fan._copyScale = fan._layout.fiveHeadScale;
            fan._spread = fan._layout.fiveHeadSpread;
            if (copies == 3)
            {
                fan._copyScale = fan._layout.threeHeadScale;
                fan._spread = fan._layout.threeHeadSpread;
            }

            // Mineral copies sit in a row. Width controls their spacing so broad slabs keep a visible gap.
            fan._length = Mathf.Max(fan._layout.stoneBranch,
                HeadWidth(head) * fan._copyScale * fan._layout.headClearance);
            if (isPlant)
            {
                fan._copyScale = fan.CopyScale(head, fan._copyScale, fan._spread);
                fan._length = fan.BranchLength(head, fan._copyScale, fan._spread);
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
                parts.Link(BranchId, top, end, _layout.branchThickness * scale, colour, PartRole.Stem, _branchShape);
            }
            else
            {
                end = top + Vector3.right * ((index - (_copies - 1) * 0.5f) * _length * scale);
                Vector3 delta = end - top;
                if (delta.sqrMagnitude > 0.000001f)
                {
                    float thickness = _layout.stoneBranchThickness * scale;
                    parts.Add(BranchId, Primitive.Stone, (top + end) * 0.5f,
                        new Vector3(thickness, delta.magnitude + thickness, thickness), colour,
                        Quaternion.FromToRotation(Vector3.up, delta).eulerAngles, 0f, PartRole.Stem,
                        shape: _branchShape);
                }
            }
            return end;
        }

        // A fanned head is scaled down when its branches, at their longest, could not keep it clear of its neighbour
        float CopyScale(LookPart[] head, float copyScale, float spread)
        {
            float fit = _layout.maxBranch * Chord(spread) / (HeadWidth(head) * _layout.headClearance);
            return Mathf.Min(copyScale, fit);
        }

        // How long a fanned branch must be for two neighbouring heads to clear each other on screen with a fifth of
        // a head to spare
        float BranchLength(LookPart[] head, float copyScale, float spread)
        {
            float length = HeadWidth(head) * copyScale * _layout.headClearance / Chord(spread);
            return Mathf.Clamp(length, _layout.minBranch, _layout.maxBranch);
        }

        // The screen distance between two neighbouring branch ends per unit of branch length
        float Chord(float spread)
        {
            return 2f * Mathf.Sin(spread * 0.5f * Mathf.Deg2Rad) * _layout.foreshortening;
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

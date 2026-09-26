using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Read-only selection presentation; gameplay remains the owner of hover and selected state.
    public readonly struct CreatureSelection
    {
        public readonly bool isHighlighted;
        readonly Color _colour;
        readonly float _width;

        public CreatureSelection(bool highlighted, Color colour, float width)
        {
            isHighlighted = highlighted && RenderMath.IsFinite(colour) && float.IsFinite(width);
            _colour = colour;
            _width = Mathf.Max(0f, width);
        }

        public static CreatureSelection Read(SelectableEntity source)
        {
            return source
                ? new CreatureSelection(source.isHighlighted, source.highlightColor, source.highlightWidth)
                : default;
        }

        public Color Tint(Color authored)
        {
            if (!isHighlighted)
            {
                return authored;
            }

            Color result = Color.Lerp(authored, _colour, 0.3f);
            result.a = authored.a;
            return result;
        }

        public float Width(float authored)
        {
            return isHighlighted ? Mathf.Max(authored, _width) : authored;
        }
    }
}

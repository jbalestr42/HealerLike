using UnityEngine;
namespace HealerLike.Render.Spells
{
    /// <summary>Aggregated cosmetic body tint, read alongside health colour rather than overwriting it.</summary>
    [DisallowMultipleComponent]
    public sealed class HLBodyTintState : MonoBehaviour
    {
        public Color Tint { get; private set; } = Color.white;
        public void Set(Color tint) => Tint = tint;
        public static Color Read(GameObject target)
        {
            var state = target ? target.GetComponent<HLBodyTintState>() : null;
            return state && state.isActiveAndEnabled ? state.Tint : Color.white;
        }
    }
}

using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLBodyTintState : MonoBehaviour
    {
        public Color Tint { get; set; } = Color.white;

        public void Set(Color tint)
        {
            Tint = tint;
        }

        public static Color Read(GameObject target)
        {
            HLBodyTintState state = target ? target.GetComponent<HLBodyTintState>() : null;
            return state && state.isActiveAndEnabled ? state.Tint : Color.white;
        }
    }
}

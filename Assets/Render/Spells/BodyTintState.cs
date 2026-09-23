using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class BodyTintState : MonoBehaviour
    {
        Color _tint = Color.white;
        public Color tint { get { return _tint; } }

        public void Set(Color tint)
        {
            _tint = tint;
        }

        public static Color Read(GameObject target)
        {
            BodyTintState state = target ? target.GetComponent<BodyTintState>() : null;
            return state && state.isActiveAndEnabled ? state.tint : Color.white;
        }
    }
}

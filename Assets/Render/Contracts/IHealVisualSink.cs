using UnityEngine;

namespace HealerLike.Render
{
    public interface IHealVisualSink
    {
        void OnHealResolved(GameObject target, float value, bool critical);
    }
}

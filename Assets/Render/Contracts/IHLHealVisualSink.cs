using UnityEngine;

namespace HealerLike.Render
{
    public interface IHLHealVisualSink
    {
        void OnHealResolved(GameObject target, float value, bool critical);
    }
}

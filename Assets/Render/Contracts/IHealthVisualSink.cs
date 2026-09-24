using UnityEngine;

namespace HealerLike.Render
{
    // Every health change a source caused, negative for damage; each sink picks the sign it draws
    public interface IHealthVisualSink
    {
        void OnHealthResolved(GameObject target, float value, bool critical);
    }
}

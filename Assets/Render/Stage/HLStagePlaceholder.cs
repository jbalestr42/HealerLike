using UnityEngine;
namespace HealerLike.Render.Stage
{
    // Authoring marker only: deliberately does not impersonate another track's component.
    public sealed class HLStagePlaceholder : MonoBehaviour
    {
        public string requiredType;
        [TextArea] public string replacement;
    }
}

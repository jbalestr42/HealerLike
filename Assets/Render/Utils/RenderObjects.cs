using UnityEngine;

namespace HealerLike.Render
{
    // Object helpers the whole render layer shares
    public static class RenderObjects
    {
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Destroy waits for the end of the frame and only works in play mode, edit mode needs DestroyImmediate
        public static void Release(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}

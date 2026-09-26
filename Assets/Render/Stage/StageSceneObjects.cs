using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    public static class StageSceneObjects
    {
        public static ComponentType Find<ComponentType>(Scene scene) where ComponentType : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ComponentType found = root.GetComponentInChildren<ComponentType>(true);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
    }
}

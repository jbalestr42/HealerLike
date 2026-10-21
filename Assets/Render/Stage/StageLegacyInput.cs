#if HEALERLIKE_RENDER_ANDROID_PREVIEW && (!UNITY_ANDROID || !ENABLE_LEGACY_INPUT_MANAGER || ENABLE_INPUT_SYSTEM)
#error The Android render preview requires only ENABLE_LEGACY_INPUT_MANAGER. Restart the build Editor with activeInputHandler set to 0 if its player defines are stale.
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    // The Android preview uses the same supported input backend for Toolkit and existing gameplay.
    public static class StageLegacyInput
    {
        public static StandaloneInputModule Configure(Scene scene)
        {
            StandaloneInputModule first = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (EventSystem system in root.GetComponentsInChildren<EventSystem>(true))
                {
                    StandaloneInputModule module = Configure(system);
                    if (first == null)
                    {
                        first = module;
                    }
                }
            }
            return first;
        }

        public static StandaloneInputModule Configure(EventSystem system)
        {
            StandaloneInputModule legacy = system.GetComponent<StandaloneInputModule>();
            foreach (BaseInputModule module in system.GetComponents<BaseInputModule>())
            {
                module.enabled = module == legacy;
            }
            if (legacy == null)
            {
                legacy = system.gameObject.AddComponent<StandaloneInputModule>();
            }
            legacy.enabled = true;
            system.UpdateModules();
            return legacy;
        }
    }
}

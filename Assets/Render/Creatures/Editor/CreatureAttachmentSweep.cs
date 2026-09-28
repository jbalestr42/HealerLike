using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Removes creature presentations that outlived their scene. A presentation root is DontSave, so closing its
    // scene does not destroy it: one left by an older build or an interrupted test keeps drawing at its last place
    // in the Scene view and in every play session. A live root always sits in a loaded scene next to its source;
    // one in no loaded scene is a leak. Runs after each script reload and before entering play mode.
    [InitializeOnLoad]
    public static class CreatureAttachmentSweep
    {
        static CreatureAttachmentSweep()
        {
            EditorApplication.delayCall += () => Sweep();
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingEditMode)
                {
                    Sweep();
                }
            };
        }

        public static bool IsLeaked(GameObject root)
        {
            return root != null && root.transform.parent == null && root.name == CreatureAttachment.RootName
                && (root.hideFlags & HideFlags.DontSave) == HideFlags.DontSave && !EditorUtility.IsPersistent(root)
                && !(root.scene.IsValid() && root.scene.isLoaded);
        }

        public static int Sweep()
        {
            List<GameObject> leaked = new List<GameObject>();
            foreach (GameObject root in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (IsLeaked(root))
                {
                    leaked.Add(root);
                }
            }

            foreach (GameObject root in leaked)
            {
                Object.DestroyImmediate(root);
            }

            if (leaked.Count > 0)
            {
                Debug.Log($"[CreatureAttachmentSweep] Removed {leaked.Count} creature presentations left without a scene.");
            }

            return leaked.Count;
        }
    }
}

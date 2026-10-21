using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Studio.Editor
{
    public static class CreatureLiveEditMeasure
    {
        public static Vector3[] BodyVertices(CreatureRig rig)
        {
            MeshFilter mesh = rig.partTransforms[0].GetComponent<MeshFilter>();
            return mesh && mesh.sharedMesh ? mesh.sharedMesh.vertices : new Vector3[0];
        }

        public static int ChangedVertices(Vector3[] before, Vector3[] after)
        {
            if (before.Length != after.Length)
            {
                return Mathf.Max(before.Length, after.Length);
            }
            int changed = 0;
            for (int i = 0; i < before.Length; i++)
            {
                if ((before[i] - after[i]).sqrMagnitude > 0.00000001f)
                {
                    changed++;
                }
            }
            return changed;
        }

        public static int ChangedPixels(Texture2D before, Texture2D after)
        {
            Color32[] a = before.GetPixels32();
            Color32[] b = after.GetPixels32();
            int count = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b) > 30)
                {
                    count++;
                }
            }
            return count;
        }
    }
}

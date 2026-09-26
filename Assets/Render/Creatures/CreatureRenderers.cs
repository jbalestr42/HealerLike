using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // Bounds and capture tools include geometry owned by a host outside its gameplay hierarchy.
    public static class CreatureRenderers
    {
        public static Renderer[] Find(Transform source, bool includeInactive = false)
        {
            List<Renderer> result = new List<Renderer>(source.GetComponentsInChildren<Renderer>(includeInactive));
            foreach (ARigHost host in source.GetComponentsInChildren<ARigHost>(includeInactive))
            {
                host.SyncGeometry();
                Transform presentation = host.presentation;
                if (presentation && (includeInactive || presentation.gameObject.activeInHierarchy))
                {
                    result.AddRange(presentation.GetComponentsInChildren<Renderer>(includeInactive));
                }
            }

            return result.ToArray();
        }
    }
}

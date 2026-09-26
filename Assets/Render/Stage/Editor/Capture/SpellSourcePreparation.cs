using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public static class SpellSourcePreparation
    {
        // Prime the Editor search index outside Play. Its startup callback otherwise races scene/domain reload.
        // Exceptions remain visible and fail the process, never suppressed or filtered.
        public static void Enter(string mode, float seconds)
        {
            Type database = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.Search.SearchDatabase")).First(t => t != null);
            MethodInfo get = database.GetMethod("GetDefaultSearchDatabase",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object result = get.Invoke(null, null);
            if (result == null) throw new InvalidOperationException("Search database initialization failed");
            Debug.Log("[SpellSourcePreparation] Search database initialized outside Play.");
            EditorApplication.delayCall += () => StagePlay.Enter(mode, seconds);
        }

        public static void Offscreen() => Enter("offscreen-player", 180f);
    }
}

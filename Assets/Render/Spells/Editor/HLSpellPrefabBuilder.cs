using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public static class HLSpellPrefabBuilder
    {
        // Batchmode check of the player defines, without building or changing a scene. The throw is the non-zero
        // exit code the batchmode run reads.
        public static void VerifyPlayerCompilation()
        {
            ScriptCompilationSettings settings = new ScriptCompilationSettings();
            settings.target = BuildTarget.StandaloneOSX;
            settings.group = BuildTargetGroup.Standalone;
            string output = Path.Combine(Path.GetTempPath(), "hl-player-scripts");
            Directory.CreateDirectory(output);
            ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, output);

            bool hasRender = false;
            bool hasSpellEditor = false;
            if (result.assemblies != null)
            {
                foreach (string assembly in result.assemblies)
                {
                    hasRender |= Path.GetFileName(assembly) == "HealerLike.Render.dll";
                    hasSpellEditor |= Path.GetFileName(assembly) == "HealerLike.Render.Spells.Editor.dll";
                }
            }

            if (result.typeDB != null)
            {
                result.typeDB.Dispose();
            }

            if (!hasRender)
            {
                throw new InvalidOperationException("[HLSpellPrefabBuilder] The player has no render assembly.");
            }

            if (hasSpellEditor)
            {
                throw new InvalidOperationException("[HLSpellPrefabBuilder] Spell authoring was included in the player.");
            }
            Debug.Log("[HLSpellPrefabBuilder] StandaloneOSX player scripts compiled, spell authoring excluded.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using Document = HealerLike.Render.Stage.AtlasDerivationDump.Document;
using Vocabulary = HealerLike.Render.Stage.AtlasDerivationDump.Vocabulary;
using Inputs = HealerLike.Render.Stage.AtlasDerivationDump.Inputs;
using Channels = HealerLike.Render.Stage.AtlasDerivationDump.Channels;
using EntityRow = HealerLike.Render.Stage.AtlasDerivationDump.EntityRow;
using HandlerRow = HealerLike.Render.Stage.AtlasDerivationDump.HandlerRow;
using BuffInput = HealerLike.Render.Stage.AtlasDerivationDump.BuffInput;
using CharacterRow = HealerLike.Render.Stage.AtlasDerivationDump.CharacterRow;
using HeadInput = HealerLike.Render.Stage.AtlasDerivationDump.HeadInput;
using ProjectileRow = HealerLike.Render.Stage.AtlasDerivationDump.ProjectileRow;
using Distinct = HealerLike.Render.Stage.AtlasDerivationDump.Distinct;

namespace HealerLike.Render.Stage
{
    public static class AtlasAssetCatalog
    {
        public static string[] Paths<T>(string folder) where T : UnityEngine.Object
        {
            string[] paths = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (paths.Length == 0)
            {
                throw new InvalidOperationException("No " + typeof(T).Name + " assets under " + folder);
            }
            return paths;
        }

        public static T Required<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null)
            {
                throw new InvalidOperationException("Missing " + typeof(T).Name + ": " + path);
            }
            return value;
        }

        public static string Commit()
        {
            System.Diagnostics.ProcessStartInfo command = new System.Diagnostics.ProcessStartInfo("git",
                "rev-parse HEAD")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(command))
            {
                string commit = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if (process.ExitCode != 0 || commit.Length != 40)
                {
                    throw new InvalidOperationException("Cannot identify source commit");
                }
                return commit;
            }
        }
    }
}

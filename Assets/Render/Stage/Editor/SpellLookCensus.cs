using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // One row per live handler: its derived channels and the element and material each caster material draws.
    // A look is the pair of element and drawn material; the census counts the distinct ones.
    public static class SpellLookCensus
    {
        const string VocabularyPath = "Assets/Render/Spells/Data/EffectVocabulary.asset";

        [MenuItem("Tools/Render/Write Spell Look Census")]
        public static void Write()
        {
            string output = System.Environment.GetEnvironmentVariable("SPELL_CENSUS_OUT");
            if (string.IsNullOrEmpty(output)) output = "Temp/spell-look-census.tsv";
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(VocabularyPath);
            StringBuilder text = new StringBuilder("handler\tchannels\tplant\tstone\n");
            HashSet<string> plantLooks = new HashSet<string>();
            HashSet<string> allLooks = new HashSet<string>();
            List<CharacterData> characters = AtlasAssetCatalog.Characters();
            foreach (string path in AssetDatabase.FindAssets("t:ABuffHandlerFactory", new[] { "Assets/Data" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, System.StringComparer.Ordinal))
            {
                ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(path);
                EffectChannels channels = Channels(handler, characters);
                string plant = Look(vocabulary, channels, LookSide.Plant);
                string stone = Look(vocabulary, channels, LookSide.Stone);
                plantLooks.Add(plant);
                allLooks.Add(plant);
                allLooks.Add(stone);
                text.Append(path.Substring("Assets/Data/".Length)).Append('\t').Append(JsonUtility.ToJson(channels))
                    .Append('\t').Append(plant).Append('\t').Append(stone).Append('\n');
            }
            text.Append("distinct plant looks\t").Append(plantLooks.Count).Append('\n');
            text.Append("distinct looks over both materials\t").Append(allLooks.Count).Append('\n');
            File.WriteAllText(output, text.ToString());
            Debug.Log($"[SpellLookCensus] wrote {output}: {plantLooks.Count} plant looks, {allLooks.Count} over both");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // A class's own skill or item is sized as that class casts it, the atlas's reading; the rest keep the plain one
        public static EffectChannels Channels(ABuffHandlerFactory handler, IEnumerable<CharacterData> characters)
        {
            return PlayerClassContext.Channels(handler, true, characters);
        }

        static string Look(EffectVocabulary vocabulary, EffectChannels channels, LookSide material)
        {
            channels.material = material;
            EffectKey element = EffectComposer.Element(vocabulary, channels);
            vocabulary.GetEntry(element, material, out LookSide drawn);
            return element + "/" + drawn;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public static class SpellIconCapture
    {
        [MenuItem("Tools/Render/Capture Grammar Spell Icons")]
        public static void All()
        {
            StagePlay.Enter("spell-icons", 360f);
        }
    }

    // Every real character spell and buff handler, not a hand-maintained list of names or art assignments.
    public class SpellIconRun : AStageRun
    {
        [Serializable] class ManifestEntry { public string file, assetPath, ownerName; }
        [Serializable] class Manifest { public List<ManifestEntry> entries = new List<ManifestEntry>(); }
        static readonly int columns = 6;
        static readonly int tileSize = 192;
        static readonly int labelHeight = 32;
        protected override bool shouldStartGame { get { return false; } }

        protected override IEnumerator Run()
        {
            string folder = Path.Combine(StagePlay.CaptureFolder, "spell-icons");
            Directory.CreateDirectory(folder);
            List<string> paths = Sources();
            int rows = Mathf.CeilToInt((float)paths.Count / columns);
            Texture2D sheet = new Texture2D(columns * tileSize, rows * (tileSize + labelHeight),
                TextureFormat.RGBA32, false);
            Color32[] background = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < background.Length; i++)
            {
                background[i] = new Color32(17, 29, 35, 255);
            }
            sheet.SetPixels32(background);
            List<string> manifest = new List<string> { "source\ticon\tlayers\treach\torigin\ttrigger" };
            Manifest jsonManifest = new Manifest();
            EffectVocabulary vocabulary = RenderAssets.Load<EffectVocabulary>(
                "Assets/Render/Spells/Data/EffectVocabulary.asset");
            Material material = RenderAssets.Load<Material>("Assets/Render/Look/Look_Default.mat");
            List<CharacterData> characters = AtlasAssetCatalog.Characters();
            try
            {
                using (SpellIconRenderer renderer = new SpellIconRenderer(_manager.meshes, material))
                {
                    for (int index = 0; index < paths.Count; index++)
                    {
                        string path = paths[index];
                        UnityEngine.Object source = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                        SpellIconRecipe recipe = Compose(source, vocabulary, _manager.spellLooks, characters);
                        if (recipe == null)
                        {
                            throw new InvalidOperationException("No grammar icon for " + path);
                        }
                        Texture2D icon = renderer.Capture(recipe);
                        if (!icon)
                        {
                            throw new InvalidOperationException("No native icon capture for " + path);
                        }
                        try
                        {
                            string file = AssetDatabase.AssetPathToGUID(path) + ".png";
                            File.WriteAllBytes(Path.Combine(folder, file), icon.EncodeToPNG());
                            Tile(sheet, icon, index, source.name);
                            manifest.Add(path + "\t" + file + "\t" + recipe.layers.Count + "\t" + recipe.reach
                                + "\t" + recipe.origin + "\t" + recipe.trigger);
                            jsonManifest.entries.Add(new ManifestEntry { file = file, assetPath = path,
                                ownerName = OwnerName(source, path) });
                        }
                        finally
                        {
                            RenderObjects.Release(icon);
                        }
                        yield return null;
                    }
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(folder, "atlas.png"), sheet.EncodeToPNG());
                File.WriteAllLines(Path.Combine(folder, "manifest.tsv"), manifest);
                File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(jsonManifest, true));
                Debug.Log("[SpellIconRun] Captured " + paths.Count + " grammar icons at " + folder);
            }
            finally
            {
                RenderObjects.Release(sheet);
            }
            StagePlay.Finish(this, true);
        }

        // Each skill or handler read as its own class casts it, the atlas's rule; an unowned one reads plain
        public static SpellIconRecipe Compose(UnityEngine.Object source, EffectVocabulary vocabulary, SpellLooks looks,
            IEnumerable<CharacterData> characters)
        {
            return SpellIconComposer.Compose(source, vocabulary, looks, PlayerClassContext.OwnerOf(source, characters));
        }

        static string OwnerName(UnityEngine.Object source, string path)
        {
            if (source is IGameDataSource data && data.sourceData is CharacterSkillData skill && !string.IsNullOrEmpty(skill.name))
                return skill.name;
            string folder = path.Substring(0, path.LastIndexOf('/'));
            foreach (string guid in AssetDatabase.FindAssets("t:Object", new[] { folder }))
            {
                string sibling = AssetDatabase.GUIDToAssetPath(guid);
                if (sibling != path)
                {
                    UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(sibling);
                    if (asset != null && !(asset is ABuffHandlerFactory)) return asset.name;
                }
            }
            return path.Split('/')[2];
        }

        static List<string> Sources()
        {
            HashSet<string> paths = new HashSet<string>();
            foreach (string type in new[] { "t:ACharacterSkillFactory", "t:ABuffHandlerFactory" })
            {
                foreach (string guid in AssetDatabase.FindAssets(type, new[] { "Assets/Data" }))
                {
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }
            List<string> result = new List<string>(paths);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        static void Tile(Texture2D sheet, Texture2D icon, int index, string label)
        {
            int x = index % columns * tileSize;
            int y = sheet.height - (index / columns + 1) * (tileSize + labelHeight);
            Color[] pixels = new Color[tileSize * tileSize];
            for (int row = 0; row < tileSize; row++)
            {
                for (int column = 0; column < tileSize; column++)
                {
                    Color ink = icon.GetPixelBilinear((column + .5f) / tileSize, (row + .5f) / tileSize);
                    pixels[row * tileSize + column] = Color.Lerp(new Color(.067f, .114f, .137f, 1), ink, ink.a);
                }
            }
            sheet.SetPixels(x, y + labelHeight, tileSize, tileSize, pixels);
            if (label.Length > 29)
            {
                label = label.Substring(0, 29);
            }
            LookSheetFont.Draw(sheet, label, x + 8, y + 23, 1, Color.white);
        }
    }
}

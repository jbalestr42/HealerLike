using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Stage
{
    // AssetDatabase collection only. No scene, gameplay instance or asset mutation is needed.
    public static class AtlasDerivationDump
    {
        [Serializable]
        public class Document
        {
            public int schemaVersion = 1;
            public string generatedAt;
            public string commit;
            public Constants constants = new Constants();
            public Vocabulary vocabulary;
            public List<Distinct> summary = new List<Distinct>();
            public List<EntityRow> entities = new List<EntityRow>();
            public List<HandlerRow> handlers = new List<HandlerRow>();
            public List<ProjectileRow> projectiles = new List<ProjectileRow>();
            public List<CharacterRow> characters = new List<CharacterRow>();
            public List<string> diagnostics = new List<string>();
        }

        [Serializable]
        public class Constants
        {
            public int fewHits = LookDerivation.FewHits;
            public int manyHits = LookDerivation.ManyHits;
            public float quickCadence = LookDerivation.QuickCadence;
            public float slowCadence = LookDerivation.SlowCadence;
            public float lightHealth = LookDerivation.LightHealth;
            public float heavyHealth = LookDerivation.HeavyHealth;
            public float shortRange = LookDerivation.ShortRange;
            public float midRange = LookDerivation.MidRange;
            public float spearSpeed = LookDerivation.SpearSpeed;
            public float swarmCurve = EffectDerivation.SwarmCurve;
            public float defaultHealth = LookDerivation.DefaultHealth;
            public float defaultAttackRate = LookDerivation.DefaultAttackRate;
            public float defaultRange = LookDerivation.DefaultRange;
            public string countComparison = ">=many,>=few,else One";
            public string ascendingComparison = "<=lower,<=upper,else highest";
            public string spearComparison = ">=spearSpeed";
            public string swarmComparison = ">=swarmCurve";
        }

        [Serializable]
        public class Vocabulary
        {
            public string path;
            public bool isReachPinned;
            public float pinnedReach;
            public float shortReach;
            public float midReach;
            public float longReach;
        }

        [Serializable]
        public class Distinct
        {
            public string collection;
            public string side;
            public string channel;
            public int distinctCount;
            public bool isConstant;
            public string[] values;
        }

        [Serializable]
        public class Channels
        {
            public string side, head, count, stem, mass, reach, accessory, accessoryHead, accent;

            public Channels(UnitChannels value)
            {
                side = value.side.ToString();
                head = value.head.ToString();
                count = value.count.ToString();
                stem = value.stem.ToString();
                mass = value.mass.ToString();
                reach = value.reach.ToString();
                accessory = value.accessory.ToString();
                accessoryHead = value.accessoryHead.ToString();
                accent = value.accent.ToString();
            }
        }

        [Serializable]
        public class EntityRow
        {
            public string path, name, side;
            public Inputs raw;
            public Channels channels;
            public string view;
            public bool authoredOverride;
            public float effectiveReach;
        }

        [Serializable]
        public class Inputs
        {
            public float health, cadence, range;
            public int hits;
            public bool splash;
            public string skillKind, primarySkill, dominantProjectile, fallbackAccessory;
            public HeadInput primary;
            public HeadInput secondary;
            public ProjectileRow[] primaryPrefabs;
        }

        [Serializable]
        public class HeadInput
        {
            public bool exists;
            public bool usesProjectile;
            public string fixedHead;
            public ProjectileRow projectile;
        }

        [Serializable]
        public class HandlerRow
        {
            public string path, name, side, handlerKind, durationType;
            public float duration;
            public bool periodic;
            public List<BuffInput> buffs = new List<BuffInput>();
            public string family, group, tempo, element;
            public float periodSeconds;
        }

        [Serializable]
        public class BuffInput
        {
            public string kind, consumer, attribute;
            public bool hasConsumer, hasModifier, prevention;
            public float harm, delta, polarity;
        }

        [Serializable]
        public class ProjectileRow
        {
            public string path, name, head, delivery;
            public bool chain, held, curved, arc, homing;
            public float speed, curveMultiplier;
        }

        [Serializable]
        public class CharacterRow
        {
            public string path, name, view;
        }

        [MenuItem("Tools/Render/Dump Atlas Derivation")]
        public static void Write()
        {
            Document document = Collect();
            string directory = System.Environment.GetEnvironmentVariable("RENDER_ATLAS_DIR");
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.Combine("Logs", "Atlas");
            }
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "derivation.json"), JsonUtility.ToJson(document, true));
            Debug.Log($"[AtlasDerivationDump] {document.entities.Count} entity-side rows, "
                + $"{document.handlers.Count} handler-side rows, {document.projectiles.Count} projectiles, "
                + $"{document.characters.Count} characters written to {directory}");
        }

        public static Document Collect()
        {
            return AtlasDerivationCollector.Collect();
        }
    }
}

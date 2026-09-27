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
            public int schemaVersion = 2;
            public string generatedAt;
            public string commit;
            public Constants constants = new Constants();
            public Vocabulary vocabulary;
            public List<Distinct> summary = new List<Distinct>();
            public List<EntityRow> entities = new List<EntityRow>();
            public List<HandlerRow> handlers = new List<HandlerRow>();
            public List<ProjectileRow> projectiles = new List<ProjectileRow>();
            public List<CharacterRow> characters = new List<CharacterRow>();
            public List<SpellCellRow> spellCells = new List<SpellCellRow>();
            public List<SpellEntryRow> spellEntries = new List<SpellEntryRow>();
            public List<SpellPieceRow> spellPieces = new List<SpellPieceRow>();
            public List<SpellRow> spells = new List<SpellRow>();
            public List<HealerSkillRow> healerSkills = new List<HealerSkillRow>();
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
            public float LightMagnitudeMax = EffectDerivation.LightMagnitudeMax;
            public float SolidMagnitudeMax = EffectDerivation.SolidMagnitudeMax;
            public float lightMagnitudeScale = 1f;
            public float solidMagnitudeScale = 1.15f;
            public float heavyMagnitudeScale = 1.3f;
            public string magnitudeComparison = "<=LightMagnitudeMax,<=SolidMagnitudeMax,else Heavy";
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
        public class SpellChannels
        {
            public string operation, aspect, magnitude, reach, delivery, trigger, side, origin, family, group, tempo;
            public float periodSeconds;
            public SpellChannels(EffectChannels value)
            {
                operation = value.operation.ToString(); aspect = value.aspect.ToString(); magnitude = value.magnitude.ToString();
                reach = value.reach.ToString(); delivery = value.delivery.ToString(); trigger = value.trigger.ToString();
                side = value.side.ToString(); origin = value.origin.ToString(); family = value.family.ToString();
                group = value.group.ToString(); tempo = value.tempo.ToString(); periodSeconds = value.periodSeconds;
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

        [Serializable] public class SpellCellRow
        {
            public string operation, aspect, once, periodic, accent;
            public bool hasPeriodic;
        }
        [Serializable] public class SpellEntryRow
        {
            public string element, motion, socket, count, colourRole;
            public int minCount, shapePartCount;
            public float cycleSeconds, scale, entranceSeconds, releaseSeconds, groundRadius, groundStrength;
            public bool billboard, closesOverHead, scalesWithAmount, isShield, avoidHead, stackBeads, criticalRings, sideRim;
        }
        [Serializable] public class SpellPieceRow { public string kind, key; }
        [Serializable] public class SpellRow
        {
            public string path, kind, ownerName, side, durationType, family, group, tempo, element;
            public float duration, periodSeconds;
            public bool isPeriodic;
            public List<BuffInput> buffs = new List<BuffInput>();
            public SpellChannels channels;
            public List<SpellChannels> layers = new List<SpellChannels>();
        }
        [Serializable] public class HealerSkillRow
        {
            public string path, name, skillClass, entityType, reach, origin;
            public bool isSingle;
            public List<Channels> layers = new List<Channels>();
            public List<string> handlerPaths = new List<string>();
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
            public string path, name, head, delivery, effectDelivery, deliveryPath, deliveryStyle, deliveryFamily;
            public bool deliveryBouncing, deliverySplash;
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
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static Document Collect()
        {
            return AtlasDerivationCollector.Collect();
        }
    }
}

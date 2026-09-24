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
            Document document = new Document();
            document.generatedAt = DateTime.UtcNow.ToString("o");
            document.commit = Commit();
            CreatureLooks looks = Required<CreatureLooks>("Assets/Render/Creatures/Data/CreatureLooks.asset");
            LookVocabulary vocabulary = looks.vocabulary;
            if (vocabulary == null)
            {
                throw new InvalidOperationException("CreatureLooks has no vocabulary");
            }
            document.vocabulary = new Vocabulary
            {
                path = AssetDatabase.GetAssetPath(vocabulary), isReachPinned = vocabulary.isReachPinned,
                pinnedReach = vocabulary.pinnedReach, shortReach = vocabulary.roots[ReachBand.Short].reach,
                midReach = vocabulary.roots[ReachBand.Mid].reach, longReach = vocabulary.roots[ReachBand.Long].reach
            };
            foreach (string path in Paths<EntityData>("Assets/Data"))
            {
                EntityData data = Required<EntityData>(path);
                ASkillFactory primary = LookDerivation.Primary(data);
                Inputs inputs = new Inputs
                {
                    health = LookDerivation.Health(data), cadence = LookDerivation.Cadence(primary, data),
                    hits = LookDerivation.Hits(primary, data),
                    splash = primary is AreaOfEffectSkillFactory || SkillWalker.HasSplash(primary, data),
                    range = SkillWalker.ReadAttribute(data, AttributeType.Range, LookDerivation.DefaultRange),
                    skillKind = primary == null ? "None" : primary.GetType().Name,
                    primarySkill = AssetDatabase.GetAssetPath(primary),
                    dominantProjectile = AssetDatabase.GetAssetPath(SkillWalker.DominantPrefab(primary)),
                    primary = Head(primary), secondary = Head(LookDerivation.Secondary(data)),
                    primaryPrefabs = SkillWalker.Prefabs(primary).Select(Projectile).ToArray(),
                    fallbackAccessory = HeadDerivation.FallbackAccessory(data, HeadDerivation.Accent(primary)).ToString()
                };
                foreach (Entity.EntityType side in new[] { Entity.EntityType.Player, Entity.EntityType.Computer })
                {
                    UnitChannels channels = LookDerivation.Channels(data, side);
                    document.entities.Add(new EntityRow
                    {
                        path = path, name = data.name, side = side.ToString(), raw = inputs,
                        channels = new Channels(channels), view = AssetDatabase.GetAssetPath(looks.GetView(data, side)),
                        authoredOverride = looks.entities.ContainsKey(data),
                        effectiveReach = vocabulary.Reach(channels.reach)
                    });
                }
                if (primary == null)
                {
                    document.diagnostics.Add(path + ": passive-only unit without primary skill; supported Bud fallback.");
                }
            }
            foreach (string path in Paths<ABuffHandlerFactory>("Assets/Data"))
            {
                ABuffHandlerFactory handler = Required<ABuffHandlerFactory>(path);
                foreach (bool same in new[] { true, false })
                {
                    EffectChannels channels = EffectDerivation.Channels(handler, same);
                    HandlerRow row = new HandlerRow
                    {
                        path = path, name = handler.name, side = same ? "Same" : "Opposing",
                        handlerKind = handler.GetType().Name, durationType = handler.durationType.ToString(),
                        duration = handler.duration, periodic = EffectDerivation.IsPeriodic(handler),
                        family = channels.family.ToString(), group = channels.group.ToString(),
                        tempo = channels.tempo.ToString(), periodSeconds = channels.periodSeconds,
                        element = EffectComposer.Element(channels).ToString()
                    };
                    if (handler.buffFactoryList != null)
                    {
                        foreach (ABuffFactory buff in handler.buffFactoryList)
                        {
                            AConsumerFactory consumer = EffectDerivation.Consumer(buff);
                            bool modifier = EffectDerivation.TryModifier(buff, out AttributeType type, out float delta);
                            row.buffs.Add(new BuffInput
                            {
                                kind = buff == null ? "None" : buff.GetType().Name,
                                consumer = AssetDatabase.GetAssetPath(consumer), hasConsumer = consumer != null,
                                harm = consumer == null ? 0f : EffectDerivation.Harm(consumer),
                                hasModifier = modifier, attribute = type.ToString(), delta = delta,
                                polarity = EffectDerivation.Polarity(type), prevention = buff is InvincibilityBuffFactory
                            });
                        }
                    }
                    document.handlers.Add(row);
                }
            }
            foreach (string path in Paths<GameObject>("Assets/Prefabs/Projectiles"))
            {
                document.projectiles.Add(Projectile(Required<GameObject>(path)));
            }
            foreach (string path in Paths<CharacterData>("Assets/Data"))
            {
                CharacterData data = Required<CharacterData>(path);
                GameObject view = looks.GetView(data);
                if (view == null)
                {
                    throw new InvalidOperationException(path + " has no character view");
                }
                document.characters.Add(new CharacterRow { path = path, name = data.name, view = AssetDatabase.GetAssetPath(view) });
            }
            Summarize(document);
            return document;
        }

        static HeadInput Head(ASkillFactory skill)
        {
            return new HeadInput
            {
                exists = skill != null,
                usesProjectile = skill is ShootProjectileSkillFactory || skill is ConfigurableSkillFactory,
                fixedHead = skill == null ? "Bud" : HeadDerivation.Head(skill).ToString(),
                projectile = Projectile(SkillWalker.DominantPrefab(skill))
            };
        }

        static ProjectileRow Projectile(GameObject prefab)
        {
            ProjectileRow row = new ProjectileRow
            {
                path = AssetDatabase.GetAssetPath(prefab), name = prefab == null ? "None" : prefab.name,
                head = LookDerivation.DeliveryHead(prefab).ToString(), delivery = EffectDerivation.Delivery(prefab).ToString()
            };
            if (prefab == null)
            {
                return row;
            }
            ChainLightningProjectile chain = prefab.GetComponent<ChainLightningProjectile>();
            CurvedHomingProjectileBehaviour curved = prefab.GetComponent<CurvedHomingProjectileBehaviour>();
            HomingProjectileBehaviour homing = prefab.GetComponent<HomingProjectileBehaviour>();
            row.chain = chain != null;
            row.held = chain != null && SkillWalker.IsHeld(chain);
            row.curved = curved != null;
            row.arc = prefab.GetComponent<ArcHomingProjectileBehaviour>() != null;
            row.homing = homing != null && homing.data != null;
            row.speed = row.homing ? homing.data.speed : 0f;
            row.curveMultiplier = curved != null && curved.data != null ? curved.data.curveMultiplier : 0f;
            return row;
        }

        static void Summarize(Document document)
        {
            foreach (string side in new[] { "Player", "Computer" })
            {
                EntityRow[] rows = document.entities.Where(row => row.side == side).ToArray();
                foreach (var field in typeof(Channels).GetFields())
                {
                    Summary(document, "entities", side, field.Name, rows.Select(row => (string)field.GetValue(row.channels)));
                }
                Summary(document, "entities", side, "effectiveReach", rows.Select(row => row.effectiveReach.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
            }
            foreach (string side in new[] { "Same", "Opposing" })
            {
                HandlerRow[] rows = document.handlers.Where(row => row.side == side).ToArray();
                foreach (string field in new[] { "family", "group", "tempo", "element" })
                {
                    Summary(document, "handlers", side, field, rows.Select(row => (string)typeof(HandlerRow).GetField(field).GetValue(row)));
                }
                Summary(document, "handlers", side, "periodSeconds", rows.Select(row => row.periodSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
            }
            Summary(document, "projectiles", "", "head", document.projectiles.Select(row => row.head));
            Summary(document, "projectiles", "", "delivery", document.projectiles.Select(row => row.delivery));
            Summary(document, "characters", "", "view", document.characters.Select(row => row.view));
        }

        static void Summary(Document document, string collection, string side, string channel, IEnumerable<string> values)
        {
            string[] distinct = values.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray();
            document.summary.Add(new Distinct { collection = collection, side = side, channel = channel,
                distinctCount = distinct.Length, isConstant = distinct.Length == 1, values = distinct });
        }

        static string[] Paths<T>(string folder) where T : UnityEngine.Object
        {
            string[] paths = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (paths.Length == 0)
            {
                throw new InvalidOperationException("No " + typeof(T).Name + " assets under " + folder);
            }
            return paths;
        }

        static T Required<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null)
            {
                throw new InvalidOperationException("Missing " + typeof(T).Name + ": " + path);
            }
            return value;
        }

        static string Commit()
        {
            using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "rev-parse HEAD")
            { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }))
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

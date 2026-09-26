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
    public static class AtlasDerivationCollector
    {
        public static Document Collect()
        {
            Document document = new Document();
            document.generatedAt = DateTime.UtcNow.ToString("o");
            document.commit = AtlasAssetCatalog.Commit();
            CreatureLooks looks = AtlasAssetCatalog.Required<CreatureLooks>(
                "Assets/Render/Creatures/Data/CreatureLooks.asset");
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
            foreach (string path in AtlasAssetCatalog.Paths<EntityData>("Assets/Data"))
            {
                EntityData data = AtlasAssetCatalog.Required<EntityData>(path);
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
                    fallbackAccessory = HeadDerivation.FallbackAccessory(data,
                        HeadDerivation.Accent(primary)).ToString()
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
                    document.diagnostics.Add(path
                        + ": passive-only unit without primary skill; supported Bud fallback.");
                }
            }
            foreach (string path in AtlasAssetCatalog.Paths<ABuffHandlerFactory>("Assets/Data"))
            {
                ABuffHandlerFactory handler = AtlasAssetCatalog.Required<ABuffHandlerFactory>(path);
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
                                polarity = EffectDerivation.Polarity(type),
                                    prevention = buff is InvincibilityBuffFactory
                            });
                        }
                    }
                    document.handlers.Add(row);
                }
            }
            foreach (string path in AtlasAssetCatalog.Paths<GameObject>("Assets/Prefabs/Projectiles"))
            {
                document.projectiles.Add(Projectile(AtlasAssetCatalog.Required<GameObject>(path)));
            }
            foreach (string path in AtlasAssetCatalog.Paths<CharacterData>("Assets/Data"))
            {
                CharacterData data = AtlasAssetCatalog.Required<CharacterData>(path);
                GameObject view = looks.GetView(data);
                if (view == null)
                {
                    throw new InvalidOperationException(path + " has no character view");
                }
                document.characters.Add(new CharacterRow { path = path, name = data.name,
                    view = AssetDatabase.GetAssetPath(view) });
            }
            AtlasDerivationSummary.Append(document);
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
                head = LookDerivation.DeliveryHead(prefab).ToString(),
                    delivery = EffectDerivation.Delivery(prefab).ToString()
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

    }
}

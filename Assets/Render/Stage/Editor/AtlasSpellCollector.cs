using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using SpellCellRow = HealerLike.Render.Stage.AtlasDerivationDump.SpellCellRow;
using SpellEntryRow = HealerLike.Render.Stage.AtlasDerivationDump.SpellEntryRow;
using SpellPieceRow = HealerLike.Render.Stage.AtlasDerivationDump.SpellPieceRow;
using SpellRow = HealerLike.Render.Stage.AtlasDerivationDump.SpellRow;
using HealerSkillRow = HealerLike.Render.Stage.AtlasDerivationDump.HealerSkillRow;
using SpellChannels = HealerLike.Render.Stage.AtlasDerivationDump.SpellChannels;

namespace HealerLike.Render.Stage
{
    public static partial class AtlasDerivationCollector
    {
        static void AppendSpellAtlas(AtlasDerivationDump.Document document, EffectVocabulary vocabulary)
        {
            foreach (var pair in vocabulary.table.OrderBy(pair => pair.Key.operation.ToString())
                .ThenBy(pair => pair.Key.aspect.ToString()))
            {
                string accent = AccentHex(vocabulary, pair.Value.once, pair.Key.operation);
                document.spellCells.Add(new SpellCellRow { operation = pair.Key.operation.ToString(),
                    aspect = pair.Key.aspect.ToString(), once = pair.Value.once.ToString(),
                    hasPeriodic = pair.Value.hasPeriodic, periodic = pair.Value.periodic.ToString(), accent = accent });
            }
            foreach (EffectElement element in Enum.GetValues(typeof(EffectElement)))
            {
                ElementEntry entry;
                if (!vocabulary.elements.TryGetValue(element, out entry) || entry == null) continue;
                EffectPresentation p = entry.presentation;
                document.spellEntries.Add(new SpellEntryRow { element = element.ToString(), motion = entry.motion.ToString(),
                    socket = entry.socket.ToString(), count = entry.count.ToString(), minCount = entry.minCount,
                    cycleSeconds = entry.cycleSeconds, shapePartCount = EffectComposer.Shapes(entry),
                    scale = p == null ? 1f : p.scale, billboard = p != null && p.billboard,
                    closesOverHead = p != null && p.closesOverHead, scalesWithAmount = p != null && p.scalesWithAmount,
                    isShield = p != null && p.isShield, avoidHead = p == null || p.avoidHead,
                    colourRole = p == null ? "" : p.colourRole.ToString(),
                    entranceSeconds = p == null ? 0f : p.entranceSeconds, releaseSeconds = p == null ? 0f : p.releaseSeconds,
                    groundRadius = entry.groundRadius, groundStrength = entry.groundStrength,
                    stackBeads = entry.stackBeads != null && entry.stackBeads.Length > 0,
                    criticalRings = entry.criticalRings != null && entry.criticalRings.Length > 0,
                    sideRim = entry.sideRim != null && entry.sideRim.Length > 0 });
            }
            AddPieces(document, "reach", vocabulary.reach.Keys.Select(k => k.ToString()));
            AddPieces(document, "delivery", vocabulary.delivery.Keys.Select(k => k.ToString()));
            AddPieces(document, "trigger", vocabulary.trigger.Keys.Select(k => k.ToString()));
            AddPieces(document, "side", vocabulary.side.Keys.Select(k => k.ToString()));
            AddPieces(document, "origin", vocabulary.origin.Keys.Select(k => k.ToString()));
            foreach (string path in AtlasAssetCatalog.Paths<ABuffHandlerFactory>("Assets/Data"))
            {
                ABuffHandlerFactory handler = AtlasAssetCatalog.Required<ABuffHandlerFactory>(path);
                foreach (bool same in new[] { true, false })
                {
                    EffectChannels channels = EffectDerivation.Channels(handler, same);
                    SpellRow row = new SpellRow { path = path, kind = FirstFolder(path), ownerName = OwnerName(path),
                        side = same ? "Same" : "Opposing", durationType = handler.durationType.ToString(),
                        duration = handler.duration, isPeriodic = EffectDerivation.IsPeriodic(handler),
                        periodSeconds = EffectDerivation.Period(handler), channels = new SpellChannels(channels),
                        family = channels.family.ToString(), group = channels.group.ToString(), tempo = channels.tempo.ToString(),
                        element = EffectComposer.Element(vocabulary, channels).ToString() };
                    foreach (EffectChannels layer in EffectDerivation.Layers(handler, same)) row.layers.Add(new SpellChannels(layer));
                    foreach (ABuffFactory buff in EffectDerivation.Buffs(handler))
                    {
                        AConsumerFactory consumer = EffectDerivation.Consumer(buff);
                        bool modifier = EffectDerivation.TryModifier(buff, out AttributeType type, out float delta);
                        row.buffs.Add(new AtlasDerivationDump.BuffInput { kind = buff.GetType().Name,
                            consumer = AssetDatabase.GetAssetPath(consumer), hasConsumer = consumer != null,
                            harm = consumer == null ? 0f : EffectDerivation.Harm(consumer), hasModifier = modifier,
                            attribute = type.ToString(), delta = delta, polarity = EffectDerivation.Polarity(type),
                            prevention = buff is InvincibilityBuffFactory });
                    }
                    document.spells.Add(row);
                }
            }
            foreach (string path in AtlasAssetCatalog.Paths<BaseCharacterSkillData>("Assets/Data/CharacterSkills"))
            {
                BaseCharacterSkillData data = AtlasAssetCatalog.Required<BaseCharacterSkillData>(path);
                SpellIconDescription description = SpellIconDerivation.Read(data);
                HealerSkillRow row = new HealerSkillRow { path = path, name = data.name, skillClass = data.GetType().Name,
                    isSingle = data.isSingle, entityType = data.entityType.ToString(),
                    reach = description == null ? "" : description.context.targetCount == 1 ? "Single" : "All",
                    origin = "Healer" };
                if (description != null)
                {
                    foreach (ABuffHandlerFactory handler in description.handlers)
                    {
                        row.handlerPaths.Add(AssetDatabase.GetAssetPath(handler));
                        foreach (EffectChannels layer in EffectDerivation.Layers(handler, description.isSameSide, description.context))
                            row.layers.Add(new SpellChannels(layer));
                    }
                    foreach (EffectChannels layer in description.layers) row.layers.Add(new SpellChannels(layer));
                }
                document.healerSkills.Add(row);
            }
        }

        static void AddPieces(AtlasDerivationDump.Document document, string kind, IEnumerable<string> keys)
        { foreach (string key in keys.OrderBy(k => k)) document.spellPieces.Add(new SpellPieceRow { kind = kind, key = key }); }
        static string FirstFolder(string path) { string[] p = path.Split('/'); return p.Length > 2 ? p[2] : ""; }
        static string OwnerName(string path)
        {
            string folder = path.Substring(0, path.LastIndexOf('/'));
            string[] assets = AssetDatabase.FindAssets("t:Object", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p != path).ToArray();
            return assets.Length == 0 ? FirstFolder(path) : AssetDatabase.LoadMainAssetAtPath(assets[0]).name;
        }
        static string AccentHex(EffectVocabulary vocabulary, EffectElement element, EffectOperation operation)
        {
            ElementEntry entry; if (!vocabulary.elements.TryGetValue(element, out entry) || entry == null || vocabulary.palette == null) return "";
            Color c = vocabulary.palette.Colour(entry.presentation == null ? ColourRole.Accent : entry.presentation.colourRole,
                operation == EffectOperation.Heal ? EffectFamily.Heal : EffectFamily.Damage);
            return ColorUtility.ToHtmlStringRGBA(c);
        }
    }
}

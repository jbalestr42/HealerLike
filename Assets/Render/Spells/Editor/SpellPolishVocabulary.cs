using System;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using static HealerLike.Render.Spells.Editor.SpellPolishParts;

namespace HealerLike.Render.Spells.Editor
{
    // Explicit authoring, never an import hook. Saved parts and presentation remain editable in Studio.
    public static class SpellPolishVocabulary
    {
        public const string AssetPath = "Assets/Render/Spells/Data/EffectVocabulary.asset";

        [MenuItem("Tools/Render/Author Polished Spell Vocabulary")]
        public static void Author()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(AssetPath);
            if (vocabulary == null)
            {
                throw new InvalidOperationException("Missing effect vocabulary: " + AssetPath);
            }

            Undo.RecordObject(vocabulary, "Author polished spell vocabulary");
            Apply(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[SpellPolishVocabulary] Authored all 20 compositions; palette and grammar mappings retained.");
        }

        public static void Apply(EffectVocabulary vocabulary)
        {
            if (vocabulary == null)
            {
                throw new ArgumentNullException(nameof(vocabulary));
            }

            vocabulary.entries[EffectKey.Burst] = Entry(Burst(), EffectMotionKind.Burst,
                EffectSocket.Body, 1.05f);
            vocabulary.entries[EffectKey.Rise] = Entry(Heal(),
                EffectMotionKind.Rise, EffectSocket.Body, 1.65f, EffectCount.Amount, 3);
            vocabulary.entries[EffectKey.Stalks] = Entry(Stalks(), EffectMotionKind.Grow,
                EffectSocket.Feet, 2f, EffectCount.Stacks, 3);
            vocabulary.entries[EffectKey.Drips] = Entry(Motes("Poison drop ", 5, 0.5f, 0.12f, 0.46f, true),
                EffectMotionKind.Fall, EffectSocket.UnderHead, 1.8f, EffectCount.Stacks, 3);
            vocabulary.entries[EffectKey.Orbit] = Entry(new[] {
                Ring("Outer blessing", 3.05f, -0.25f, 0.075f, rotation: new Vector3(14, 0, 0)),
                Ring("Inner blessing", 2.85f, -0.15f, 0.085f, rotation: new Vector3(-18, 35, 0)) },
                EffectMotionKind.Orbit, EffectSocket.Body, 2.4f);
            vocabulary.entries[EffectKey.Plates] = Entry(Petals("Armor petal ", 6, 1.5f, -0.12f,
                new Vector3(1.35f, 1.45f, 0.3f), angleOffset: 180f), EffectMotionKind.Close, EffectSocket.Body,
                1.1f, EffectCount.Charges, 1);
            vocabulary.entries[EffectKey.Bud] = Entry(Petals("Ward petal ", 6, 1.08f, 0.12f,
                new Vector3(0.74f, 1.65f, 0.26f), false, -12), EffectMotionKind.Close, EffectSocket.Body, 1.1f);
            vocabulary.entries[EffectKey.Press] = Entry(Press(), EffectMotionKind.Press,
                EffectSocket.AboveHead, 1.65f, EffectCount.Stacks, 4);
            vocabulary.entries[EffectKey.Crack] = Entry(Petals("Fracture ", 6, 1.08f, -0.12f,
                new Vector3(0.58f, 1f, 0.26f), true, 16), EffectMotionKind.Shed,
                EffectSocket.Body, 1.65f, EffectCount.Stacks, 3);
            vocabulary.entries[EffectKey.ManaUp] = Entry(Motes("Mana pearl ", 5, 1.25f, -0.2f, 0.42f, false),
                EffectMotionKind.Rise, EffectSocket.Body, 1.45f, EffectCount.Amount, 3);
            vocabulary.entries[EffectKey.ManaDown] = Entry(Motes("Spent mana ", 5, 0.55f, 0.15f, 0.42f, true),
                EffectMotionKind.Fall, EffectSocket.UnderHead, 1.45f, EffectCount.Amount, 3);
            vocabulary.entries[EffectKey.Beam] = Entry(Beam(), EffectMotionKind.Orbit,
                EffectSocket.Link, 1.3f);
            vocabulary.entries[EffectKey.Ring] = Entry(Zone(false), EffectMotionKind.Orbit,
                EffectSocket.Ground, 2.8f);
            vocabulary.entries[EffectKey.Litter] = Entry(Zone(true), EffectMotionKind.Orbit,
                EffectSocket.Ground, 2.4f);
            vocabulary.legacyTable[new EffectCell(EffectOperation.Ward, EffectAspect.Prevention)] =
                new EffectCellEntry(EffectKey.Bud, EffectKey.Bud, false);
            foreach (EffectAspect aspect in Enum.GetValues(typeof(EffectAspect)))
            {
                vocabulary.legacyTable[new EffectCell(EffectOperation.ManaDrain, aspect)] =
                    new EffectCellEntry(EffectKey.ManaDown, EffectKey.ManaDown, false);
            }

            Configure(vocabulary);
            SpellPolishGround.Apply(vocabulary);
            ApplyStone(vocabulary);
            ApplyKinds(vocabulary);
            ApplyEventKinds(vocabulary);
            ApplyStoneEventKinds(vocabulary);
        }

        // Only the Plant Boon offence kinds: the saved entries and cells are read, never rewritten
        [MenuItem("Tools/Render/Author Boon Kind Entries")]
        public static void AuthorKinds()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(AssetPath);
            if (vocabulary == null)
            {
                throw new InvalidOperationException("Missing effect vocabulary: " + AssetPath);
            }

            Undo.RecordObject(vocabulary, "Author boon kind entries");
            ApplyKinds(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[SpellPolishVocabulary] Authored the six Plant Boon offence kinds; Stone draws them in Plant.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        // Six Boon offence kinds in Plant. Stone has none of its own and draws these through the Plant fallback.
        // The Orbit entry lends presentation, ground, beads and rim: kind changes construction and motion only
        public static void ApplyKinds(EffectVocabulary vocabulary)
        {
            if (vocabulary == null)
            {
                throw new ArgumentNullException(nameof(vocabulary));
            }

            ElementEntry orbit = vocabulary.entries[EffectKey.Orbit];
            Kind(vocabulary, EffectKind.Projectile, Kind(orbit, "Boon dart", Dart(), EffectMotionKind.Orbit,
                EffectSocket.Body, 1.6f));
            Kind(vocabulary, EffectKind.Volume, Kind(orbit, "Boon seeds", Seeds(), EffectMotionKind.Orbit,
                EffectSocket.AboveHead, 2.2f));
            Kind(vocabulary, EffectKind.Rate, Kind(orbit, "Boon cadence", Cadence(), EffectMotionKind.Press,
                EffectSocket.Body, 1f));
            Kind(vocabulary, EffectKind.Conditional, Kind(orbit, "Boon brackets", Brackets(), EffectMotionKind.Close,
                EffectSocket.Body, 1.4f));
            Kind(vocabulary, EffectKind.Positional, Kind(orbit, "Boon footring", Footring(), EffectMotionKind.Grow,
                EffectSocket.Feet, 2.4f));
            Kind(vocabulary, EffectKind.Flat, Kind(orbit, "Boon canopy", Canopy(), EffectMotionKind.Orbit,
                EffectSocket.AboveHead, 2.4f));
        }

        // Only the five kinds of the game's October content: the saved entries, cells and other kinds are read, never
        // rewritten
        [MenuItem("Tools/Render/Author Event Kind Entries")]
        public static void AuthorEventKinds()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(AssetPath);
            if (vocabulary == null)
            {
                throw new InvalidOperationException("Missing effect vocabulary: " + AssetPath);
            }

            Undo.RecordObject(vocabulary, "Author event kind entries");
            ApplyEventKinds(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[SpellPolishVocabulary] Authored Spark, Echo, Tether, Sprout and Stem; Stone draws them in Plant.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        // Reactive, Echo, Link, Summon and Growth in Plant, built like the six Boon kinds from the Orbit entry.
        // Growth also refines the Bane defence cell: the Hungering Mask's battle growth costs health
        public static void ApplyEventKinds(EffectVocabulary vocabulary)
        {
            if (vocabulary == null)
            {
                throw new ArgumentNullException(nameof(vocabulary));
            }

            ElementEntry orbit = vocabulary.entries[EffectKey.Orbit];
            Kind(vocabulary, EffectKind.Reactive, Kind(orbit, "Reactive spark", Spark(), EffectMotionKind.Burst,
                EffectSocket.Body, 1.2f));
            Kind(vocabulary, EffectKind.Echo, Kind(orbit, "Echo blades", Echo(), EffectMotionKind.Press,
                EffectSocket.Body, 1.1f));
            Kind(vocabulary, EffectKind.Link, Kind(orbit, "Soul tether", Tether(), EffectMotionKind.Orbit,
                EffectSocket.AboveHead, 2.2f));
            Kind(vocabulary, EffectKind.Summon, Kind(orbit, "Summon sprout", Sprout(), EffectMotionKind.Rise,
                EffectSocket.Body, 1.6f));
            ElementEntry stem = Kind(orbit, "Growth stem", Stem(), EffectMotionKind.Grow, EffectSocket.Body, 2f);
            stem.count = EffectCount.Stacks;
            stem.minCount = 3;
            Kind(vocabulary, EffectKind.Growth, stem);
            vocabulary.kinds[new EffectKindCell(EffectOperation.Bane, EffectAspect.Defence, EffectKind.Growth)] = stem;
        }

        // Only the Stone cells of the five event kinds: the saved Plant entries are read, never rewritten
        [MenuItem("Tools/Render/Author Stone Event Kind Entries")]
        public static void AuthorStoneEventKinds()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(AssetPath);
            if (vocabulary == null)
            {
                throw new InvalidOperationException("Missing effect vocabulary: " + AssetPath);
            }

            Undo.RecordObject(vocabulary, "Author stone event kind entries");
            ApplyStoneEventKinds(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[SpellPolishVocabulary] Authored Stone Spark, Echo, Tether, Sprout and Stem.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        // Each Stone entry keeps its Plant entry's motion, socket, count, clock and presentation; its beads and rims
        // are chips and faceted rings. The Stem's Stone entry also refines the Bane defence cell, as its Plant one does
        public static void ApplyStoneEventKinds(EffectVocabulary vocabulary)
        {
            if (vocabulary == null)
            {
                throw new ArgumentNullException(nameof(vocabulary));
            }

            StoneKind(vocabulary, EffectKind.Reactive, "Stone spark", StoneSpark());
            StoneKind(vocabulary, EffectKind.Echo, "Stone echo", StoneEcho());
            StoneKind(vocabulary, EffectKind.Link, "Stone tether", StoneTether());
            StoneKind(vocabulary, EffectKind.Summon, "Stone sprout", StoneSprout());
            ElementEntry stem = StoneKind(vocabulary, EffectKind.Growth, "Stone stem", StoneStem());
            vocabulary.kinds[new EffectKindCell(EffectOperation.Bane, EffectAspect.Defence, EffectKind.Growth,
                LookSide.Stone)] = stem;
        }

        static ElementEntry StoneKind(EffectVocabulary vocabulary, EffectKind kind, string label, LookPart[] parts)
        {
            ElementEntry plant = vocabulary.entries[EffectVocabulary.KindKey(kind)];
            ElementEntry entry = Stone(plant, label, parts);
            if (entry.stackBeads.Length > 0)
            {
                entry.stackBeads = Chips("Stack chip ", 5, 1.42f, -0.32f, 0.2f);
            }

            entry.sideRim = Facet(entry.sideRim);
            entry.criticalRings = Facet(entry.criticalRings);
            foreach (LookPart part in entry.parts)
            {
                if (!part.shape.IsValid())
                {
                    Debug.LogError($"[SpellPolishVocabulary] {entry.label}: {part.id} trips a shape profile bound.");
                }
            }

            vocabulary.kinds[new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, kind, LookSide.Stone)] = entry;
            return entry;
        }

        // A smooth ring becomes a faceted one of the same size; anything else becomes a chip in its place
        static LookPart[] Facet(LookPart[] parts)
        {
            LookPart[] faceted = new LookPart[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                faceted[i] = parts[i];
                if (parts[i].primitive == Primitive.Torus)
                {
                    faceted[i].shape = ShapeProfile.Ring(parts[i].shape.tubeRatio, true);
                }
                else
                {
                    faceted[i].primitive = Primitive.Boulder;
                    faceted[i].shape = Slab(0.6f);
                }
            }
            return faceted;
        }

        static void Kind(EffectVocabulary vocabulary, EffectKind kind, ElementEntry entry)
        {
            foreach (LookPart part in entry.parts)
            {
                if (!part.shape.IsValid())
                {
                    Debug.LogError($"[SpellPolishVocabulary] {entry.label}: {part.id} trips a shape profile bound.");
                }
            }

            vocabulary.kinds[new EffectKindCell(EffectOperation.Boon, EffectAspect.Offence, kind)] = entry;
            // Like every other element, the Plant entry is also the element's own entry for Studio and previews
            vocabulary.entries[EffectVocabulary.KindKey(kind)] = entry;
        }

        static ElementEntry Kind(ElementEntry orbit, string label, LookPart[] parts, EffectMotionKind motion,
            EffectSocket socket, float seconds)
        {
            ElementEntry entry = Stone(orbit, label, parts);
            entry.motion = motion;
            entry.socket = socket;
            entry.cycleSeconds = seconds;
            return entry;
        }

        // Only the Stone cells: the saved Plant entries, labels and part edits are read, never rewritten
        [MenuItem("Tools/Render/Author Stone Spell Cells")]
        public static void AuthorStone()
        {
            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(AssetPath);
            if (vocabulary == null)
            {
                throw new InvalidOperationException("Missing effect vocabulary: " + AssetPath);
            }

            Undo.RecordObject(vocabulary, "Author stone spell cells");
            ApplyStone(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[SpellPolishVocabulary] Authored Stone Burst, Rise and Press; other elements draw Plant.");
        }

        // Stone Burst, Rise and Press. Every other element has no Stone cell and draws its Plant entry.
        public static void ApplyStone(EffectVocabulary vocabulary)
        {
            if (vocabulary == null)
            {
                throw new ArgumentNullException(nameof(vocabulary));
            }

            ElementEntry burst = Stone(vocabulary.entries[EffectKey.Burst], "Stone burst", StoneBurst());
            burst.criticalRings = new[] { FacetedRing("Critical outer halo", 2.8f, 0, 0.07f,
                ColourRole.MushroomCapPale, new Vector3(90, 0, 0)) };
            ElementEntry rise = Stone(vocabulary.entries[EffectKey.Rise], "Stone rise", StoneRise());
            rise.criticalRings = new[] { FacetedRing("Critical healing halo", 2.65f, -0.35f, 0.07f) };
            ElementEntry press = Stone(vocabulary.entries[EffectKey.Press], "Stone press", StonePress());
            press.stackBeads = Chips("Stack chip ", 5, 1.42f, -0.32f, 0.2f);
            foreach (EffectAspect aspect in Enum.GetValues(typeof(EffectAspect)))
            {
                vocabulary.cells[new EffectCell(EffectOperation.Damage, aspect, LookSide.Stone)] =
                    new EffectCellEntries(burst, null, false);
                vocabulary.cells[new EffectCell(EffectOperation.Heal, aspect, LookSide.Stone)] =
                    new EffectCellEntries(rise, null, false);
            }
            vocabulary.cells[new EffectCell(EffectOperation.Bane, EffectAspect.Offence, LookSide.Stone)] =
                new EffectCellEntries(press, null, false);
        }

        // Same motion, socket, count, clock, presentation and ground as the Plant entry: material changes
        // construction only
        static ElementEntry Stone(ElementEntry plant, string label, LookPart[] parts)
        {
            return new ElementEntry { label = label, parts = parts,
                presentation = plant.presentation != null ? plant.presentation.Clone() : new EffectPresentation(),
                ground = plant.ground, groundRadius = plant.groundRadius, groundStrength = plant.groundStrength,
                stackBeads = Copy(plant.stackBeads), criticalRings = Copy(plant.criticalRings), sideRim = Copy(plant.sideRim),
                motion = plant.motion, socket = plant.socket, count = plant.count, minCount = plant.minCount,
                cycleSeconds = plant.cycleSeconds };
        }

        static LookPart[] Copy(LookPart[] parts) { return parts != null ? (LookPart[])parts.Clone() : Array.Empty<LookPart>(); }

        static ElementEntry Entry(LookPart[] parts, EffectMotionKind motion, EffectSocket socket,
            float seconds, EffectCount count = EffectCount.Fixed, int minimum = 1)
        {
            return new ElementEntry { parts = parts, motion = motion, socket = socket,
                cycleSeconds = seconds, count = count, minCount = minimum,
                presentation = new EffectPresentation { enabled = true, entranceSeconds = 0.14f,
                    releaseSeconds = 0.38f, motionSpan = 0.45f, idleVisibility = 0.45f } };
        }

        static void Configure(EffectVocabulary vocabulary)
        {
            ElementEntry burst = vocabulary.entries[EffectKey.Burst];
            burst.presentation.billboard = true;
            burst.presentation.scale = 2.2f;
            burst.presentation.cameraDepth = 1.1f;
            burst.presentation.scalesWithAmount = true;
            burst.presentation.avoidHead = false;
            burst.presentation.entranceSeconds = 0.07f;
            burst.criticalRings = new[] { Ring("Critical outer halo", 2.8f, 0, 0.07f,
                ColourRole.MushroomCapPale, new Vector3(90, 0, 0)) };
            ElementEntry rise = vocabulary.entries[EffectKey.Rise];
            rise.presentation.avoidHead = false;
            rise.presentation.scale = 1.65f;
            rise.criticalRings = new[] { Ring("Critical healing halo", 2.65f, -0.35f, 0.07f) };
            vocabulary.entries[EffectKey.Stalks].presentation.scale = 1.35f;
            vocabulary.entries[EffectKey.Drips].presentation.scale = 1.25f;
            vocabulary.entries[EffectKey.Orbit].presentation.scale = 1.15f;
            vocabulary.entries[EffectKey.Crack].presentation.scale = 1.3f;
            vocabulary.entries[EffectKey.Press].presentation.scale = 1.25f;
            vocabulary.entries[EffectKey.ManaUp].presentation.scale = 1.5f;
            vocabulary.entries[EffectKey.ManaDown].presentation.scale = 1.3f;
            vocabulary.entries[EffectKey.Plates].presentation.isShield = true;
            vocabulary.entries[EffectKey.Bud].presentation.closesOverHead = true;
            vocabulary.entries[EffectKey.ManaUp].presentation.colourRole = ColourRole.Mana;
            vocabulary.entries[EffectKey.ManaDown].presentation.colourRole = ColourRole.Mana;
            vocabulary.entries[EffectKey.ManaUp].presentation.avoidHead = false;
            vocabulary.entries[EffectKey.Beam].presentation.linkWidth = 0.065f;
            vocabulary.entries[EffectKey.Beam].presentation.linkBeadSeconds = 1.05f;
            foreach (EffectKey element in new[] { EffectKey.Stalks, EffectKey.Drips,
                EffectKey.Orbit, EffectKey.Plates, EffectKey.Bud, EffectKey.Press, EffectKey.Crack })
            {
                ElementEntry entry = vocabulary.entries[element];
                entry.stackBeads = Motes("Stack pearl ", 5, 1.42f, -0.32f, 0.18f, false);
                entry.sideRim = new[] { Ring("Caster signature", 2.65f, -0.55f, 0.06f, ColourRole.Rim) };
                entry.presentation.releaseSeconds = 0.45f;
                if (element == EffectKey.Press)
                {
                    entry.sideRim = Array.Empty<LookPart>();
                }

                if (element == EffectKey.Drips)
                {
                    entry.sideRim = new[] { Ring("Caster signature", 1.2f, -0.12f, 0.06f, ColourRole.Rim) };
                }
            }
        }
    }
}

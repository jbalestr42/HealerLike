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
            if (vocabulary == null) throw new InvalidOperationException("Missing effect vocabulary: " + AssetPath);
            Undo.RecordObject(vocabulary, "Author polished spell vocabulary");
            Apply(vocabulary);
            EditorUtility.SetDirty(vocabulary);
            AssetDatabase.SaveAssetIfDirty(vocabulary);
            Debug.Log("[SpellPolishVocabulary] Authored all 14 compositions; palette and grammar mappings retained.");
        }

        public static void Apply(EffectVocabulary vocabulary)
        {
            if (vocabulary == null) throw new ArgumentNullException(nameof(vocabulary));
            vocabulary.entries[EffectKey.Burst] = Entry(Burst(), EffectMotionKind.Burst,
                EffectSocket.Body, 1.05f);
            vocabulary.entries[EffectKey.Rise] = Entry(Heal(),
                EffectMotionKind.Rise, EffectSocket.Body, 1.65f, EffectCount.Amount, 3);
            vocabulary.entries[EffectKey.Stalks] = Entry(Stalks(), EffectMotionKind.Grow,
                EffectSocket.Feet, 2f, EffectCount.Stacks, 3);
            vocabulary.entries[EffectKey.Drips] = Entry(Motes("Poison drop ", 5, .5f, .12f, .46f, true),
                EffectMotionKind.Fall, EffectSocket.UnderHead, 1.8f, EffectCount.Stacks, 3);
            vocabulary.entries[EffectKey.Orbit] = Entry(new[] {
                Ring("Outer blessing", 3.05f, -.25f, .075f, rotation: new Vector3(14, 0, 0)),
                Ring("Inner blessing", 2.85f, -.15f, .085f, rotation: new Vector3(-18, 35, 0)) },
                EffectMotionKind.Orbit, EffectSocket.Body, 2.4f);
            vocabulary.entries[EffectKey.Plates] = Entry(Petals("Armor petal ", 6, 1.5f, -.12f,
                new Vector3(1.35f, 1.45f, .3f), angleOffset: 180f), EffectMotionKind.Close, EffectSocket.Body,
                1.1f, EffectCount.Charges, 1);
            vocabulary.entries[EffectKey.Bud] = Entry(Petals("Ward petal ", 6, 1.08f, .12f,
                new Vector3(.74f, 1.65f, .26f), false, -12), EffectMotionKind.Close, EffectSocket.Body, 1.1f);
            vocabulary.entries[EffectKey.Press] = Entry(Press(), EffectMotionKind.Press,
                EffectSocket.AboveHead, 1.65f, EffectCount.Stacks, 4);
            vocabulary.entries[EffectKey.Crack] = Entry(Petals("Fracture ", 6, 1.08f, -.12f,
                new Vector3(.58f, 1f, .26f), true, 16), EffectMotionKind.Shed,
                EffectSocket.Body, 1.65f, EffectCount.Stacks, 3);
            vocabulary.entries[EffectKey.ManaUp] = Entry(Motes("Mana pearl ", 5, 1.25f, -.2f, .42f, false),
                EffectMotionKind.Rise, EffectSocket.Body, 1.45f, EffectCount.Amount, 3);
            vocabulary.entries[EffectKey.ManaDown] = Entry(Motes("Spent mana ", 5, .55f, .15f, .42f, true),
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
                vocabulary.legacyTable[new EffectCell(EffectOperation.ManaDrain, aspect)] =
                    new EffectCellEntry(EffectKey.ManaDown, EffectKey.ManaDown, false);
            Configure(vocabulary);
            SpellPolishGround.Apply(vocabulary);
        }

        static ElementEntry Entry(LookPart[] parts, EffectMotionKind motion, EffectSocket socket,
            float seconds, EffectCount count = EffectCount.Fixed, int minimum = 1)
        {
            return new ElementEntry { parts = parts, motion = motion, socket = socket,
                cycleSeconds = seconds, count = count, minCount = minimum,
                presentation = new EffectPresentation { enabled = true, entranceSeconds = .14f,
                    releaseSeconds = .38f, motionSpan = .45f, idleVisibility = .45f } };
        }

        static void Configure(EffectVocabulary vocabulary)
        {
            ElementEntry burst = vocabulary.entries[EffectKey.Burst];
            burst.presentation.billboard = true;
            burst.presentation.scale = 2.2f;
            burst.presentation.cameraDepth = 1.1f;
            burst.presentation.scalesWithAmount = true;
            burst.presentation.avoidHead = false;
            burst.presentation.entranceSeconds = .07f;
            burst.criticalRings = new[] { Ring("Critical outer halo", 2.8f, 0, .07f,
                ColourRole.MushroomCapPale, new Vector3(90, 0, 0)) };
            ElementEntry rise = vocabulary.entries[EffectKey.Rise];
            rise.presentation.avoidHead = false;
            rise.presentation.scale = 1.65f;
            rise.criticalRings = new[] { Ring("Critical healing halo", 2.65f, -.35f, .07f) };
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
            vocabulary.entries[EffectKey.Beam].presentation.linkWidth = .065f;
            vocabulary.entries[EffectKey.Beam].presentation.linkBeadSeconds = 1.05f;
            foreach (EffectKey element in new[] { EffectKey.Stalks, EffectKey.Drips,
                EffectKey.Orbit, EffectKey.Plates, EffectKey.Bud, EffectKey.Press, EffectKey.Crack })
            {
                ElementEntry entry = vocabulary.entries[element];
                entry.stackBeads = Motes("Stack pearl ", 5, 1.42f, -.32f, .18f, false);
                entry.sideRim = new[] { Ring("Caster signature", 2.65f, -.55f, .06f, ColourRole.Rim) };
                entry.presentation.releaseSeconds = .45f;
                if (element == EffectKey.Press) entry.sideRim = Array.Empty<LookPart>();
                if (element == EffectKey.Drips)
                    entry.sideRim = new[] { Ring("Caster signature", 1.2f, -.12f, .06f, ColourRole.Rim) };
            }
        }
    }
}

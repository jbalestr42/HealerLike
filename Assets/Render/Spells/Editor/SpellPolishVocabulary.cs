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
            vocabulary.elements[EffectElement.Burst] = Entry(Burst(), EffectMotionKind.Burst,
                EffectSocket.Body, 1.05f);
            vocabulary.elements[EffectElement.Rise] = Entry(Heal(),
                EffectMotionKind.Rise, EffectSocket.Body, 1.65f, EffectCount.Amount, 3);
            vocabulary.elements[EffectElement.Stalks] = Entry(Stalks(), EffectMotionKind.Grow,
                EffectSocket.Feet, 2f, EffectCount.Stacks, 3);
            vocabulary.elements[EffectElement.Drips] = Entry(Motes("Poison drop ", 5, .5f, .12f, .46f, true),
                EffectMotionKind.Fall, EffectSocket.UnderHead, 1.8f, EffectCount.Stacks, 3);
            vocabulary.elements[EffectElement.Orbit] = Entry(new[] {
                Ring("Outer blessing", 3.05f, -.25f, .075f, rotation: new Vector3(14, 0, 0)),
                Ring("Inner blessing", 2.85f, -.15f, .085f, rotation: new Vector3(-18, 35, 0)) },
                EffectMotionKind.Orbit, EffectSocket.Body, 2.4f);
            vocabulary.elements[EffectElement.Plates] = Entry(Petals("Armor petal ", 6, 1.18f, -.12f,
                new Vector3(.84f, 1.25f, .3f)), EffectMotionKind.Close, EffectSocket.Body,
                1.1f, EffectCount.Charges, 1);
            vocabulary.elements[EffectElement.Bud] = Entry(Petals("Ward petal ", 6, 1.08f, .12f,
                new Vector3(.74f, 1.65f, .26f), false, -12), EffectMotionKind.Close, EffectSocket.Body, 1.1f);
            vocabulary.elements[EffectElement.Press] = Entry(Press(), EffectMotionKind.Press,
                EffectSocket.AboveHead, 1.65f, EffectCount.Stacks, 4);
            vocabulary.elements[EffectElement.Crack] = Entry(Petals("Fracture ", 6, 1.08f, -.12f,
                new Vector3(.58f, 1f, .26f), true, 16), EffectMotionKind.Shed,
                EffectSocket.Body, 1.65f, EffectCount.Stacks, 3);
            vocabulary.elements[EffectElement.ManaUp] = Entry(Motes("Mana pearl ", 5, 1.25f, -.2f, .42f, false),
                EffectMotionKind.Rise, EffectSocket.Body, 1.45f, EffectCount.Amount, 3);
            vocabulary.elements[EffectElement.ManaDown] = Entry(Motes("Spent mana ", 5, .55f, .15f, .42f, true),
                EffectMotionKind.Fall, EffectSocket.UnderHead, 1.45f, EffectCount.Amount, 3);
            vocabulary.elements[EffectElement.Beam] = Entry(Beam(), EffectMotionKind.Orbit,
                EffectSocket.Link, 1.3f);
            vocabulary.elements[EffectElement.Ring] = Entry(Zone(false), EffectMotionKind.Orbit,
                EffectSocket.Ground, 2.8f);
            vocabulary.elements[EffectElement.Litter] = Entry(Zone(true), EffectMotionKind.Orbit,
                EffectSocket.Ground, 2.4f);
            vocabulary.table[new EffectCell(EffectOperation.Ward, EffectAspect.Prevention)] =
                new EffectCellEntry(EffectElement.Bud, EffectElement.Bud, false);
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
            ElementEntry burst = vocabulary.elements[EffectElement.Burst];
            burst.presentation.billboard = true;
            burst.presentation.scale = 2.2f;
            burst.presentation.cameraDepth = 1.1f;
            burst.presentation.scalesWithAmount = true;
            burst.presentation.avoidHead = false;
            burst.presentation.entranceSeconds = .07f;
            burst.criticalRings = new[] { Ring("Critical outer halo", 2.8f, 0, .07f,
                ColourRole.MushroomCapPale, new Vector3(90, 0, 0)) };
            ElementEntry rise = vocabulary.elements[EffectElement.Rise];
            rise.presentation.avoidHead = false;
            rise.presentation.scale = 1.65f;
            rise.criticalRings = new[] { Ring("Critical healing halo", 2.65f, -.35f, .07f) };
            vocabulary.elements[EffectElement.Stalks].presentation.scale = 1.35f;
            vocabulary.elements[EffectElement.Drips].presentation.scale = 1.25f;
            vocabulary.elements[EffectElement.Orbit].presentation.scale = 1.15f;
            vocabulary.elements[EffectElement.Crack].presentation.scale = 1.3f;
            vocabulary.elements[EffectElement.Press].presentation.scale = 1.25f;
            vocabulary.elements[EffectElement.ManaUp].presentation.scale = 1.5f;
            vocabulary.elements[EffectElement.ManaDown].presentation.scale = 1.3f;
            vocabulary.elements[EffectElement.Plates].presentation.isShield = true;
            vocabulary.elements[EffectElement.Bud].presentation.closesOverHead = true;
            vocabulary.elements[EffectElement.ManaUp].presentation.colourRole = ColourRole.Mana;
            vocabulary.elements[EffectElement.ManaDown].presentation.colourRole = ColourRole.Mana;
            vocabulary.elements[EffectElement.ManaUp].presentation.avoidHead = false;
            vocabulary.elements[EffectElement.Beam].presentation.linkWidth = .065f;
            vocabulary.elements[EffectElement.Beam].presentation.linkBeadSeconds = 1.05f;
            foreach (EffectElement element in new[] { EffectElement.Stalks, EffectElement.Drips,
                EffectElement.Orbit, EffectElement.Plates, EffectElement.Bud, EffectElement.Press, EffectElement.Crack })
            {
                ElementEntry entry = vocabulary.elements[element];
                entry.stackBeads = Motes("Stack pearl ", 5, 1.42f, -.32f, .18f, false);
                entry.sideRim = new[] { Ring("Caster signature", 2.65f, -.55f, .06f, ColourRole.Rim) };
                entry.presentation.releaseSeconds = .45f;
                if (element == EffectElement.Drips)
                    entry.sideRim = new[] { Ring("Caster signature", 1.2f, -.12f, .06f, ColourRole.Rim) };
            }
        }
    }
}

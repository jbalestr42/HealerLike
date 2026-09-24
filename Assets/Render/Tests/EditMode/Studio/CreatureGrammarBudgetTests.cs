using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio
{

public class CreatureGrammarBudgetTests
{
    readonly List<Object> _objects = new List<Object>();
    CreatureGrammarPreset _preset;

    [SetUp]
    public void SetUp()
    {
        _preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        _preset.vocabulary = RenderTestAssets.LoadLookVocabulary();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            if (trackedObject != null)
            {
                Object.DestroyImmediate(trackedObject);
            }
        }
        _objects.Clear();
    }

    T Track<T>(T instance) where T : Object
    {
        _objects.Add(instance);
        return instance;
    }

    static LookPart Sphere(string id, PartRole role, ColourRole colour, Vector3 size, Vector3 position)
    {
        return new LookPart { id = id, primitive = Primitive.Sphere, role = role, colour = colour, size = size,
            position = position };
    }

    // One body, one head fanned five times and a small torus bead, in four parts at most
    LookVocabulary CreateTightVocabulary()
    {
        LookVocabulary custom = Track(ScriptableObject.CreateInstance<LookVocabulary>());
        custom.palette = _preset.vocabulary.palette;
        custom.bodyUnit = 1f;
        custom.plantScale = 1f;
        custom.maxParts = 4;
        LookPart body = Sphere("Body", PartRole.Body, ColourRole.Body, Vector3.one, Vector3.zero);
        LookPart crown = Sphere("Head", PartRole.Head, ColourRole.Accent, Vector3.one, Vector3.zero);
        // Inside the outermost of five fanned heads, clearly outside the single head the budget leaves
        Vector3 beadAt = Quaternion.Euler(0f, 0f, -56f) * Vector3.up * 1.2f;
        LookPart bead = Sphere("Bead", PartRole.Accessory, ColourRole.Accent, Vector3.one * 0.05f, beadAt);
        custom.bodies[MassBand.Light] = new LookVocabulary.BodyEntry { plant = new LookPart[] { body } };
        custom.heads[HeadKind.Bud] = new LookVocabulary.HeadEntry { plant = new LookPart[] { crown },
            carriesCount = false };
        custom.stems[StemBand.Steady] = new LookVocabulary.StemEntry { length = 1f, thickness = 0.1f,
            limbLength = 0.4f };
        custom.accessories[AccessoryKind.SmallTorus] = new LookVocabulary.AccessoryEntry
        {
            socket = AccessorySocket.NeckOrbit,
            plant = new LookPart[] { bead }
        };
        return custom;
    }

    [Test]
    public void Check_AccessoryInsideFannedHeads_MeasuresAfterTheBudgetDropsCopies()
    {
        LookVocabulary custom = CreateTightVocabulary();
        _preset.vocabulary = custom;
        _preset.count = CountBand.Many;
        _preset.accessory = AccessoryKind.SmallTorus;
        List<string> errors = new List<string>();

        CreatureGrammarBudget.Check(custom, _preset.Channels(), errors);

        Assert.Less(LookMeasure.AccessoryReach(_preset.Channels(), custom), LookComposer.PlantAccessoryReach);
        Assert.IsEmpty(errors, string.Join("; ", errors));
        CreatureRecipe actual = Track(_preset.Compose());
        CreatureRecipe expected = Track(LookComposer.Compose(_preset.Channels(), custom));
        Assert.AreEqual(4, actual.parts.Length);
        CollectionAssert.AreEqual(expected.parts, actual.parts);
    }

    [Test]
    public void Check_AccessoryInsideTheBody_ReportsItDoesNotReachOut()
    {
        LookVocabulary custom = CreateTightVocabulary();
        custom.accessories[AccessoryKind.SmallTorus].plant[0].position = Vector3.zero;
        _preset.vocabulary = custom;
        _preset.accessory = AccessoryKind.SmallTorus;
        List<string> errors = new List<string>();

        CreatureGrammarBudget.Check(custom, _preset.Channels(), errors);

        StringAssert.Contains("does not extend far enough", string.Join(" ", errors));
    }

    [Test]
    public void Notes_ShippedVocabulary_ExplainTheBudgetAndThePinnedReach()
    {
        string text = string.Join(" ", CreatureGrammarBudget.Notes(_preset));

        StringAssert.Contains("maxParts", text);
        if (_preset.vocabulary.isReachPinned)
        {
            StringAssert.Contains("pinned", text);
        }
    }

    [Test]
    public void Notes_StoneDerivedFromEntity_ExplainBoth()
    {
        _preset.side = LookSide.Stone;
        _preset.deriveFromEntity = true; // without an entity the manual stone channels stand

        string text = string.Join(" ", CreatureGrammarBudget.Notes(_preset));

        StringAssert.Contains("entity's actual skill", text);
        StringAssert.Contains("Stone recipes stand on limbs", text);
    }
}

}

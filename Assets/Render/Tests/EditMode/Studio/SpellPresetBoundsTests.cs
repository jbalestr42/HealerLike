using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio
{

public class SpellPresetBoundsTests
{
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    [Test]
    public void SanitizedEntry_MalformedPart_BoundsPositionSizeAndCycle()
    {
        ElementEntry source = StudioTestAssets.CreateRise();
        source.parts[0].position = new Vector3(float.NaN, float.PositiveInfinity, 1000f);
        source.parts[0].size = new Vector3(-1f, float.NaN, 1000f);
        source.parts[0].primitive = (Primitive)999;
        source.cycleSeconds = float.NaN;

        ElementEntry result = SpellPresetBounds.SanitizedEntry(source);

        Assert.AreEqual(new Vector3(0f, 0f, 50f), result.parts[0].position);
        Assert.AreEqual(new Vector3(0.001f, 0.1f, 20f), result.parts[0].size);
        Assert.AreEqual(default(Primitive), result.parts[0].primitive);
        Assert.AreEqual(0.6f, result.cycleSeconds);
        Assert.IsTrue(float.IsNaN(source.parts[0].position.x)); // the authored entry keeps its values
    }

    [Test]
    public void SanitizedEntry_UnknownEnumsAndNullLayer_FallBackAndEmpty()
    {
        ElementEntry source = StudioTestAssets.CreateRise();
        source.motion = (EffectMotionKind)999;
        source.minCount = int.MaxValue;
        source.stackBeads = null;

        ElementEntry result = SpellPresetBounds.SanitizedEntry(source);

        Assert.AreEqual(EffectMotionKind.Burst, result.motion);
        Assert.AreEqual(4, result.minCount); // no more than the four shapes
        Assert.IsEmpty(result.stackBeads);
        Assert.IsNull(source.stackBeads);
    }

    [Test]
    public void SanitizedEntry_Twice_ReturnsArraysOfItsOwn()
    {
        ElementEntry source = StudioTestAssets.CreateRise();

        ElementEntry first = SpellPresetBounds.SanitizedEntry(source);
        ElementEntry second = SpellPresetBounds.SanitizedEntry(source);
        first.parts[0].position = Vector3.up;
        first.stackBeads[0].id = "Changed";

        Assert.AreEqual(Vector3.zero, source.parts[0].position);
        Assert.AreEqual(Vector3.zero, second.parts[0].position);
        Assert.AreEqual("Bead", source.stackBeads[0].id);
    }

    [Test]
    public void SafeParts_MoreThanMaxParts_KeepsMaxParts()
    {
        LookPart[] parts = new LookPart[SpellStudioPreset.MaxParts + 20];

        LookPart[] result = SpellPresetBounds.SafeParts(parts);

        Assert.AreEqual(SpellStudioPreset.MaxParts, result.Length);
        Assert.AreEqual("Part 1", result[0].id); // a part without a name is numbered
    }

    [Test]
    public void SafeColour_NotFiniteAndOutOfRange_ClampsEachChannel()
    {
        Color colour = new Color(float.NaN, float.PositiveInfinity, -2f, 10f);

        Color result = SpellPresetBounds.SafeColour(colour);

        Assert.AreEqual(new Color(1f, 1f, 0f, 1f), result);
        Assert.IsFalse(SpellPresetBounds.IsValidColour(colour));
    }

    [Test]
    public void Defined_UnknownValue_ReturnsTheFallback()
    {
        EffectFamily unknown = (EffectFamily)999;

        Assert.AreEqual(EffectFamily.Damage, SpellPresetBounds.Defined(unknown, EffectFamily.Damage));
        Assert.AreEqual(EffectFamily.Heal, SpellPresetBounds.Defined(EffectFamily.Heal, EffectFamily.Damage));
    }

    [Test]
    public void CloneEntry_MissingArrays_ReturnsEmptyArrays()
    {
        Assert.IsNull(SpellPresetBounds.CloneEntry(null));

        ElementEntry cloned = SpellPresetBounds.CloneEntry(new ElementEntry { parts = null });

        Assert.IsEmpty(cloned.parts);
    }

    [Test]
    public void Compose_MalformedAuthoring_BoundsTheRecipeAndKeepsThePreset()
    {
        SpellStudioPreset preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
        _objects.Add(preset);
        preset.overrideEntry = true;
        preset.entry = StudioTestAssets.CreateRise();
        preset.entry.count = EffectCount.Stacks;
        preset.stacks = int.MaxValue;
        preset.scale = float.NaN;
        preset.side = (Entity.EntityType)999;
        preset.family = (EffectFamily)999;
        preset.overrideColour = true;

        EffectRecipe result = preset.Compose();

        Assert.AreEqual(4, result.count); // every shape, however many stacks
        Assert.AreEqual(EffectFamily.Damage, result.family);
        Assert.AreEqual(1f, preset.safeScale);
        Assert.AreEqual(Entity.EntityType.Player, preset.safeSide);
        Assert.AreEqual(int.MaxValue, preset.stacks);
    }
}

}

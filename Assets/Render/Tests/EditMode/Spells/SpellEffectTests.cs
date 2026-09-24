using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class SpellEffectTests
{
    readonly List<GameObject> _objects = new List<GameObject>();

    static EffectVocabulary LoadVocabulary()
    {
        return AssetDatabase.LoadAssetAtPath<EffectVocabulary>("Assets/Render/Spells/Data/EffectVocabulary.asset");
    }

    SpellEffect CreateEffect(EffectElement element, EffectFamily family, EffectTempo tempo, float period = 0f,
                             int stacks = 1)
    {
        EffectRecipe recipe = EffectComposer.Compose(LoadVocabulary(), element, family, tempo, period, stacks, 0f, 0f);
        GameObject go = new GameObject(element.ToString());
        _objects.Add(go);
        SpellEffect effect = go.AddComponent<SpellEffect>();
        effect.Init(recipe, AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(SpellSinkFixture.MeshesPath), null);
        return effect;
    }

    static float LargestShape(SpellEffect effect)
    {
        float largest = 0f;
        foreach (Transform shape in effect.shapes)
        {
            if (shape.gameObject.activeSelf)
            {
                largest = Mathf.Max(largest, shape.localScale.magnitude);
            }
        }
        return largest;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            Object.DestroyImmediate(go);
        }
        _objects.Clear();
    }

    [Test]
    public void Init_Recipe_BuildsOneChildPerVocabularyPart()
    {
        ElementEntry entry = LoadVocabulary().GetEntry(EffectElement.Stalks);

        SpellEffect effect = CreateEffect(EffectElement.Stalks, EffectFamily.Renew, EffectTempo.PerPeriod, 1f);

        int expected = entry.parts.Length + entry.stackBeads.Length + entry.criticalRings.Length + entry.sideRim.Length;
        Assert.AreEqual(expected, effect.transform.childCount);
        Assert.AreEqual(effect.shapes.Count, effect.stalks.Count);
    }

    [Test]
    public void Advance_OnceImpactPastItsCycle_ShowsNothing()
    {
        SpellEffect effect = CreateEffect(EffectElement.Burst, EffectFamily.Damage, EffectTempo.Once);

        effect.Advance(effect.lifetime * 0.5f);
        float during = LargestShape(effect);
        effect.Advance(effect.lifetime);

        Assert.Greater(during, 0f);
        Assert.AreEqual(0f, LargestShape(effect), 0.0001f);
    }

    [Test]
    public void SetStatus_PerPeriodBeforeTheFirstTick_ShowsNothingThenDrips()
    {
        SpellEffect effect = CreateEffect(EffectElement.Drips, EffectFamily.Rot, EffectTempo.PerPeriod, 2f);

        effect.SetStatus(1, 1f, 6f, ClockKind.Simulation);
        float before = LargestShape(effect);
        effect.SetStatus(1, 2.5f, 6f, ClockKind.Simulation);

        Assert.AreEqual(0f, before, 0.0001f);
        Assert.Greater(LargestShape(effect), 0f);
    }

    [Test]
    public void SetStatus_RenewPastItsFirstPeriod_StalksReachTheirSpheres()
    {
        SpellEffect effect = CreateEffect(EffectElement.Stalks, EffectFamily.Renew, EffectTempo.PerPeriod, 1f);

        effect.SetStatus(1, 1.5f, 6f, ClockKind.Simulation);

        Transform sphere = effect.shapes[0];
        Transform stalk = effect.stalks[0];
        Assert.Greater(sphere.localPosition.y, 0f);
        Assert.AreEqual(sphere.localPosition.y, stalk.localScale.y, 0.0001f);
    }

    [Test]
    public void SetStatus_RenewLateInItsPeriod_StalksStillStand()
    {
        SpellEffect effect = CreateEffect(EffectElement.Stalks, EffectFamily.Renew, EffectTempo.PerPeriod, 1f);

        effect.SetStatus(1, 1.95f, 6f, ClockKind.Simulation);

        Assert.Greater(LargestShape(effect), 0.1f);
    }

    [Test]
    public void Advance_RenewPastItsDuration_KeepsGrowing()
    {
        SpellEffect effect = CreateEffect(EffectElement.Stalks, EffectFamily.Renew, EffectTempo.PerPeriod, 1f);
        effect.SetStatus(1, 2.5f, 6f, ClockKind.Simulation);

        effect.Advance(4f);

        Assert.Greater(LargestShape(effect), 0.1f);
    }

    [Test]
    public void SetStatus_RenewJustAfterALaterTick_StalksStillStand()
    {
        SpellEffect effect = CreateEffect(EffectElement.Stalks, EffectFamily.Renew, EffectTempo.PerPeriod, 1f);

        effect.SetStatus(1, 3.02f, 6f, ClockKind.Simulation);

        Assert.Greater(LargestShape(effect), 0.1f);
        Assert.Greater(effect.shapes[0].localPosition.y, 1f);
    }

    [Test]
    public void Advance_ForDurationFall_LoopsUntilRemoval()
    {
        SpellEffect effect = CreateEffect(EffectElement.Drips, EffectFamily.Rot, EffectTempo.ForDuration);
        effect.SetStatus(1, 0f, float.PositiveInfinity, ClockKind.Simulation);

        effect.Advance(effect.lifetime * 3.25f);

        Assert.Greater(LargestShape(effect), 0f);
    }

    [Test]
    public void SetStatus_Stacks_ShowOneBeadEach()
    {
        SpellEffect effect = CreateEffect(EffectElement.Orbit, EffectFamily.Boon, EffectTempo.ForDuration);

        effect.SetStatus(3, 0f, 4f, ClockKind.Simulation);

        int beads = 0;
        foreach (Transform child in effect.transform)
        {
            if (child.name == "StackBead" && child.gameObject.activeSelf)
            {
                beads++;
            }
        }
        Assert.AreEqual(3, beads);
    }

    [Test]
    public void SetCount_FewerCharges_HidesThePlatesThatFell()
    {
        SpellEffect effect = CreateEffect(EffectElement.Plates, EffectFamily.Boon, EffectTempo.ForDuration);

        effect.SetCount(2);

        int shown = 0;
        foreach (Transform plate in effect.shapes)
        {
            if (plate.gameObject.activeSelf)
            {
                shown++;
            }
        }
        Assert.AreEqual(2, shown);
    }

    [Test]
    public void Advance_Orbit_TurnsTheTiltedPlane()
    {
        SpellEffect effect = CreateEffect(EffectElement.Orbit, EffectFamily.Boon, EffectTempo.ForDuration);
        effect.SetStatus(1, 0f, 10f, ClockKind.Simulation);
        Quaternion start = effect.shapes[0].localRotation;

        effect.Advance(1f);

        Assert.Greater(Quaternion.Angle(start, effect.shapes[0].localRotation), 1f);
    }

    [Test]
    public void BeginRemoval_QuarterSecond_CompletesAndHides()
    {
        SpellEffect effect = CreateEffect(EffectElement.Orbit, EffectFamily.Boon, EffectTempo.ForDuration);
        effect.SetStatus(1, 0f, 10f, ClockKind.Simulation);

        effect.BeginRemoval();
        effect.Advance(0.25f);

        Assert.IsTrue(effect.removalComplete);
        Assert.AreEqual(0f, LargestShape(effect), 0.0001f);
    }

    [Test]
    public void SetSide_Computer_ShowsTheRimInTheLitBaneColour()
    {
        SpellEffect effect = CreateEffect(EffectElement.Orbit, EffectFamily.Boon, EffectTempo.ForDuration);

        effect.SetSide(Entity.EntityType.Computer);

        Transform rim = null;
        foreach (Transform child in effect.transform)
        {
            if (child.name == "SideRim")
            {
                rim = child;
            }
        }
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        rim.GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.IsTrue(rim.gameObject.activeSelf);
        Assert.Less(Vector4.Distance(LoadVocabulary().palette.baneLit, block.GetColor("_BaseColor")), 0.0001f);
    }

    [Test]
    public void SetEndpoints_Beam_StartsAtTheFirstPoint()
    {
        SpellEffect effect = CreateEffect(EffectElement.Beam, EffectFamily.Heal, EffectTempo.Once);

        effect.SetEndpoints(Vector3.left, Vector3.right, false);

        Transform first = effect.stalks[0];
        Vector3 start = first.position - first.up * first.localScale.y * 0.5f;
        Assert.Less(Vector3.Distance(Vector3.left, start), 0.0001f);
    }

    [Test]
    public void Init_NoRecipe_LogsAndBuildsNothing()
    {
        GameObject go = new GameObject("Empty");
        _objects.Add(go);
        SpellEffect effect = go.AddComponent<SpellEffect>();

        string message = "[SpellEffect] Init needs a recipe and the primitive meshes.";
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, message);
        effect.Init(null, null, null);

        Assert.AreEqual(0, go.transform.childCount);
    }
}

}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{

public class SpellInspectorPaneTests
{
    readonly List<Object> _objects = new List<Object>();
    SpellStudioPreset _preset;
    SpellLooks _looks;
    GameObject _projectile;

    [SetUp]
    public void SetUp()
    {
        _preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
        _looks = ScriptableObject.CreateInstance<SpellLooks>();
        _projectile = new GameObject("Studio projectile");
        _objects.Add(_preset);
        _objects.Add(_looks);
        _objects.Add(_projectile);
        _preset.sourceProjectile = _projectile;
        _preset.spellLooks = _looks;
    }

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
    public void ProjectileReadout_EmptyNativeRow_AsksForARepairAndLeavesTheRow()
    {
        _looks.projectiles[_projectile] = null;

        string readout = SpellInspectorPane.ProjectileReadout(_preset);

        StringAssert.Contains("empty", readout);
        Assert.IsNull(_looks.projectiles[_projectile]); // the inspector never repairs native data on its own
    }

    [Test]
    public void ProjectileReadout_NativeRow_NamesItsDelivery()
    {
        _looks.projectiles[_projectile] = new ProjectileLook { style = DeliveryStyle.Arc, preserveContactPath = true };

        string readout = SpellInspectorPane.ProjectileReadout(_preset);

        StringAssert.StartsWith("Delivery: Arc · preserves contact path", readout);
    }

    [Test]
    public void ProjectileReadout_NativeRowsOff_ReadsTheDerivedDelivery()
    {
        _preset.useGameplayOverrides = false;

        string readout = SpellInspectorPane.ProjectileReadout(_preset);

        StringAssert.StartsWith("Delivery: Direct", readout); // a bare GameObject flies straight
    }
}

}

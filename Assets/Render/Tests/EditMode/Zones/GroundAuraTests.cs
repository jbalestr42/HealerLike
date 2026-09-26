using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class GroundAuraTests
{
    [Test]
    public void Radius_Ash_ShrinksAsTheEnemyWeakens()
    {
        Assert.AreEqual(GroundAura.AshMinRadius + GroundAura.AshRadiusRange, GroundAura.Radius(ZoneKind.Ash, 1f), 1e-6f);
        Assert.Less(GroundAura.Radius(ZoneKind.Ash, 0.4f), GroundAura.Radius(ZoneKind.Ash, 0.8f));
        Assert.AreEqual(GroundAura.AshMinRadius, GroundAura.Radius(ZoneKind.Ash, -3f), 1e-6f);
    }

    [Test]
    public void Radius_Wilt_SpreadsAsTheAllyWeakens()
    {
        Assert.AreEqual(GroundAura.WiltMinRadius, GroundAura.Radius(ZoneKind.Wilt, 1f), 1e-6f);
        Assert.Greater(GroundAura.Radius(ZoneKind.Wilt, 0.2f), GroundAura.Radius(ZoneKind.Wilt, 0.7f));
        Assert.AreEqual(0f, GroundAura.Radius(ZoneKind.Heal, 0.5f));
    }

    [Test]
    public void Strength_AshBurnsWhileStandingAndWiltDeepensWithMissingHealth()
    {
        Assert.AreEqual(1f, GroundAura.Strength(ZoneKind.Ash, 0.1f));
        Assert.AreEqual(0f, GroundAura.Strength(ZoneKind.Ash, 0f), "A dead enemy lets the grass regrow.");
        Assert.AreEqual(0f, GroundAura.Strength(ZoneKind.Wilt, 1f), "A healthy ally's grass lives.");
        Assert.AreEqual(0.75f, GroundAura.Strength(ZoneKind.Wilt, 0.25f), 1e-6f);
    }

    [Test]
    public void HealthShare_InvalidOrOutOfRange_Clamps()
    {
        Assert.AreEqual(0.5f, GroundAura.HealthShare(50f, 100f), 1e-6f);
        Assert.AreEqual(1f, GroundAura.HealthShare(150f, 100f));
        Assert.AreEqual(0f, GroundAura.HealthShare(10f, 0f));
        Assert.AreEqual(0f, GroundAura.HealthShare(float.NaN, 100f));
    }

    [Test]
    public void Refresh_WithoutAnEntity_AddsNoZone()
    {
        GameObject go = new GameObject("aura");
        try
        {
            ZoneRegistry registry = go.AddComponent<ZoneRegistry>();
            registry.Init(new ZoneFakeUpload());
            GroundAura aura = go.AddComponent<GroundAura>();

            aura.Init(null, registry, 1f);
            aura.Refresh();

            Assert.AreEqual(ZoneKind.None, aura.kind);
            Assert.AreEqual(0, registry.liveCount);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

}

using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class GroundAuraTests
{
    [Test]
    public void Radius_Ash_ShrinksAsTheEnemyWeakens()
    {
        Assert.AreEqual(GroundAura.AshMinRadius + GroundAura.AshRadiusRange,
            GroundAura.Radius(GroundAura.Mark.Ash, 1f), 1e-6f);
        Assert.Less(GroundAura.Radius(GroundAura.Mark.Ash, 0.4f), GroundAura.Radius(GroundAura.Mark.Ash, 0.8f));
        Assert.AreEqual(GroundAura.AshMinRadius, GroundAura.Radius(GroundAura.Mark.Ash, -3f), 1e-6f);
    }

    [Test]
    public void Radius_Wilt_SpreadsAsTheAllyWeakens()
    {
        Assert.AreEqual(GroundAura.WiltMinRadius, GroundAura.Radius(GroundAura.Mark.Wilt, 1f), 1e-6f);
        Assert.Greater(GroundAura.Radius(GroundAura.Mark.Wilt, 0.2f), GroundAura.Radius(GroundAura.Mark.Wilt, 0.7f));
        Assert.AreEqual(0f, GroundAura.Radius(GroundAura.Mark.None, 0.5f));
    }

    [Test]
    public void Strength_AshBurnsWhileStandingAndWiltDeepensWithMissingHealth()
    {
        Assert.AreEqual(1f, GroundAura.Strength(GroundAura.Mark.Ash, 0.1f));
        Assert.AreEqual(0f, GroundAura.Strength(GroundAura.Mark.Ash, 0f), "A dead enemy lets the grass regrow.");
        Assert.AreEqual(0f, GroundAura.Strength(GroundAura.Mark.Wilt, 1f), "A healthy ally's grass lives.");
        Assert.AreEqual(0.75f, GroundAura.Strength(GroundAura.Mark.Wilt, 0.25f), 1e-6f);
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
    public void IsHeld_ColliderOnTheIgnoreRaycastLayer_IsHeld()
    {
        GameObject go = new GameObject("held");
        try
        {
            Collider collider = go.AddComponent<BoxCollider>();
            Assert.IsFalse(EntityHold.IsHeld(collider));
            go.layer = Layers.IgnoreRaycast;
            Assert.IsTrue(EntityHold.IsHeld(collider));
            Assert.IsFalse(EntityHold.IsHeld(null));
            Assert.AreSame(collider, EntityHold.Find(go.transform));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Refresh_WithoutAnEntity_MarksNothing()
    {
        GameObject go = new GameObject("aura");
        try
        {
            using Ground ground = new Ground();
            GroundAura aura = go.AddComponent<GroundAura>();

            aura.Init(null, ground, 1f);
            aura.Refresh();

            Assert.AreEqual(GroundAura.Mark.None, aura.mark);
            Assert.AreEqual(0, ground.heldCount);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Refresh_HurtAlly_KillsTheGrassRoundItAsDeepAsItsMissingHealth()
    {
        GameObject go = new GameObject("ally");
        GameObject healthGo = new GameObject("health");
        try
        {
            using Ground ground = new Ground();
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
            entity.entityType = Entity.EntityType.Player;
            GroundAura aura = go.AddComponent<GroundAura>();
            aura.Init(entity, ground, 1f);
            ResourceAttribute health = TestHelpers.CreateResourceAttribute(healthGo, AttributeType.HealthMax, 100f);
            TestHelpers.SetPrivateField(aura, "_health", health);
            TestHelpers.SetPrivateField(health, "_value", 60f);

            aura.Refresh();

            Assert.AreEqual(GroundAura.Mark.Wilt, aura.mark);
            Assert.IsTrue(aura.isShown);
            Assert.AreEqual(-0.4f, GroundProbe.State(ground, go.transform.position).y, 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(healthGo);
        }
    }
}

}

using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class GroundWarningTests
{
    [Test]
    public void Strength_Cooldown_RisesOverItsLastShare()
    {
        Assert.AreEqual(0f, GroundWarning.Strength(1f));
        Assert.AreEqual(0f, GroundWarning.Strength(GroundWarning.WarningShare));
        Assert.AreEqual(0.5f, GroundWarning.Strength(GroundWarning.WarningShare * 0.5f), 1e-5f);
        Assert.Greater(GroundWarning.Strength(0.01f), 0.95f);
    }

    [Test]
    public void Strength_WaitingAtZeroOrInvalid_ShowsNothing()
    {
        Assert.AreEqual(0f, GroundWarning.Strength(0f), "A skill waiting for a target may wait for ever.");
        Assert.AreEqual(0f, GroundWarning.Strength(-1f));
        Assert.AreEqual(0f, GroundWarning.Strength(float.NaN));
    }

    [Test]
    public void Refresh_SkillNotRunning_WarnsNothing()
    {
        GameObject go = new GameObject("enemy");
        try
        {
            Ground ground = new Ground();
            Entity entity = null;
            TestHelpers.WithLoggingDisabled(() => entity = go.AddComponent<Entity>());
            entity.entityType = Entity.EntityType.Computer;
            AreaOfEffectSkill skill = null;
            TestHelpers.WithLoggingDisabled(() => skill = go.AddComponent<AreaOfEffectSkill>());
            skill.isEnabled = false;
            GroundWarning warning = go.AddComponent<GroundWarning>();

            warning.Init(entity, ground);
            warning.Refresh();

            Assert.IsTrue(warning.hasSkill);
            Assert.IsFalse(warning.isShown, "Out of combat its cooldown stands still; nothing is coming.");
            Assert.AreEqual(1, ground.heldCount);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Refresh_NoEntity_WarnsNothing()
    {
        GameObject go = new GameObject("warning");
        try
        {
            Ground ground = new Ground();
            GroundWarning warning = go.AddComponent<GroundWarning>();

            warning.Init(null, ground);
            warning.Refresh();

            Assert.IsFalse(warning.hasSkill);
            Assert.AreEqual(0, ground.heldCount, "No skill, nothing held.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

}

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
    public void Refresh_NoEntity_WarnsNothing()
    {
        GameObject go = new GameObject("warning");
        try
        {
            ZoneRegistry zones = go.AddComponent<ZoneRegistry>();
            zones.Init(new ZoneFakeUpload());
            GroundWarning warning = go.AddComponent<GroundWarning>();

            warning.Init(null, zones);
            warning.Refresh();

            Assert.IsFalse(warning.hasSkill);
            Assert.AreEqual(0, zones.liveCount);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The heal an entity receives is reported to it, with the entity that sent it
public class EntityHealReceivedTests
{
    readonly TestUnits _units = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void NotifyHealed_Heal_IsReportedToTheHealedEntityWithItsSource()
    {
        Entity healer = _units.Create(100f, 100f, "Healer");
        Entity healed = _units.Create(50f, 100f, "Healed");
        List<(GameObject, float)> reports = new List<(GameObject, float)>();
        healed.OnHealReceived.AddListener((source, amount) => reports.Add((source, amount)));

        Entity.NotifyHealed(healer.gameObject, healed.gameObject, 12f);

        CollectionAssert.AreEqual(new[] { (healer.gameObject, 12f) }, reports);
    }

    [Test]
    public void NotifyHealed_Damage_IsNotReported()
    {
        Entity target = _units.Create(100f, 100f, "Target");
        bool isReported = false;
        target.OnHealReceived.AddListener((source, amount) => isReported = true);

        Entity.NotifyHealed(null, target.gameObject, -12f);
        Entity.NotifyHealed(null, target.gameObject, 0f);

        Assert.IsFalse(isReported);
    }

    [Test]
    public void NotifyHealed_WithoutSource_IsStillReported()
    {
        Entity healed = _units.Create(50f, 100f, "Healed");
        float received = 0f;
        healed.OnHealReceived.AddListener((source, amount) => received += amount);

        Entity.NotifyHealed(null, healed.gameObject, 5f);

        Assert.AreEqual(5f, received, 0.0001f);
    }

    [Test]
    public void NotifyHealed_ANonEntityOrNoTarget_DoesNothing()
    {
        GameObject character = new GameObject("Character");
        try
        {
            Assert.DoesNotThrow(() => Entity.NotifyHealed(null, character, 5f));
            Assert.DoesNotThrow(() => Entity.NotifyHealed(null, null, 5f));
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }
}

}

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
        List<(GameObject, ConsumerResult)> reports = new List<(GameObject, ConsumerResult)>();
        healed.OnHealReceived.AddListener((source, heal) => reports.Add((source, heal)));

        Entity.NotifyHealed(healer.gameObject, healed.gameObject, new ConsumerResult(12f, true, 2f));

        Assert.AreEqual(1, reports.Count);
        Assert.AreSame(healer.gameObject, reports[0].Item1);
        Assert.AreEqual(12f, reports[0].Item2.value, 0.0001f);
        Assert.IsTrue(reports[0].Item2.isCritical);
        Assert.AreEqual(2f, reports[0].Item2.overflow, 0.0001f);
    }

    [Test]
    public void NotifyHealed_Damage_IsNotReported()
    {
        Entity target = _units.Create(100f, 100f, "Target");
        bool isReported = false;
        target.OnHealReceived.AddListener((source, heal) => isReported = true);

        Entity.NotifyHealed(null, target.gameObject, new ConsumerResult(-12f, false));
        Entity.NotifyHealed(null, target.gameObject, new ConsumerResult(0f, false));

        Assert.IsFalse(isReported);
    }

    [Test]
    public void NotifyHealed_WithoutSource_IsStillReported()
    {
        Entity healed = _units.Create(50f, 100f, "Healed");
        float received = 0f;
        healed.OnHealReceived.AddListener((source, heal) => received += heal.value);

        Entity.NotifyHealed(null, healed.gameObject, new ConsumerResult(5f, false));

        Assert.AreEqual(5f, received, 0.0001f);
    }

    [Test]
    public void NotifyHealed_ANonEntityOrNoTarget_DoesNothing()
    {
        GameObject character = new GameObject("Character");
        try
        {
            Assert.DoesNotThrow(() => Entity.NotifyHealed(null, character, new ConsumerResult(5f, false)));
            Assert.DoesNotThrow(() => Entity.NotifyHealed(null, null, new ConsumerResult(5f, false)));
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }
}

}

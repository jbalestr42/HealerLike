using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The damage an entity takes is reported to the entity that dealt it
public class EntityDamageDealtTests
{
    readonly TestUnits _units = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    [Test]
    public void NotifyAttacker_Damage_IsReportedToTheAttackerAsAPositiveAmount()
    {
        Entity attacker = _units.Create(100f, 100f, "Attacker");
        Entity target = _units.Create(100f, 100f, "Target");
        List<(GameObject, float)> reports = new List<(GameObject, float)>();
        attacker.OnDamageDealt.AddListener((hit, damage) => reports.Add((hit, damage)));

        Entity.NotifyAttacker(attacker.gameObject, target.gameObject, -12f);

        CollectionAssert.AreEqual(new[] { (target.gameObject, 12f) }, reports);
    }

    [Test]
    public void NotifyAttacker_Heal_IsNotReported()
    {
        Entity healer = _units.Create(100f, 100f, "Healer");
        bool isReported = false;
        healer.OnDamageDealt.AddListener((hit, damage) => isReported = true);

        Entity.NotifyAttacker(healer.gameObject, healer.gameObject, 12f);

        Assert.IsFalse(isReported);
    }

    [Test]
    public void NotifyAttacker_FromANonEntityOrNoSource_DoesNothing()
    {
        GameObject character = new GameObject("Character");
        try
        {
            Assert.DoesNotThrow(() => Entity.NotifyAttacker(character, null, -5f));
            Assert.DoesNotThrow(() => Entity.NotifyAttacker(null, null, -5f));
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }
}

}

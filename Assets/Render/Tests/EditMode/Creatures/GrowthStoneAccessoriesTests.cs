using System.Linq;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class GrowthStoneAccessoriesTests : GrowthStoneFixture
{
    [Test]
    public void ReferenceCollarsCrownsAndPairedSeeds_AreCenteredWhileSideShootsRemainAsymmetric()
    {
        AccessoryKind[] centered =
        {
            AccessoryKind.TierRings,
            AccessoryKind.SmallTorus,
            AccessoryKind.ThornCollar,
            AccessoryKind.ConeCrown,
            AccessoryKind.TwinSeeds,
            AccessoryKind.ShardBarbs,
        };
        foreach (AccessoryKind accessory in Enum.GetValues(typeof(AccessoryKind)))
        {
            if (accessory == AccessoryKind.None)
            {
                continue;
            }

            LookVocabulary.AccessoryEntry entry = _vocabulary.accessories[accessory];
            Assert.AreEqual(centered.Contains(accessory), entry.isCentered, accessory.ToString());
            if (entry.isCentered)
            {
                Assert.AreEqual(0f, entry.plant.Sum(p => p.position.x), 0.001f, accessory.ToString());
                Assert.AreEqual(0f, entry.stone.Sum(p => p.position.x), 0.001f, accessory.ToString());
            }
        }

        Assert.AreEqual(3, _vocabulary.accessories[AccessoryKind.TierRings].plant.Length);
        Assert.AreEqual(2, _vocabulary.accessories[AccessoryKind.TwinSeeds].plant.Count(p => p.id == "TwinSeed"));
    }

    [Test]
    public void MineralTiersStayExposedAndPlantCollarUsesNeedles()
    {
        LookPart[] rings = _vocabulary.accessories[AccessoryKind.TierRings].stone;
        Assert.AreEqual(3, rings.Length);
        for (int i = 0; i < rings.Length; i++)
        {
            Assert.Greater(
                rings[i].size.x * _vocabulary.bodies[MassBand.Light].effectiveHeadScale,
                _vocabulary.bodies[MassBand.Light].stone[0].size.x
            );
            if (i > 0)
            {
                Assert.Greater(
                    rings[i - 1].position.y - rings[i].position.y,
                    (rings[i - 1].size.y + rings[i].size.y) * 0.5f
                );
            }
        }

        LookPart[] collar = _vocabulary.accessories[AccessoryKind.ThornCollar].plant;
        Assert.IsTrue(collar.Any(p => p.shape.kind == ShapeKind.Ring), "Needles attach to a collar.");
        LookPart[] thorns = collar.Where(p => p.id == "Thorn").ToArray();
        Assert.AreEqual(6, thorns.Length);
        foreach (LookPart thorn in thorns)
        {
            Assert.AreEqual(ShapeKind.Segment, thorn.shape.kind);
            Assert.Greater(thorn.shape.taper, 0.85f);
            Assert.Less(thorn.size.x / thorn.size.y, 0.18f);
        }
    }
}
}

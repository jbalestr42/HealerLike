using System.Collections.Generic;
using HealerLike.Render.Grass;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class TrampleFootprintTests : TrampleZoneFixture
    {
        [Test]
        public void CreatureFootprint_ScaledRoot_IsHalfTheLargerHorizontalScale()
        {
            _obstacle.transform.localScale = new Vector3(0.9f, 3f, -1.2f);

            float footprint = TrampleZone.CreatureFootprint(_obstacle.transform);

            Assert.AreEqual(0.6f, footprint, 0.0001f); // 1 cell * 0.5 * |-1.2|
            Assert.AreEqual(0.75f, TrampleZone.TrampleRadius(footprint), 0.0001f); // + 0.15 margin
            Assert.AreEqual(TrampleZone.Margin, TrampleZone.TrampleRadius(-1f), 0.0001f);
        }

        [Test]
        public void Refresh_RecomposedRootReach_ResizesOneFootprintAndSteadyRefreshAllocatesNothing()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.roots.footRadius = 0.8f;
            _recipe.roots.thickness = 0.08f;
            _recipe.idle = default;
            BuildRig();
            Assert.That(_zone.radius, Is.EqualTo(0.8f + 0.08f + TrampleZone.Margin).Within(0.0001f));
            _zone.Refresh();
            _recipe.roots.footRadius = 1.8f;
            Assert.IsTrue(_host.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
                RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
            _host.rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
            _zone.Refresh();
            Assert.That(_zone.radius, Is.EqualTo(1.8f + 0.08f + TrampleZone.Margin).Within(0.0001f));
            Assert.IsFalse(Disc(out _), "A creature presses as a body, not as a disc.");
            Assert.AreEqual(1, _ground.bodyCount);
            for (int i = 0; i < 100; i++) _zone.Refresh();
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) _zone.Refresh();
            Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Test]
        public void CreatureFootprint_StoneBasalMassAndFeet_ExcludeElevatedHeadAndArms()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.roots.count = 0;
            _recipe.idle = default;
            _recipe.parts = new[]
            {
                new CreaturePart { id = "Body", parent = -1, shape = ShapeProfile.Block(),
                    dimensions = Vector3.one * 0.4f, colour = Color.white, role = PartRole.Body },
                new CreaturePart { id = "ExtraBase", parent = 0, shape = ShapeProfile.Block(),
                    localPosition = new Vector3(0.8f, 0f, 0f), dimensions = new Vector3(0.6f, 0.3f, 0.4f),
                    colour = Color.white, role = PartRole.Body },
                new CreaturePart { id = "Foot", parent = 0, shape = ShapeProfile.Block(),
                    localPosition = new Vector3(-1.2f, 0f, 0f), dimensions = Vector3.one * 0.4f,
                    colour = Color.white, role = PartRole.Limb },
                new CreaturePart { id = "WideHead", parent = 0, shape = ShapeProfile.Block(),
                    localPosition = new Vector3(0f, 4f, 0f), dimensions = new Vector3(12f, 1f, 12f),
                    colour = Color.white, role = PartRole.Head }
            };
            BuildRig();
            float footprint = TrampleZone.CreatureFootprint(_obstacle.transform, _host.rig);
            Assert.That(footprint, Is.EqualTo(Mathf.Sqrt(1.4f * 1.4f + 0.2f * 0.2f)).Within(0.001f));
            Assert.That(_zone.radius, Is.LessThan(1.6f), "The wide head and gesture arms must not clear a large disc.");
            _recipe.parts[2].localPosition = new Vector3(-0.3f, 0f, 0f);
            Assert.IsTrue(_host.rig.Recompose(_recipe, RenderTestAssets.LoadLookMaterial(),
                RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()));
            _zone.Refresh();
            Assert.That(_zone.radius, Is.EqualTo(Mathf.Sqrt(1.1f * 1.1f + 0.2f * 0.2f) + TrampleZone.Margin)
                .Within(0.001f), "The extra basal piece remains part of the footprint despite parenting to Body.");
        }

    }
}

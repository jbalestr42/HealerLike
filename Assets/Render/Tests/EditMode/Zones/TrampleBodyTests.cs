using System.Collections.Generic;
using HealerLike.Render.Grass;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public class TrampleBodyTests : TrampleZoneFixture
    {
        [Test]
        public void InitFootprint_Rig_RegistersOneBodyAndNoDisc()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.idle = default;
            BuildRig();
            _zone.Refresh();
            _zone.InitFootprint(_ground);

            Assert.IsTrue(_zone.isBody);
            Assert.AreEqual(1, _ground.bodyCount);
            Assert.IsFalse(Disc(out _));

            TestHelpers.InvokePrivate(_zone, "OnDisable");
            Assert.AreEqual(0, _ground.bodyCount, "A disabled body stops pressing.");
            TestHelpers.InvokePrivate(_zone, "OnEnable");
            Assert.AreEqual(1, _ground.bodyCount);
            TestHelpers.InvokePrivate(_zone, "OnDestroy");
            Assert.AreEqual(0, _ground.bodyCount);
        }

        [Test]
        public void AppendCapsules_Rig_SendsTheLowMeshesAndSkipsTheRaisedOnes()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.roots.count = 0;
            _recipe.idle = default;
            _recipe.parts = new[]
            {
                new CreaturePart { id = "Body", parent = -1, shape = ShapeProfile.Block(),
                    dimensions = Vector3.one * 0.4f, colour = Color.white, role = PartRole.Body },
                new CreaturePart { id = "Head", parent = 0, shape = ShapeProfile.Block(),
                    localPosition = new Vector3(0f, 4f, 0f), dimensions = Vector3.one * 0.5f,
                    colour = Color.white, role = PartRole.Head }
            };
            _recipe.arms = new ArmDefinition[0];
            BuildRig();
            BodyCapsule[] capsules = new BodyCapsule[8];

            int count = _zone.AppendCapsules(capsules, 1);

            Assert.AreEqual(1, count, "The head floats past the grass's reach.");
            Assert.Less(capsules[1].bottom, TrampleZone.BodyReach);
            Assert.Greater(capsules[1].radius, 0f);
            Assert.AreEqual(0, _zone.AppendCapsules(capsules, capsules.Length), "No room, nothing written.");
            Assert.AreEqual(0, _zone.AppendCapsules(null, 0));
        }

        [Test]
        public void AppendCapsules_Steady_AllocatesNothing()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.idle = default;
            BuildRig();
            BodyCapsule[] capsules = new BodyCapsule[TrampleZone.MaxCapsules];
            _zone.AppendCapsules(capsules, 0);

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 200; i++)
            {
                _zone.AppendCapsules(capsules, 0);
            }

            Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Test]
        public void AppendCapsules_Obstacle_IsNoBody()
        {
            _zone.Init(_ground);

            Assert.IsFalse(_zone.isBody);
            Assert.AreEqual(0, _zone.AppendCapsules(new BodyCapsule[4], 0));
            Assert.AreEqual(0, _ground.bodyCount);
        }

        [Test]
        public void CollectBodyMeshes_Rig_TakesThePartsAndRootsButNotTheArms()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.idle = default;
            _recipe.roots.count = 4;
            _recipe.roots.footRadius = 0.8f;
            _recipe.roots.thickness = 0.08f;
            BuildRig();
            List<MeshFilter> meshes = new List<MeshFilter>();

            _host.rig.CollectBodyMeshes(meshes);

            int roots = _recipe.roots.count * _recipe.roots.segments + _recipe.roots.count * (_recipe.roots.segments - 1);
            Assert.AreEqual(_recipe.parts.Length + roots, meshes.Count);
            Assert.AreEqual(_host.rig.partTransforms[0], meshes[meshes.Count - 1].transform,
                "The feet come first, so a many-part body never crowds them out.");
            foreach (MeshFilter filter in meshes)
            {
                Assert.AreNotEqual("LianaChain", filter.sharedMesh.name, "An arm is one mesh along its whole chain.");
            }

            bool hasArm = false;
            foreach (MeshFilter filter in _host.rig.root.GetComponentsInChildren<MeshFilter>())
            {
                hasArm |= filter.sharedMesh != null && filter.sharedMesh.name == "LianaChain";
            }

            Assert.IsTrue(hasArm, "The rig does draw its arm, the body only leaves it out.");
        }

    }
}

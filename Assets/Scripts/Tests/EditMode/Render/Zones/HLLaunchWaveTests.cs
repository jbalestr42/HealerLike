using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grass;
namespace HealerLike.Render.Zones
{
    public class HLLaunchWaveTests
    {
        [Test] public void ProjectileInitEmitsDirectionalPulseAndGustWhichOutliveProjectile()
        {
            var root = new GameObject("zones"); var shot = new GameObject("projectile");
            var source = new GameObject("source"); var target = new GameObject("target");
            try
            {
                var owner = root.AddComponent<HLZoneRegistry>(); owner.Initialize(new HLZoneFakeUpload());
                var field = root.AddComponent<HLGrassField>();
                TestHelpers.WithLoggingDisabled(() => target.AddComponent<Entity>());
                target.transform.position = Vector3.forward * 4;
                var projectile = shot.AddComponent<Projectile>(); var wave = shot.AddComponent<HLLaunchWave>(); wave.Field = field;
                projectile.Init(source, target, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
                owner.PublishFrame(0);
                Assert.AreEqual(1, owner.Count); Assert.AreEqual(5, owner.Snapshot[0].kind);
                Assert.AreEqual(4, owner.Snapshot[0].radius); Assert.AreEqual(1073741824u, owner.Snapshot[0].reserved);
                Assert.AreEqual(source.transform.position, owner.Snapshot[0].position);
                Assert.AreEqual(1, field.wind.current.y); Assert.AreEqual(0.13f, field.wind.current.w, 0.0001f);
                Object.DestroyImmediate(shot); owner.PublishFrame(0.2f); Assert.AreEqual(1, owner.Count);
                owner.PublishFrame(0.2f); Assert.AreEqual(0, owner.Count);
            }
            finally { Object.DestroyImmediate(shot); Object.DestroyImmediate(source); Object.DestroyImmediate(target); Object.DestroyImmediate(root); }
        }
    }
}

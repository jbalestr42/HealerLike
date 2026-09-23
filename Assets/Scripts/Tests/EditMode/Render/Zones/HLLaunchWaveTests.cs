using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Zones
{
    public class HLLaunchWaveTests
    {
        [Test]
        public void ProjectileInitEmitsDirectionalPulseAndGustWhichOutliveProjectile()
        {
            GameObject root = new GameObject("zones");
            GameObject shot = new GameObject("projectile");
            GameObject source = new GameObject("source");
            GameObject target = new GameObject("target");
            try
            {
                HLZoneRegistry owner = root.AddComponent<HLZoneRegistry>();
                owner.Initialize(new HLZoneFakeUpload());
                HLGrassField field = root.AddComponent<HLGrassField>();
                TestHelpers.WithLoggingDisabled(() => target.AddComponent<Entity>());
                target.transform.position = Vector3.forward * 4f;
                Projectile projectile = shot.AddComponent<Projectile>();
                HLLaunchWave wave = shot.AddComponent<HLLaunchWave>();
                wave.field = field;

                projectile.Init(source, target, new List<ABuffHandlerFactory>(), new List<AConsumerFactory>());
                owner.PublishFrame(0f);

                Assert.AreEqual(1, owner.count);
                Assert.AreEqual(5, owner.snapshot[0].kind);
                Assert.AreEqual(4, owner.snapshot[0].radius);
                Assert.AreEqual(1073741824u, owner.snapshot[0].reserved);
                Assert.AreEqual(source.transform.position, owner.snapshot[0].position);
                Assert.AreEqual(1, field.Wind.y);
                Assert.AreEqual(0.13f, field.Wind.w, 0.0001f);

                Object.DestroyImmediate(shot);
                owner.PublishFrame(0.2f);
                Assert.AreEqual(1, owner.count);

                owner.PublishFrame(0.2f);
                Assert.AreEqual(0, owner.count);
            }
            finally
            {
                Object.DestroyImmediate(shot);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(root);
            }
        }
    }
}

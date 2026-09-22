using HealerLike.Render.Environment;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    public class HLStageLaunchGustTests
    {
        [Test] public void LaunchPushesTheGustAlongSourceToTarget()
        {
            var go=new GameObject("HLGustTest");
            try {
                var gust=go.AddComponent<HLEnvironmentGust>();
                Assert.That(HLStageLaunchGust.Launch(gust,new Vector3(1,0,1),new Vector3(1,3,5)),Is.True);
                var wind=gust.Sample(Time.timeAsDouble+HLStageLaunchGust.Seconds*.5f);
                Assert.That(wind.z,Is.EqualTo(HLStageLaunchGust.Strength).Within(1e-3f));
                Assert.That(wind.x,Is.EqualTo(0).Within(1e-5f)); Assert.That(wind.y,Is.Zero);
                Assert.That(gust.Sample(Time.timeAsDouble+HLStageLaunchGust.Seconds+.01f).sqrMagnitude,Is.Zero);
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MissingGustIsANoOp() => Assert.That(HLStageLaunchGust.Launch(null,Vector3.zero,Vector3.forward),Is.False);
        [Test] public void InitWithoutProjectileTargetDoesNothing()
        {
            var go=new GameObject("HLGustTest"); var target=new GameObject("HLGustTarget");
            var saved=HLStageLaunchGust.Target;
            try {
                var gust=target.AddComponent<HLEnvironmentGust>(); HLStageLaunchGust.Target=gust;
                var launch=go.AddComponent<HLStageLaunchGust>();
                Assert.DoesNotThrow(()=>launch.Init(go));
                Assert.DoesNotThrow(()=>launch.Init(null));
                Assert.That(gust.Sample(Time.timeAsDouble+.1).sqrMagnitude,Is.Zero);
            } finally { HLStageLaunchGust.Target=saved; Object.DestroyImmediate(go); Object.DestroyImmediate(target); }
        }
    }
}

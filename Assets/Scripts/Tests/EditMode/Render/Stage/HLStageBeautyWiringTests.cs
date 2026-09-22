using HealerLike.Render.Environment;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stage
{
    public class HLStageBeautyWiringTests
    {
        [Test] public void BoardIsCentredOnTheGridWithFullExtent()
        {
            var board=HLStageBeautyWiring.Board(new Vector3(2,.5f,-1),16,8,1.5f);
            Assert.That(board.min,Is.EqualTo(new Vector3(-10,.5f,-7)));
            Assert.That(board.size,Is.EqualTo(new Vector3(24,0,12)));
        }
        [Test] public void PublishSetsTheLookGridGlobalsAndClearZeroesStrengthAndTip()
        {
            try {
                var board=HLStageBeautyWiring.Board(Vector3.zero,16,16,1);
                HLStageBeautyWiring.PublishGrid(board,1,HLStageBeautyWiring.GridStrength,HLStageBeautyWiring.TipLight);
                Assert.That((Vector3)Shader.GetGlobalVector("_HLGridOrigin"),Is.EqualTo(new Vector3(-8,0,-8)));
                Assert.That((Vector3)Shader.GetGlobalVector("_HLGridExtent"),Is.EqualTo(new Vector3(16,0,16)));
                Assert.That(Shader.GetGlobalFloat("_HLGridCell"),Is.EqualTo(1));
                Assert.That(Shader.GetGlobalFloat("_HLGridStrength"),Is.EqualTo(.12f).Within(1e-6f));
                Assert.That(Shader.GetGlobalFloat("_HLTipLight"),Is.EqualTo(.035f).Within(1e-6f));
                HLStageBeautyWiring.ClearGrid();
                Assert.That(Shader.GetGlobalFloat("_HLGridStrength"),Is.Zero);
                Assert.That(Shader.GetGlobalFloat("_HLTipLight"),Is.Zero);
            } finally { HLStageBeautyWiring.ClearGrid(); }
        }
        [Test] public void TrampleIsTheFootprintPlusMarginAndAttachIsIdempotent()
        {
            Assert.That(HLStageBeautyWiring.TrampleRadius(.5f),Is.EqualTo(.65f).Within(1e-6f));
            Assert.That(HLStageBeautyWiring.TrampleRadius(-1),Is.EqualTo(HLStageBeautyWiring.TrampleMargin));
            var go=new GameObject("HLTrampleTest");
            try {
                var first=HLStageBeautyWiring.AttachTrample(go,1);
                var second=HLStageBeautyWiring.AttachTrample(go,2);
                Assert.That(second,Is.SameAs(first));
                Assert.That(go.GetComponents<HLTrampleZone>().Length,Is.EqualTo(1));
                Assert.That(second.Radius,Is.EqualTo(2.15f).Within(1e-6f));
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void DisableClearsItsOwnGustTargetOnly()
        {
            var go=new GameObject("HLWiringTest"); go.SetActive(false);
            var saved=HLStageLaunchGust.Target;
            try {
                var gust=go.AddComponent<HLEnvironmentGust>(); var wiring=go.AddComponent<HLStageBeautyWiring>();
                wiring.Configure(null,null,gust,null,null);
                TestHelpers.InvokePrivate(wiring,"OnEnable"); Assert.That(HLStageLaunchGust.Target,Is.SameAs(gust));
                TestHelpers.InvokePrivate(wiring,"OnDisable"); Assert.That(HLStageLaunchGust.Target,Is.Null);
                var other=new GameObject("HLOtherGust").AddComponent<HLEnvironmentGust>();
                HLStageLaunchGust.Target=other; TestHelpers.InvokePrivate(wiring,"OnDisable");
                Assert.That(HLStageLaunchGust.Target,Is.SameAs(other)); Object.DestroyImmediate(other.gameObject);
            } finally { HLStageLaunchGust.Target=saved; HLStageBeautyWiring.ClearGrid(); Object.DestroyImmediate(go); }
        }
    }
}

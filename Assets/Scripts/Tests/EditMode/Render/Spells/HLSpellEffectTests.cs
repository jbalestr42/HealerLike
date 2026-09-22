using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Spells
{
    public class HLSpellEffectTests
    {
        [Test] public void StatusPoseUsesObserverTimeAndRemovalOpensPlates()
        {
            var go=new GameObject("HLStatus");
            try {
                var fx=go.AddComponent<HLSpellEffect>(); fx.kind=HLSpellEffectKind.Buff;
                fx.SetStatus(1,1,4,HLClockKind.Simulation,default);
                var pose=fx.parts[0].localRotation;fx.Advance(7);Assert.AreEqual(pose,fx.parts[0].localRotation);
                fx.SetStatus(2,2,4,HLClockKind.Simulation,default);Assert.AreNotEqual(pose,fx.parts[0].localRotation);
            } finally { Object.DestroyImmediate(go); }
            go=new GameObject("HLShield");
            try {
                var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Shield;
                fx.SetStatus(1,.25f,4,HLClockKind.Simulation,default);var closed=fx.parts[0].localRotation;
                fx.BeginRemoval();fx.Advance(.25f);Assert.IsTrue(fx.RemovalComplete);Assert.AreNotEqual(closed,fx.parts[0].localRotation);
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void DripsFollowPeriodAndTintResetsOnRemoval()
        {
            var go=new GameObject("HLDrip");
            try {
                var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Drip;fx.PeriodSeconds=2;
                Color tint=Color.clear;fx.OnTint.AddListener(c=>tint=c);
                fx.SetStatus(1,2,6,HLClockKind.Simulation,default);var position=fx.parts[0].localPosition;
                Assert.AreEqual((Color)new Color32(242,96,122,255),tint);
                fx.SetStatus(1,3,6,HLClockKind.Simulation,default);Assert.Less(fx.parts[0].localPosition.y,position.y);
                fx.SetStatus(1,4,6,HLClockKind.Simulation,default);Assert.AreEqual(position,fx.parts[0].localPosition);
                fx.BeginRemoval();Assert.AreEqual(Color.white,tint);
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void LinkDotsTravelAndExpireAtAuthoredLifetime()
        {
            var go=new GameObject("HLBeam");try {
                var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Chain;fx.SetEndpoints(Vector3.zero,Vector3.right*4);
                fx.Advance(.15f);Assert.Greater(fx.parts[1].position.x,0);Assert.Greater(fx.parts[1].position.y,0);
            } finally {Object.DestroyImmediate(go);}
        }
        [Test] public void SideRimUpdatesWithoutDuplicatingAndCriticalUsesTwoRings()
        {
            var go=new GameObject("HLSide");try{var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Heal;fx.Initialize();int count=go.transform.childCount;fx.SetSide(Entity.EntityType.Player);fx.SetSide(Entity.EntityType.Computer);Assert.AreEqual(count+1,go.transform.childCount);HLSpellPrimitives.AddCritical(fx);Assert.AreEqual(count+3,go.transform.childCount);}finally{Object.DestroyImmediate(go);}
        }
        [Test] public void HealRisesAndStatusDoesNotAutoExpire()
        {
            var go=new GameObject("HLHeal");var status=new GameObject("HLStatus");
            try{var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Heal;fx.Initialize();float before=fx.parts[0].localPosition.y;fx.Advance(.2f);Assert.Greater(fx.parts[0].localPosition.y,before);
                var s=status.AddComponent<HLSpellEffect>();s.SetStatus(3,2,4,HLClockKind.Realtime,default);s.Advance(10);Assert.AreEqual(3,s.Stacks);Assert.AreEqual(2,s.ElapsedSeconds);Assert.IsTrue(status);}
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(status);}
        }
        [Test] public void ChargePlatesUseObservedChargeCountAndPreserveStackData()
        {
            var go=new GameObject("HLShield");try{var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Shield;
                fx.SetStatus(9,0,4,HLClockKind.Simulation,new HLSpellSignature{operation=HLOperation.Attribute,attribute=AttributeType.HitArmor,hasAttribute=true});fx.SetShieldState(2);
                int count=0;foreach(var part in fx.parts)if(part.gameObject.activeSelf)count++;Assert.AreEqual(2,count);Assert.AreEqual(9,fx.Stacks);
            }finally{Object.DestroyImmediate(go);}
        }
        [Test] public void ChainUsesSuppliedEndpointsAndCurvesAboveChord()
        {
            var go=new GameObject("HLChain");try{var fx=go.AddComponent<HLSpellEffect>();fx.kind=HLSpellEffectKind.Chain;fx.SetEndpoints(Vector3.zero,Vector3.right*4);Assert.AreEqual(Vector3.zero,fx.parts[1].position);Assert.Greater(fx.parts[17].position.y,0);Assert.AreEqual(32,fx.parts.Length);}finally{Object.DestroyImmediate(go);}
        }
    }
}

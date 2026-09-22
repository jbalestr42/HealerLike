using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellGrammarTests
    {
        readonly HLSpellGrammar grammar=new HLSpellGrammar();
        static ConsumerData Flat(float value,bool bypass=true)=>new ConsumerData { value=new FlatValue { data=new FlatValueData { value=value } },ignoreDamageReduction=bypass };
        [TestCase(5,HLSign.Negative)] [TestCase(-5,HLSign.Positive)] [TestCase(0,HLSign.Zero)]
        public void ConsumerInvertsFlatValue(float value,HLSign sign)
        {var r=grammar.DescribeData(Flat(value));Assert.AreEqual(sign,r.Signature.sign);Assert.AreEqual(-value,r.PreviewAmount);}
        [Test] public void SourceReadIsNotDestinationAndPostReductionMultiplierIsPreserved()
        {
            var c=new HLGrammarContext { SourceAttribute=t=>t==AttributeType.HealPower?20:null };
            var r=grammar.Consumer(new ConsumerData { value=new AttributeValue { data=new AttributeValueData { type=AttributeType.HealPower,multiplier=1 } },ignoreConsumerPrevention=true },c,multiplier:-.8f);
            Assert.AreEqual(16,r.PreviewAmount);Assert.AreEqual(AttributeType.HealthMax,r.Signature.attribute);Assert.AreEqual(AttributeType.HealPower,r.ReadAttribute);Assert.IsFalse(r.IgnoreReduction);Assert.IsTrue(r.IgnorePrevention);
        }
        [Test] public void HealthProvenanceSurvivesEqualMagnitudeAndMaxIgnoresInverse()
        {
            var c=new HLGrammarContext { SourceHealth=50,SourceMaxHealth=100 };
            var data=new ConsumerData { value=new CurrentHealthValue { data=new CurrentHealthValueData { multiplier=.1f } } };
            var a=grammar.DescribeData(data,c);((CurrentHealthValue)data.value).data.inverse=true;var b=grammar.DescribeData(data,c);
            Assert.AreEqual(a.PreviewAmount,b.PreviewAmount);Assert.AreNotEqual(a.Expression,b.Expression);
            data.value=new MaxHealthValue { data=new MaxHealthValueData { multiplier=.1f,inverse=true } };
            Assert.AreEqual(-10,grammar.DescribeData(data,c).PreviewAmount);
        }
        [Test] public void ActiveAndInstantMultiplyDifferAndFlatArmorIsHarmful()
        {
            var r=grammar.DescribeData(new FlatModifierData {type=AttributeType.Damage,modifierType=AttributeModifierType.Multiply,value=.5f});
            Assert.AreEqual(HLSign.Negative,r.Signature.sign);
            r=grammar.DescribeData(new FlatModifierData {type=AttributeType.FlatArmor,value=2});Assert.AreEqual(HLSign.Negative,r.Signature.sign);
        }
        [Test] public void BrokenModifiersAndUnknownDataAreInvalidWithoutConstruction()
        {Assert.IsFalse(grammar.DescribeData(new SlowModifierData()).IsValid);Assert.IsFalse(grammar.DescribeData(new TimeModifierData()).IsValid);Assert.IsFalse(grammar.DescribeData(new object()).IsValid);Assert.IsFalse(grammar.DescribeData(Flat(float.NaN)).IsValid);}
        [Test] public void HandlerPreservesDuplicateAtomsAndPeriodicLifetime()
        {
            var f=ScriptableObject.CreateInstance<ApplyConsumerBuffFactory>();var consumer=ScriptableObject.CreateInstance<ConsumerFactory>();consumer.data=Flat(5);f.data=new ApplyConsumerBuffData {consumerFactory=consumer};
            try
            {
                var r=grammar.DescribeData(new BuffHandlerData {durationType=DurationType.Infinite,isPeriodic=true,periodDuration=1.5f,buffFactoryList=new List<ABuffFactory>{f,f}});
                Assert.AreEqual(2,r.Children.Count);Assert.AreEqual(HLDurationShape.Infinite,r.Children[0].Signature.duration);Assert.AreEqual(1.5f,r.Children[0].PeriodSeconds);Assert.AreEqual(HLTempo.HandlerTick,r.Children[0].Signature.tempo);
            }finally{Object.DestroyImmediate(f);Object.DestroyImmediate(consumer);}
        }
        [Test] public void CharacterGroupIsNotAreaAndNamesDoNotClassify()
        {
            var f=ScriptableObject.CreateInstance<ConsumerFactory>();f.data=Flat(10);
            try
            {
                var data=new ApplyConsumerCharacterSkillData {consumer=f,isSingle=false,multiplier=-1,name="Anything"};
                var a=grammar.DescribeData(data);data.name="Changed";var b=grammar.DescribeData(data);
                Assert.AreEqual(HLTopology.Group,a.Signature.topology);Assert.AreEqual(HLSign.Positive,a.Children[0].Signature.sign);Assert.AreEqual(a.DeliveryData,b.DeliveryData);
            }finally{Object.DestroyImmediate(f);}
        }
        [Test] public void SnapshotPreservesOrderDuplicatesAndOmitsIdentity()
        {
            var a=ScriptableObject.CreateInstance<FlatModifierFactory>();var b=ScriptableObject.CreateInstance<FlatModifierFactory>();
            try{a.data=new FlatModifierData {value=1};b.data=new FlatModifierData {value=2};
                Assert.AreNotEqual(HLSpellGrammar.Snapshot(new[]{a,b}),HLSpellGrammar.Snapshot(new[]{b,a}));
                Assert.AreNotEqual(HLSpellGrammar.Snapshot(new[]{a,a}),HLSpellGrammar.Snapshot(new[]{a}));
                string before=HLSpellGrammar.Snapshot(a);a.name="Renamed";a.uniqueID="new-id";Assert.AreEqual(before,HLSpellGrammar.Snapshot(a));
            }finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
        }
        [Test] public void CharacterCostsAreSeparateSelfManaAtoms()
        {
            var consumer=ScriptableObject.CreateInstance<ConsumerFactory>();var cost=ScriptableObject.CreateInstance<ResourceValidatorFactory>();
            try { consumer.data=Flat(10);cost.data=new ResourceValidatorData{consumer=consumer};
                var data=new ApplyConsumerCharacterSkillData{consumer=consumer,multiplier=-1,validators=new List<ACharacterSkillValidatorFactory>{cost}};
                var recipe=grammar.DescribeData(data);Assert.AreEqual(2,recipe.Children.Count);Assert.AreEqual(AttributeType.ManaMax,recipe.Children[1].Signature.attribute);Assert.AreEqual(HLTopology.Self,recipe.Children[1].Signature.topology);Assert.AreEqual(HLSign.Negative,recipe.Children[1].Signature.sign);
            } finally { Object.DestroyImmediate(consumer);Object.DestroyImmediate(cost); }
        }
        [Test] public void CurveSnapshotRetainsCurveKeys()
        {
            var a=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,0),new Keyframe(1,1)));
            var b=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,0),new Keyframe(1,2)));
            Assert.AreNotEqual(HLSpellGrammar.Snapshot(a),HLSpellGrammar.Snapshot(b));
        }
        [Test] public void MissingStatsRemainUnknownAndNoBypassHealIsConditional()
        {Assert.AreEqual(HLSign.Conditional,grammar.DescribeData(new ConsumerData {value=new AttributeValue {data=new AttributeValueData()}}).Signature.sign);Assert.AreEqual(HLSign.Conditional,grammar.DescribeData(Flat(-2,false)).Signature.sign);}
    }
}

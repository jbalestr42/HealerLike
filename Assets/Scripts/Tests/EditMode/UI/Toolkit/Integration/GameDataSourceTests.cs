using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public class GameDataSourceTests : UiAccessorFixture
    {
        [TestCase(typeof(ItemFactory), typeof(ItemData))]
        [TestCase(typeof(Item), typeof(ItemData))]
        [TestCase(typeof(ApplyConsumerCharacterSkillFactory), typeof(ApplyConsumerCharacterSkillData))]
        [TestCase(typeof(ApplyConsumerCharacterSkill), typeof(ApplyConsumerCharacterSkillData))]
        [TestCase(typeof(ConfigurableSkillFactory), typeof(ConfigurableSkillData))]
        [TestCase(typeof(ConfigurableSkill), typeof(ConfigurableSkillData))]
        [TestCase(typeof(ApplyConsumerBuffFactory), typeof(ApplyConsumerBuffData))]
        [TestCase(typeof(ApplyConsumerBuff), typeof(ApplyConsumerBuffData))]
        [TestCase(typeof(BuffHandlerFactory), typeof(BuffHandlerData))]
        [TestCase(typeof(BuffHandler), typeof(BuffHandlerData))]
        [TestCase(typeof(ConsumerFactory), typeof(ConsumerData))]
        [TestCase(typeof(Consumer), typeof(ConsumerData))]
        [TestCase(typeof(FlatModifier), typeof(FlatModifierData))]
        [TestCase(typeof(FlatValue), typeof(FlatValueData))]
        [TestCase(typeof(OnlySelfValidatorFactory), typeof(OnlySelfValidatorData))]
        [TestCase(typeof(OnlySelfValidator), typeof(OnlySelfValidatorData))]
        [TestCase(typeof(DurationValidatorFactory), typeof(DurationValidatorData))]
        [TestCase(typeof(DurationValidator), typeof(DurationValidatorData))]
        [TestCase(typeof(BounceProjectileBehaviourFactory), typeof(BounceProjectileBehaviourData))]
        [TestCase(typeof(BounceProjectileBehaviour), typeof(BounceProjectileBehaviourData))]
        [TestCase(typeof(DurationSkillStepFactory), typeof(DurationSkillStepData))]
        [TestCase(typeof(DurationSkillStep), typeof(DurationSkillStepData))]
        [TestCase(typeof(AnimationOnSkillTriggerFactory), typeof(AnimationOnSkillTriggerData))]
        [TestCase(typeof(AnimationOnSkillTrigger), typeof(AnimationOnSkillTriggerData))]
        public void SourceData_DataReplacementKeepsAuthoritativeIdentity(System.Type ownerType, System.Type dataType)
        {
            object owner;
            if (typeof(ScriptableObject).IsAssignableFrom(ownerType))
            {
                owner = ScriptableObject.CreateInstance(ownerType);
            }
            else if (typeof(Component).IsAssignableFrom(ownerType))
            {
                owner = root.AddComponent(ownerType);
            }
            else
            {
                owner = System.Activator.CreateInstance(ownerType);
            }

            try
            {
                IGameDataSource source = (IGameDataSource)owner;
                System.Reflection.FieldInfo field = ownerType.GetField("data");
                object first = System.Activator.CreateInstance(dataType);
                object replacement = System.Activator.CreateInstance(dataType);
                field.SetValue(owner, first);
                Assert.AreSame(first, source.sourceData);

                field.SetValue(owner, replacement);
                Assert.AreSame(replacement, source.sourceData);
                field.SetValue(owner, null);
                Assert.IsNull(source.sourceData);
            }
            finally
            {
                if (owner is ScriptableObject)
                {
                    Object.DestroyImmediate((ScriptableObject)owner);
                }
            }
        }
    }
}

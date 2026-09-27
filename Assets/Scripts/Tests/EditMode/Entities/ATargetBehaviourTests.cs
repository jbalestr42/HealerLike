using System;
using NUnit.Framework;

namespace Entities
{

public class ATargetBehaviourTests
{
    [Test]
    public void IsSupported_Fastest_IsFalse()
    {
        Assert.IsFalse(ATargetBehaviour.IsSupported(TargetBehaviourType.Fastest));
    }

    [TestCase(TargetBehaviourType.First)]
    [TestCase(TargetBehaviourType.Nearest)]
    [TestCase(TargetBehaviourType.LowestHealth)]
    [TestCase(TargetBehaviourType.Random)]
    [TestCase(TargetBehaviourType.Farest)]
    public void Create_SupportedType_BuildsABehaviourOfThatType(TargetBehaviourType type)
    {
        Assert.IsTrue(ATargetBehaviour.IsSupported(type));
        Assert.AreEqual(type, ATargetBehaviour.Create(type).targetType);
    }

    [Test]
    public void Create_UnsupportedType_IsNull()
    {
        ATargetBehaviour targetBehaviour = null;
        TestHelpers.WithLoggingDisabled(() => targetBehaviour = ATargetBehaviour.Create(TargetBehaviourType.Fastest));

        Assert.IsNull(targetBehaviour);
    }

    [Test]
    public void GetNextSupportedType_SkipsTheTypesWithoutImplementation()
    {
        Assert.AreEqual(TargetBehaviourType.LowestHealth, ATargetBehaviour.GetNextSupportedType(TargetBehaviourType.Nearest));
    }

    [Test]
    public void GetNextSupportedType_AfterTheLastType_GoesBackToTheFirst()
    {
        Assert.AreEqual(TargetBehaviourType.First, ATargetBehaviour.GetNextSupportedType(TargetBehaviourType.Farest));
    }

    [Test]
    public void GetNextSupportedType_CyclingFromAnyType_OnlyGivesSupportedTypes()
    {
        foreach (TargetBehaviourType type in Enum.GetValues(typeof(TargetBehaviourType)))
        {
            Assert.IsTrue(ATargetBehaviour.IsSupported(ATargetBehaviour.GetNextSupportedType(type)), type.ToString());
        }
    }
}

}

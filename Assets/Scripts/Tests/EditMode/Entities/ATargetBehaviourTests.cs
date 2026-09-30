using System;
using NUnit.Framework;

namespace Entities
{

public class ATargetBehaviourTests
{
    // A value no longer in the enum, e.g. read from an old asset
    const TargetBehaviourType UnknownType = (TargetBehaviourType)99;

    [Test]
    public void IsSupported_UnknownType_IsFalse()
    {
        Assert.IsFalse(ATargetBehaviour.IsSupported(UnknownType));
    }

    [Test]
    public void IsSupported_EveryTypeOfTheEnum_IsTrue()
    {
        foreach (TargetBehaviourType type in Enum.GetValues(typeof(TargetBehaviourType)))
        {
            Assert.IsTrue(ATargetBehaviour.IsSupported(type), type.ToString());
        }
    }

    [TestCase(TargetBehaviourType.First)]
    [TestCase(TargetBehaviourType.Nearest)]
    [TestCase(TargetBehaviourType.LowestHealth)]
    [TestCase(TargetBehaviourType.Random)]
    [TestCase(TargetBehaviourType.Farest)]
    [TestCase(TargetBehaviourType.HighestHealth)]
    public void Create_SupportedType_BuildsABehaviourOfThatType(TargetBehaviourType type)
    {
        Assert.IsTrue(ATargetBehaviour.IsSupported(type));
        Assert.AreEqual(type, ATargetBehaviour.Create(type).targetType);
    }

    [Test]
    public void Create_UnsupportedType_IsNull()
    {
        ATargetBehaviour targetBehaviour = null;
        TestHelpers.WithLoggingDisabled(() => targetBehaviour = ATargetBehaviour.Create(UnknownType));

        Assert.IsNull(targetBehaviour);
    }

    [Test]
    public void GetNextSupportedType_GivesTheNextTypeOfTheEnum()
    {
        Assert.AreEqual(TargetBehaviourType.HighestHealth, ATargetBehaviour.GetNextSupportedType(TargetBehaviourType.Nearest));
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

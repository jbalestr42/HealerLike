using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneAssemblyProfileTests
{
    StoneAssemblyProfile _profile;

    [SetUp]
    public void SetUp()
    {
        _profile = ScriptableObject.CreateInstance<StoneAssemblyProfile>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_profile);
    }

    [Test]
    public void CreateInstance_Defaults_ShedsTheThirdPartAtHalfHealth()
    {
        Assert.AreEqual(0.5f, _profile.shedHealthFraction);
        Assert.AreEqual(2, _profile.detachablePartIndex);
    }
}

}

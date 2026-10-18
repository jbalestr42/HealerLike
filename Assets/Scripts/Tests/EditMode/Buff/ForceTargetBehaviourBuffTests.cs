using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The holder picks its targets another way as long as the buff lasts (e.g. Poison: at random)
public class ForceTargetBehaviourBuffTests
{
    GameObject _holder;
    FakeTargetProvider _targetProvider;
    ForceTargetBehaviourBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _holder = new GameObject("Holder");
        _targetProvider = _holder.AddComponent<FakeTargetProvider>();
        _targetProvider.targetBehaviourType = TargetBehaviourType.HighestHealth;
        _buff = new ForceTargetBehaviourBuff { data = new ForceTargetBehaviourBuffData { targetBehaviourType = TargetBehaviourType.Random } };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_holder);
    }

    [Test]
    public void Add_TheHolderPicksItsTargetsTheForcedWay()
    {
        _buff.Add(_holder, _holder);

        Assert.AreEqual(TargetBehaviourType.Random, _targetProvider.targetBehaviourType);
    }

    [Test]
    public void Remove_TheHolderPicksItsTargetsAsBefore()
    {
        _buff.Add(_holder, _holder);

        _buff.Remove(_holder, _holder);

        Assert.AreEqual(TargetBehaviourType.HighestHealth, _targetProvider.targetBehaviourType);
    }

    [Test]
    public void Remove_HolderGone_NoError()
    {
        _buff.Add(_holder, _holder);

        Assert.DoesNotThrow(() => _buff.Remove(_holder, null));
    }
}

}

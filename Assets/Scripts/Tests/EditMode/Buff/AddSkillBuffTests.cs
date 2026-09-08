using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

public class FakeSkill : ASkill
{
    public override void UpdateBehaviour(GameObject source) { }
    public override void Reset() { }
}

public class FakeStackableSkill : ASkill, IStackableBuff
{
    public int stackCount;
    public int unstackCount;

    public override void UpdateBehaviour(GameObject source) { }
    public override void Reset() { }
    public void Stack(GameObject source, GameObject target) => stackCount++;
    public void Unstack(GameObject source, GameObject target) => unstackCount++;
}

public class FakeSkillFactory : ASkillFactory
{
    public override ASkill AddSkill(GameObject target) => target.AddComponent<FakeSkill>();
}

public class FakeStackableSkillFactory : ASkillFactory
{
    public override ASkill AddSkill(GameObject target) => target.AddComponent<FakeStackableSkill>();
}

public class AddSkillBuffTests
{
    GameObject _source;
    GameObject _target;
    readonly List<Object> _scriptableObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    AddSkillBuff CreateBuff(ASkillFactory skillFactory)
    {
        return new AddSkillBuff { data = new AddSkillBuffData { skillFactory = skillFactory } };
    }

    [Test]
    public void Add_AddsSkillInstanceToTarget()
    {
        AddSkillBuff buff = CreateBuff(CreateTracked<FakeSkillFactory>());

        buff.Add(_source, _target);

        Assert.IsNotNull(_target.GetComponent<FakeSkill>());
    }

    [Test]
    public void IsStackable_NonStackableSkill_ReturnsFalse()
    {
        AddSkillBuff buff = CreateBuff(CreateTracked<FakeSkillFactory>());
        buff.Add(_source, _target);

        Assert.IsFalse(buff.isStackable);
    }

    [Test]
    public void IsStackable_StackableSkill_ReturnsTrue()
    {
        AddSkillBuff buff = CreateBuff(CreateTracked<FakeStackableSkillFactory>());
        buff.Add(_source, _target);

        Assert.IsTrue(buff.isStackable);
    }

    [Test]
    public void Stack_DelegatesToSkillInstance()
    {
        AddSkillBuff buff = CreateBuff(CreateTracked<FakeStackableSkillFactory>());
        buff.Add(_source, _target);
        FakeStackableSkill skill = _target.GetComponent<FakeStackableSkill>();

        buff.Stack(_source, _target);

        Assert.AreEqual(1, skill.stackCount);
    }

    [Test]
    public void Unstack_DelegatesToSkillInstance()
    {
        AddSkillBuff buff = CreateBuff(CreateTracked<FakeStackableSkillFactory>());
        buff.Add(_source, _target);
        FakeStackableSkill skill = _target.GetComponent<FakeStackableSkill>();

        buff.Unstack(_source, _target);

        Assert.AreEqual(1, skill.unstackCount);
    }
}

}

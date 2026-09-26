using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

public class BuffHandlerValidatorTests
{
    GameObject _source;
    GameObject _target;
    BuffManager _buffManager;
    Buff.FakeBuffFactory _buffFactory;
    BuffHandlerFactory _handlerFactory;
    BuffHandlerFactory _otherHandlerFactory;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Source");
        _target = new GameObject("Target");
        _buffManager = _target.AddComponent<BuffManager>();
        _buffManager.isEnabled = true;

        _buffFactory = ScriptableObject.CreateInstance<Buff.FakeBuffFactory>();
        _buffFactory.data = new Buff.FakeBuffData();
        _handlerFactory = CreateHandlerFactory();
        _otherHandlerFactory = CreateHandlerFactory();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_target);
        Object.DestroyImmediate(_buffFactory);
        Object.DestroyImmediate(_handlerFactory);
        Object.DestroyImmediate(_otherHandlerFactory);
    }

    BuffHandlerFactory CreateHandlerFactory()
    {
        BuffHandlerFactory handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 100f,
            buffFactoryList = new List<ABuffFactory> { _buffFactory },
            tags = new List<GameplayTag>(),
        };
        return handlerFactory;
    }

    ATargetValidator CreateValidator(bool mustHave)
    {
        BuffHandlerValidatorFactory factory = ScriptableObject.CreateInstance<BuffHandlerValidatorFactory>();
        factory.data = new BuffHandlerValidatorData { buffHandlerFactory = _handlerFactory, mustHave = mustHave };
        ATargetValidator validator = factory.GetTargetValidator();
        Object.DestroyImmediate(factory);
        return validator;
    }

    void ApplyHandler(ABuffHandlerFactory handlerFactory)
    {
        _buffManager.AddHandler(handlerFactory, _source, _target);
        _buffManager.ForceUpdate();
    }

    [Test]
    public void MustNotHave_TargetWithoutTheHandler_IsValid()
    {
        Assert.IsTrue(CreateValidator(false).IsValid(_source, _target));
    }

    [Test]
    public void MustNotHave_TargetWithTheHandler_IsNotValid()
    {
        ApplyHandler(_handlerFactory);

        Assert.IsFalse(CreateValidator(false).IsValid(_source, _target));
    }

    [Test]
    public void MustNotHave_TargetWithAnotherHandler_IsValid()
    {
        ApplyHandler(_otherHandlerFactory);

        Assert.IsTrue(CreateValidator(false).IsValid(_source, _target));
    }

    [Test]
    public void MustHave_TargetWithTheHandler_IsValid()
    {
        ApplyHandler(_handlerFactory);

        Assert.IsTrue(CreateValidator(true).IsValid(_source, _target));
    }

    [Test]
    public void MustHave_TargetWithoutTheHandler_IsNotValid()
    {
        Assert.IsFalse(CreateValidator(true).IsValid(_source, _target));
    }
}

}

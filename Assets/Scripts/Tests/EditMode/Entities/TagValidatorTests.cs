using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// Targets by tag (e.g. the Shield Wardens only recharging the Iron Matron)
public class TagValidatorTests
{
    readonly TestUnits _units = new TestUnits();
    GameplayTag _tag;
    GameplayTag _otherTag;
    Entity _source;
    Entity _target;

    [SetUp]
    public void SetUp()
    {
        _tag = ScriptableObject.CreateInstance<GameplayTag>();
        _tag.name = "IronMatron";
        _otherTag = ScriptableObject.CreateInstance<GameplayTag>();
        _otherTag.name = "Other";
        _source = _units.Create(100f, 100f, "Source");
        _target = _units.Create(100f, 100f, "Target");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_tag);
        Object.DestroyImmediate(_otherTag);
    }

    ATargetValidator CreateValidator(bool mustHave)
    {
        TagValidatorFactory factory = ScriptableObject.CreateInstance<TagValidatorFactory>();
        factory.data = new TagValidatorData { tag = _tag, mustHave = mustHave };
        ATargetValidator validator = factory.GetTargetValidator();
        Object.DestroyImmediate(factory);
        return validator;
    }

    [Test]
    public void MustHave_TargetWithTheTag_IsValid()
    {
        _target.AddTag(_tag);

        Assert.IsTrue(CreateValidator(true).IsValid(_source.gameObject, _target.gameObject));
    }

    [Test]
    public void MustHave_TargetWithoutTheTag_IsNotValid()
    {
        Assert.IsFalse(CreateValidator(true).IsValid(_source.gameObject, _target.gameObject));
    }

    [Test]
    public void MustHave_TargetWithAnotherTag_IsNotValid()
    {
        _target.AddTag(_otherTag);

        Assert.IsFalse(CreateValidator(true).IsValid(_source.gameObject, _target.gameObject));
    }

    [Test]
    public void MustNotHave_TargetWithTheTag_IsNotValid()
    {
        _target.AddTag(_tag);

        Assert.IsFalse(CreateValidator(false).IsValid(_source.gameObject, _target.gameObject));
    }

    [Test]
    public void MustNotHave_TargetWithoutTheTag_IsValid()
    {
        Assert.IsTrue(CreateValidator(false).IsValid(_source.gameObject, _target.gameObject));
    }

    [Test]
    public void MustHave_TargetWithoutEntity_IsNotValid()
    {
        GameObject bare = new GameObject("Bare");

        bool isValid = CreateValidator(true).IsValid(_source.gameObject, bare);

        Object.DestroyImmediate(bare);
        Assert.IsFalse(isValid);
    }
}

}

using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// A tag given to the holder as long as the buff lasts (e.g. the taunt of the Taunt Totem)
public class AddTagBuffTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    GameplayTag _tag;
    AddTagBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(100f, 100f, "Owner");
        _tag = ScriptableObject.CreateInstance<GameplayTag>();
        _tag.name = "Taunt";
        _buff = new AddTagBuff { data = new AddTagBuffData { tag = _tag } };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_tag);
    }

    [Test]
    public void Add_GivesTheTag()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);

        Assert.IsTrue(_owner.HasTag(_tag));
    }

    [Test]
    public void Remove_TakesTheTagBack()
    {
        _buff.Add(_owner.gameObject, _owner.gameObject);
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Assert.IsFalse(_owner.HasTag(_tag));
    }
}

}

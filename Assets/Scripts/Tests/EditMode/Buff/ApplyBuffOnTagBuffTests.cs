using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// A buff given when the holder gets a tag, kept while it has it, or given on each buff carrying it (e.g. the Blessed
// effects of the Cleric units)
public class ApplyBuffOnTagBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    Entity _owner;
    BuffManager _buffManager;
    GameplayTag _blessed;
    BuffHandlerFactory _handlerFactory;
    ApplyBuffOnTagBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(100f, 100f, "Owner");
        _buffManager = _owner.GetComponent<BuffManager>();
        _buffManager.isEnabled = true;
        _blessed = CreateTag("Blessed");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    GameplayTag CreateTag(string name, GameplayTag parent = null)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        if (parent != null)
        {
            TestHelpers.SetPrivateField(tag, "_parent", parent);
        }
        _scriptableObjects.Add(tag);
        return tag;
    }

    // The buff given: 5s for WhenTagged and EachApplication, endless for WhileTagged, up to maxStacks stacks
    void AddBuff(TagTrigger trigger, int maxStacks = 1)
    {
        FakeBuffFactory fakeBuff = ScriptableObject.CreateInstance<FakeBuffFactory>();
        fakeBuff.data = new FakeBuffData();
        _handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _handlerFactory.data = new BuffHandlerData
        {
            durationType = trigger == TagTrigger.WhenTagged ? DurationType.Duration : DurationType.Infinite,
            duration = 5f,
            maxStacks = maxStacks,
            buffFactoryList = new List<ABuffFactory> { fakeBuff },
        };
        _scriptableObjects.Add(fakeBuff);
        _scriptableObjects.Add(_handlerFactory);

        _buff = new ApplyBuffOnTagBuff { data = new ApplyBuffOnTagBuffData { tag = _blessed, trigger = trigger, buffHandlerFactory = _handlerFactory } };
        _buff.Add(_owner.gameObject, _owner.gameObject);
    }

    // A 4s blessing, tagged with what it gives like the Blessed of the Cleric
    BuffHandlerFactory CreateBlessing(GameplayTag tag)
    {
        AddTagBuffFactory addTag = ScriptableObject.CreateInstance<AddTagBuffFactory>();
        addTag.data = new AddTagBuffData { tag = tag };
        BuffHandlerFactory blessing = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        blessing.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 4f,
            maxStacks = 1,
            stackAcrossSources = true,
            buffFactoryList = new List<ABuffFactory> { addTag },
            tags = new List<GameplayTag> { tag },
        };
        _scriptableObjects.Add(addTag);
        _scriptableObjects.Add(blessing);
        return blessing;
    }

    void Bless(BuffHandlerFactory blessing)
    {
        _buffManager.AddHandler(blessing, _owner.gameObject, _owner.gameObject);
        _buffManager.ForceUpdate();
    }

    int GetStacks()
    {
        _buffManager.ForceUpdate();
        BuffManager.BuffHandlerData handler = _buffManager.GetActiveHandlers().Find(h => h.buffHandlerFactory == _handlerFactory);
        return handler != null ? handler.currentStacks : 0;
    }

    bool HasBuff()
    {
        _buffManager.ForceUpdate();
        return _buffManager.GetActiveHandlers().Exists(handler => handler.buffHandlerFactory == _handlerFactory);
    }

    [Test]
    public void WhenTagged_GettingTheTag_GivesTheBuffFromItself()
    {
        AddBuff(TagTrigger.WhenTagged);

        _owner.AddTag(_blessed);

        Assert.IsTrue(HasBuff());
        BuffManager.BuffHandlerData handler = _buffManager.GetActiveHandlers().Find(h => h.buffHandlerFactory == _handlerFactory);
        Assert.AreSame(_owner.gameObject, handler.source);
    }

    [Test]
    public void WhenTagged_LosingTheTag_KeepsTheBuffForItsOwnDuration()
    {
        AddBuff(TagTrigger.WhenTagged);
        _owner.AddTag(_blessed);
        _buffManager.ForceUpdate();

        _owner.RemoveTag(_blessed);

        Assert.IsTrue(HasBuff());
    }

    [Test]
    public void WhenTagged_AlreadyTaggedWhenAdded_GivesNothing()
    {
        _owner.AddTag(_blessed);

        AddBuff(TagTrigger.WhenTagged);

        Assert.IsFalse(HasBuff());
    }

    [Test]
    public void WhileTagged_LastsAsLongAsTheTag()
    {
        AddBuff(TagTrigger.WhileTagged);

        _owner.AddTag(_blessed);
        Assert.IsTrue(HasBuff());

        _owner.RemoveTag(_blessed);
        Assert.IsFalse(HasBuff());
    }

    [Test]
    public void WhileTagged_AlreadyTaggedWhenAdded_GivesTheBuff()
    {
        _owner.AddTag(_blessed);

        AddBuff(TagTrigger.WhileTagged);

        Assert.IsTrue(HasBuff());
    }

    [Test]
    public void WhileTagged_Removed_TakesTheBuffBack()
    {
        AddBuff(TagTrigger.WhileTagged);
        _owner.AddTag(_blessed);
        _buffManager.ForceUpdate();

        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Assert.IsFalse(HasBuff());
    }

    [Test]
    public void WhileTagged_ADescendantOfTheTag_CountsToo()
    {
        GameplayTag greatBlessing = CreateTag("Great Blessing", _blessed);
        AddBuff(TagTrigger.WhileTagged);

        _owner.AddTag(greatBlessing);

        Assert.IsTrue(HasBuff());
    }

    [Test]
    public void AnotherTag_GivesNothing()
    {
        AddBuff(TagTrigger.WhenTagged);

        _owner.AddTag(CreateTag("Taunt"));

        Assert.IsFalse(HasBuff());
    }

    [Test]
    public void EachApplication_EveryBlessing_GivesTheBuffAgain_EvenWhileAlreadyBlessed()
    {
        BuffHandlerFactory blessing = CreateBlessing(_blessed);
        AddBuff(TagTrigger.EachApplication, 3);

        Bless(blessing);
        Assert.AreEqual(1, GetStacks());
        // Still blessed: the tag doesn't change, the blessing is refreshed
        Bless(blessing);
        Bless(blessing);

        Assert.AreEqual(3, GetStacks());
    }

    [Test]
    public void EachApplication_ABuffWithoutTheTag_GivesNothing()
    {
        BuffHandlerFactory taunt = CreateBlessing(CreateTag("Taunt"));
        AddBuff(TagTrigger.EachApplication);

        Bless(taunt);

        Assert.IsFalse(HasBuff());
    }

    [Test]
    public void EachApplication_TheTagGivenAnotherWay_GivesNothing()
    {
        AddBuff(TagTrigger.EachApplication);

        _owner.AddTag(_blessed);

        Assert.IsFalse(HasBuff());
    }

    [Test]
    public void EachApplication_ItsOwnBuffCarryingTheTag_IsGivenOnce()
    {
        BuffHandlerFactory blessing = CreateBlessing(_blessed);
        _handlerFactory = CreateBlessing(_blessed);
        _buff = new ApplyBuffOnTagBuff { data = new ApplyBuffOnTagBuffData { tag = _blessed, trigger = TagTrigger.EachApplication, buffHandlerFactory = _handlerFactory } };
        _buff.Add(_owner.gameObject, _owner.gameObject);

        Bless(blessing);

        Assert.AreEqual(1, GetStacks());
    }

    // E.g. a reaction to Blessed giving stacks of a status carrying the Blessed tag: given once, not again and again
    [Test]
    public void EachApplication_ItsOwnBuffGivenAsStacksOfAStatus_IsGivenOnce()
    {
        BuffHandlerFactory blessing = CreateBlessing(_blessed);
        _handlerFactory = CreateBlessing(_blessed);
        _handlerFactory.data.maxStacks = 10;
        StatusData status = ScriptableObject.CreateInstance<StatusData>();
        status.tag = _blessed;
        status.buffHandlerFactory = _handlerFactory;
        StatusStacksFactory twoStacks = ScriptableObject.CreateInstance<StatusStacksFactory>();
        twoStacks.data = new StatusStacksData { status = status, stacks = 2 };
        _scriptableObjects.Add(status);
        _scriptableObjects.Add(twoStacks);
        _buff = new ApplyBuffOnTagBuff { data = new ApplyBuffOnTagBuffData { tag = _blessed, trigger = TagTrigger.EachApplication, buffHandlerFactory = twoStacks } };
        _buff.Add(_owner.gameObject, _owner.gameObject);

        Bless(blessing);

        Assert.AreEqual(2, GetStacks());
    }

    [Test]
    public void EachApplication_Removed_GivesNothingAnymore()
    {
        BuffHandlerFactory blessing = CreateBlessing(_blessed);
        AddBuff(TagTrigger.EachApplication);
        _buff.Remove(_owner.gameObject, _owner.gameObject);

        Bless(blessing);

        Assert.IsFalse(HasBuff());
    }
}

}

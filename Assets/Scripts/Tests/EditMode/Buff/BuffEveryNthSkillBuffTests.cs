using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Rosary: every fifth skill of the character is used with a buff (the spell free), given once four are used
public class BuffEveryNthSkillBuffTests
{
    GameObject _characterGo;
    Character _character;
    BuffManager _buffManager;
    BuffHandlerFactory _freeSpell;
    BuffEveryNthSkillBuff _buff;
    readonly CharacterSkillData _skill = new CharacterSkillData { name = "Heal" };

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its event is used
        TestHelpers.WithLoggingDisabled(() => _character = _characterGo.AddComponent<Character>());
        _buffManager = _characterGo.GetComponent<BuffManager>() ?? _characterGo.AddComponent<BuffManager>();
        _buffManager.isEnabled = true;

        _freeSpell = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _freeSpell.data = new BuffHandlerData { durationType = DurationType.Infinite, maxStacks = 1, buffFactoryList = new List<ABuffFactory>(), tags = new List<GameplayTag>() };
        _buff = new BuffEveryNthSkillBuff { data = new BuffEveryNthSkillBuffData { count = 5, buffHandlerFactory = _freeSpell } };
        _buff.Add(_characterGo, _characterGo);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_freeSpell);
    }

    void UseSkills(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _character.OnSkillUsed.Invoke(_skill);
            _buffManager.ForceUpdate();
        }
    }

    bool HasTheBuff()
    {
        _buffManager.ForceUpdate();
        return _buffManager.GetActiveHandlers().Exists(handler => handler.buffHandlerFactory == _freeSpell);
    }

    [Test]
    public void ThreeSkillsUsed_NoBuffYet()
    {
        UseSkills(3);

        Assert.IsFalse(HasTheBuff());
        Assert.AreEqual(3, _buff.used);
    }

    // The fifth skill will be used with it
    [Test]
    public void FourSkillsUsed_TheBuffIsGiven_FromTheCharacter()
    {
        UseSkills(4);

        Assert.IsTrue(HasTheBuff());
        Assert.AreSame(_characterGo, _buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _freeSpell).source);
    }

    [Test]
    public void FifthSkillUsed_TheBuffIsTakenBack_AndTheCountStartsAgain()
    {
        UseSkills(5);

        Assert.IsFalse(HasTheBuff());
        Assert.AreEqual(0, _buff.used);
    }

    // Skills 5, 10, 15... used with it
    [Test]
    public void EveryFiveSkills_AgainAndAgain()
    {
        UseSkills(5);
        UseSkills(4);
        Assert.IsTrue(HasTheBuff());

        UseSkills(1);
        Assert.IsFalse(HasTheBuff());
    }

    [Test]
    public void Removed_TakesTheGivenBuffBack_AndCountsNoMore()
    {
        UseSkills(4);

        _buff.Remove(_characterGo, _characterGo);
        Assert.IsFalse(HasTheBuff());

        UseSkills(4);
        Assert.IsFalse(HasTheBuff());
    }

    [Test]
    public void OtherCount_EveryThirdSkill()
    {
        _buff.data.count = 3;

        UseSkills(2);

        Assert.IsTrue(HasTheBuff());
    }
}

}

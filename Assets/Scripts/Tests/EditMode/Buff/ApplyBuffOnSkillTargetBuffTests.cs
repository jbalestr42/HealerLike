using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// Held by the character: each unit its skills are used on gets a buff (e.g. the Cleric blessing its allies)
public class ApplyBuffOnSkillTargetBuffTests
{
    readonly TestUnits _units = new TestUnits();
    readonly List<Object> _scriptableObjects = new List<Object>();
    GameObject _characterGo;
    Character _character;
    Entity _ally;
    Entity _enemy;
    BuffHandlerFactory _handlerFactory;
    ApplyBuffOnSkillTargetBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its event is used
        TestHelpers.WithLoggingDisabled(() => _character = _characterGo.AddComponent<Character>());

        _ally = _units.Create(50f, 100f, "Ally");
        _ally.entityType = Entity.EntityType.Player;
        _ally.GetComponent<BuffManager>().isEnabled = true;
        _enemy = _units.Create(50f, 100f, "Enemy");
        _enemy.entityType = Entity.EntityType.Computer;
        _enemy.GetComponent<BuffManager>().isEnabled = true;

        FakeBuffFactory fakeBuff = ScriptableObject.CreateInstance<FakeBuffFactory>();
        fakeBuff.data = new FakeBuffData();
        _handlerFactory = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        _handlerFactory.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 4f,
            maxStacks = 1,
            buffFactoryList = new List<ABuffFactory> { fakeBuff },
        };
        _scriptableObjects.Add(fakeBuff);
        _scriptableObjects.Add(_handlerFactory);

        _buff = new ApplyBuffOnSkillTargetBuff { data = new ApplyBuffOnSkillTargetBuffData { buffHandlerFactory = _handlerFactory, entityType = Entity.EntityType.Player } };
        _buff.Add(_characterGo, _characterGo);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_characterGo);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    BuffManager.BuffHandlerData GetHandler(Entity entity)
    {
        BuffManager buffManager = entity.GetComponent<BuffManager>();
        buffManager.ForceUpdate();
        return buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == _handlerFactory);
    }

    [Test]
    public void SkillUsedOnAnAlly_GivesItTheBuff_FromTheCharacter()
    {
        _character.OnSkillAppliedOnTarget.Invoke(_ally.gameObject);

        BuffManager.BuffHandlerData handler = GetHandler(_ally);
        Assert.IsNotNull(handler);
        Assert.AreSame(_characterGo, handler.source);
    }

    [Test]
    public void SkillUsedOnAnEnemy_GivesNothing()
    {
        _character.OnSkillAppliedOnTarget.Invoke(_enemy.gameObject);

        Assert.IsNull(GetHandler(_enemy));
    }

    [Test]
    public void AnySide_GivesItToTheEnemiesToo()
    {
        _buff.data.entityType = Entity.EntityType.Any;

        _character.OnSkillAppliedOnTarget.Invoke(_enemy.gameObject);

        Assert.IsNotNull(GetHandler(_enemy));
    }

    [Test]
    public void Removed_SkillsGiveNothingAnymore()
    {
        _buff.Remove(_characterGo, _characterGo);

        _character.OnSkillAppliedOnTarget.Invoke(_ally.gameObject);

        Assert.IsNull(GetHandler(_ally));
    }

    // e.g. the Blessing item: only the Blessed status
    [Test]
    public void WithAStatus_GivesItToo_FromTheCharacter()
    {
        BuffHandlerFactory statusHandler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        statusHandler.data = new BuffHandlerData { durationType = DurationType.Duration, duration = 4f, maxStacks = 1, buffFactoryList = new List<ABuffFactory>() };
        StatusData status = ScriptableObject.CreateInstance<StatusData>();
        status.buffHandlerFactory = statusHandler;
        _scriptableObjects.Add(statusHandler);
        _scriptableObjects.Add(status);
        _buff.data.buffHandlerFactory = null;
        _buff.data.status = status;

        _character.OnSkillAppliedOnTarget.Invoke(_ally.gameObject);

        BuffManager buffManager = _ally.GetComponent<BuffManager>();
        buffManager.ForceUpdate();
        BuffManager.BuffHandlerData given = buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == statusHandler);
        Assert.IsNotNull(given);
        Assert.AreSame(_characterGo, given.source);
        Assert.IsNull(GetHandler(_ally));
    }
}

}

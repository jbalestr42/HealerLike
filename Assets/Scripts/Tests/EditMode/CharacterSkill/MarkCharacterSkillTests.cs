using System.Collections.Generic;
using System.Reflection;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// The Mark spell: the chosen enemy becomes the one marked, shown on it, until another one is marked
public class MarkCharacterSkillTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _managerGo;
    MarkManager _markManager;
    Entity _enemy;
    Entity _otherEnemy;
    readonly List<bool> _enemyMarks = new List<bool>();

    [SetUp]
    public void SetUp()
    {
        // Not the singleton instance: Start() isn't called in EditMode, nothing looks for it
        _managerGo = new GameObject("MarkManager");
        _markManager = _managerGo.AddComponent<MarkManager>();
        _enemy = _units.Create(100f, 100f, "Enemy");
        _otherEnemy = _units.Create(100f, 100f, "Other Enemy");
        _enemy.OnMarkChanged.AddListener(_enemyMarks.Add);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_managerGo);
        _enemyMarks.Clear();
    }

    [Test]
    public void Mark_TheEnemyBecomesTheMarkedOne_ShownOnIt()
    {
        MarkCharacterSkill.Mark(_markManager, _enemy.gameObject);

        Assert.AreSame(_enemy.gameObject, _markManager.markedEntity);
        Assert.IsTrue(_markManager.IsEntityMarked(_enemy.gameObject));
        CollectionAssert.AreEqual(new[] { true }, _enemyMarks);
    }

    [Test]
    public void MarkAnother_TheFirstOneIsNoLongerMarked()
    {
        MarkCharacterSkill.Mark(_markManager, _enemy.gameObject);

        MarkCharacterSkill.Mark(_markManager, _otherEnemy.gameObject);

        Assert.AreSame(_otherEnemy.gameObject, _markManager.markedEntity);
        Assert.IsFalse(_markManager.IsEntityMarked(_enemy.gameObject));
        CollectionAssert.AreEqual(new[] { true, false }, _enemyMarks);
    }

    [Test]
    public void TheMarkedKilled_NoMoreMarked()
    {
        MarkCharacterSkill.Mark(_markManager, _enemy.gameObject);

        typeof(MarkManager).GetMethod("OnEntityKilled", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_markManager, new object[] { _enemy });

        Assert.IsNull(_markManager.markedEntity);
    }

    [Test]
    public void Mark_WithoutTargetOrManager_DoesNothing()
    {
        Assert.DoesNotThrow(() => MarkCharacterSkill.Mark(_markManager, null));
        Assert.DoesNotThrow(() => MarkCharacterSkill.Mark(null, _enemy.gameObject));
        Assert.IsNull(_markManager.markedEntity);
    }
}

}

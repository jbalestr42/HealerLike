using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CharacterSkills
{

// The skills applied on each target (a single one or a whole side) tell the character about it
public class BaseCharacterSkillTests
{
    GameObject _characterGo;
    Character _character;
    GameObject _target;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its event is used
        TestHelpers.WithLoggingDisabled(() => _character = _characterGo.AddComponent<Character>());
        _target = new GameObject("Target");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_target);
    }

    [Test]
    public void ApplyOnTarget_TellsTheCharacterWhichUnitTheSkillWasUsedOn()
    {
        List<GameObject> targets = new List<GameObject>();
        _character.OnSkillAppliedOnTarget.AddListener(targets.Add);
        BuffCharacterSkill skill = new BuffCharacterSkill { data = new BuffCharacterSkillData { buffHandlerFactory = new List<ABuffHandlerFactory>() } };

        skill.ApplyOnTarget(_characterGo, _target);

        CollectionAssert.AreEqual(new[] { _target }, targets);
    }

    [Test]
    public void ApplyOnTarget_NoCharacterAsSource_StillAppliesTheSkill()
    {
        BuffCharacterSkill skill = new BuffCharacterSkill { data = new BuffCharacterSkillData { buffHandlerFactory = new List<ABuffHandlerFactory>() } };

        Assert.DoesNotThrow(() => skill.ApplyOnTarget(_target, _target));
    }
}

}

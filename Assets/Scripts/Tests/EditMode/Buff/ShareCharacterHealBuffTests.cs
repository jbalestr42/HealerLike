using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;

namespace Buff
{

// The allies and the grid come from the EntityManager and PlayerBehaviour singletons in the game
public class TestShareCharacterHealBuff : ShareCharacterHealBuff
{
    public List<GameObject> allies = new List<GameObject>();
    public float cellSize = 2f;

    protected override List<GameObject> GetAllies() => allies;
    protected override float GetCellSize() => cellSize;
}

// Beacon: each heal of the character on an ally is copied, in part, on the most wounded ally around the holder
public class ShareCharacterHealBuffTests
{
    const float CellSize = 2f;

    readonly TestUnits _units = new TestUnits();
    GameObject _character;
    Entity _beacon;
    Entity _healed;
    Entity _wounded;
    Entity _lessWounded;
    Entity _far;
    TestShareCharacterHealBuff _buff;

    [SetUp]
    public void SetUp()
    {
        _character = new GameObject("Character");
        // Adding Character triggers its editor-only Reset(), which NREs without Init(): only its presence counts
        TestHelpers.WithLoggingDisabled(() => _character.AddComponent<Character>());

        _beacon = CreateAt("Beacon", 0, 0, 100f);
        _healed = CreateAt("Healed", 1, 0, 20f);
        _wounded = CreateAt("Wounded", 0, 1, 40f);
        _lessWounded = CreateAt("Less Wounded", -1, 0, 80f);
        _far = CreateAt("Far", 3, 0, 10f);

        _buff = new TestShareCharacterHealBuff { data = new ShareCharacterHealBuffData { ratio = 0.4f, pattern = RelativeCellPatternType.Adjacent, range = 1 }, cellSize = CellSize };
        _buff.allies = new List<GameObject> { _beacon.gameObject, _healed.gameObject, _wounded.gameObject, _lessWounded.gameObject, _far.gameObject };
        _buff.Add(_beacon.gameObject, _beacon.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_character);
    }

    // A unit on the cell (x, y) of the grid
    Entity CreateAt(string name, int x, int y, float health)
    {
        Entity entity = _units.Create(health, 100f, name);
        entity.transform.position = new Vector3(x * CellSize, 0f, y * CellSize);
        return entity;
    }

    static void Heal(GameObject source, Entity target, float amount)
    {
        Entity.NotifyHealed(source, target.gameObject, new ConsumerResult(amount, false));
    }

    [Test]
    public void CharacterHeal_OnAnAlly_HealsTheMostWoundedAllyNextToTheHolderFor40Percent()
    {
        Heal(_character, _healed, 20f);
        TestUnits.Process(_wounded.health);

        Assert.AreEqual(48f, _wounded.health.Value, 0.0001f);
        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_lessWounded));
    }

    // The healed ally, the most wounded one and next to the Beacon, is left out: the copy spreads the heal
    [Test]
    public void CharacterHeal_TheHealedAllyIsLeftOut()
    {
        Heal(_character, _healed, 20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_healed));
    }

    [Test]
    public void CharacterHeal_AnAllyAwayFromTheHolderIsNeverHealed()
    {
        Heal(_character, _wounded, 20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_far));
    }

    // The holder can be healed or be the copy target, it is around itself
    [Test]
    public void CharacterHeal_TheCopyComesFromTheHolder()
    {
        Heal(_character, _healed, 20f);

        Assert.AreSame(_beacon.gameObject, TestUnits.GetPendingModifiers(_wounded)[0].source);
    }

    [Test]
    public void HealOfAUnit_IsNotCopied()
    {
        Heal(_lessWounded.gameObject, _healed, 20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_wounded));
    }

    // The copies come from the holder, not the character: never copied again
    [Test]
    public void TheCopy_IsNotCopiedAgain()
    {
        Heal(_beacon.gameObject, _wounded, 8f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_lessWounded));
    }

    [Test]
    public void CharacterHeal_AfterRemove_IsNotCopied()
    {
        _buff.Remove(_beacon.gameObject, _beacon.gameObject);

        Heal(_character, _healed, 20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_wounded));
    }

    [Test]
    public void CharacterHeal_OnAnEnemy_IsNotCopied()
    {
        Entity enemy = CreateAt("Enemy", 0, -1, 10f);
        enemy.entityType = Entity.EntityType.Computer;
        _buff.allies.Add(enemy.gameObject);
        _buff.Listen(enemy);

        Heal(_character, enemy, 20f);

        CollectionAssert.IsEmpty(TestUnits.GetPendingModifiers(_wounded));
    }
}

}

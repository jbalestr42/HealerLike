using Entities;
using NUnit.Framework;

namespace Buff
{

// Counts its repeats without shooting (projectiles need the EntityManager singleton)
public class CountingProjectileAttack : ProjectileAttack
{
    public int repeats;

    public override void Repeat()
    {
        repeats++;
    }
}

// Echo: every Nth attack of the owner is repeated
public class EchoAttackBuffTests
{
    readonly TestUnits _units = new TestUnits();
    Entity _owner;
    CountingProjectileAttack _attack;

    [SetUp]
    public void SetUp()
    {
        _owner = _units.Create(100f, 100f, "Owner");
        _attack = new CountingProjectileAttack { source = _owner.gameObject };
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
    }

    EchoAttackBuff AddBuff(float delay)
    {
        EchoAttackBuff buff = new EchoAttackBuff { data = new EchoAttackBuffData { attackCount = 4, delay = delay } };
        buff.Add(_owner.gameObject, _owner.gameObject);
        return buff;
    }

    void Attack(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _owner.OnAttack.Invoke(_attack);
        }
    }

    [Test]
    public void Attacks_BeforeTheCount_AreNotRepeated()
    {
        AddBuff(0f);

        Attack(3);

        Assert.AreEqual(0, _attack.repeats);
    }

    [Test]
    public void Attack_ReachingTheCount_IsRepeatedOnce()
    {
        AddBuff(0f);

        Attack(4);

        Assert.AreEqual(1, _attack.repeats);
    }

    [Test]
    public void Attacks_TheCountStartsOverAfterEachRepeat()
    {
        AddBuff(0f);

        Attack(8);

        Assert.AreEqual(2, _attack.repeats);
    }

    [Test]
    public void Attack_WithADelay_IsNotRepeatedRightAway()
    {
        AddBuff(0.2f);

        Attack(4);

        Assert.AreEqual(0, _attack.repeats);
    }

    [Test]
    public void Attacks_AfterRemove_AreNeverRepeated()
    {
        EchoAttackBuff buff = AddBuff(0f);
        buff.Remove(_owner.gameObject, _owner.gameObject);

        Attack(4);

        Assert.AreEqual(0, _attack.repeats);
    }
}

}

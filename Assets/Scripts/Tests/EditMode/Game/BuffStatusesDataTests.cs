using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static Game.StatusesTestData;

namespace Game
{

// The positive buffs are statuses with a fixed effect, a giver choosing only how many stacks it applies (user decision
// 2026-10-17): Invulnerable, Hasted, Empowered, Fortified and Regenerating
public class BuffStatusesDataTests
{
    const string Data = "Assets/Data/";

    readonly Entities.TestUnits _units = new Entities.TestUnits();
    GameObject _source;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Giver");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        Object.DestroyImmediate(_source);
    }

    static T LoadAsset<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(Data + path + ".asset");
        Assert.IsNotNull(asset, path);
        return asset;
    }

    // A unit with the attribute, given the status the number of times
    float ValueAfter(string statusName, AttributeType type, float baseValue, int applications)
    {
        Entity unit = _units.Create(100f, 100f);
        BuffManager buffManager = unit.GetComponent<BuffManager>();
        buffManager.isEnabled = true;
        AttributeManager attributes = unit.GetComponent<AttributeManager>();
        attributes.Add(type, new Attribute(baseValue));
        StatusData status = LoadStatus(statusName);
        for (int i = 0; i < applications; i++)
        {
            unit.ApplyStatus(status, _source);
        }
        buffManager.ForceUpdate();
        Attribute attribute = attributes.Get(type);
        attribute.Update();
        return attribute.Value;
    }

    // Every status: its title, a description, its own tag given by its buff, never a debuff (the Purifier keeps it),
    // an icon, stacking on the unit whoever gives it, gone at the end of the fight
    [TestCase("Invulnerable")]
    [TestCase("Hasted")]
    [TestCase("Empowered")]
    [TestCase("Fortified")]
    [TestCase("Regenerating")]
    public void Status_ABuffWithItsTagAndIcon_SharedByEverySource(string name)
    {
        StatusData status = LoadStatus(name);

        Assert.AreEqual(name, status.title);
        Assert.IsFalse(string.IsNullOrEmpty(status.description));
        Assert.AreEqual(name, status.tag.name);
        Assert.IsTrue(status.GivesItsTag());
        ABuffHandlerFactory buff = status.buffHandlerFactory;
        Assert.IsNotNull(buff.icon);
        Assert.IsTrue(buff.stackAcrossSources);
        Assert.AreEqual(DurationType.Duration, buff.durationType);
        Assert.IsFalse(buff.HasTag(TagNames.Debuff));
        Assert.IsFalse(buff.HasTag(TagNames.Permanent));
    }

    [TestCase("Invulnerable", 2f, 1)]
    [TestCase("Hasted", 5f, 8)]
    [TestCase("Empowered", 5f, 10)]
    [TestCase("Fortified", 6f, 3)]
    [TestCase("Regenerating", 4f, 10)]
    public void Status_ItsDurationAndMaxStacks(string name, float duration, int maxStacks)
    {
        ABuffHandlerFactory buff = LoadStatus(name).buffHandlerFactory;

        Assert.AreEqual(duration, buff.duration, 0.0001f);
        Assert.AreEqual(maxStacks, buff.maxStacks);
    }

    [Test]
    public void Invulnerable_NoDamageTaken()
    {
        Entity unit = _units.Create(100f, 100f);
        BuffManager buffManager = unit.GetComponent<BuffManager>();
        buffManager.isEnabled = true;

        unit.ApplyStatus(LoadStatus("Invulnerable"), _source);
        buffManager.ForceUpdate();

        Assert.IsTrue(unit.health.preventConsumers);
    }

    // AttackRate is the time between two attacks: +25% per stack, up to +200% (three times as many attacks)
    [TestCase(1, 1f / 1.25f)]
    [TestCase(4, 1f / 2f)]
    [TestCase(8, 1f / 3f)]
    [TestCase(10, 1f / 3f)]
    public void Hasted_25PercentAttackSpeedPerStack_UpTo200(int applications, float attackRate)
    {
        Assert.AreEqual(attackRate, ValueAfter("Hasted", AttributeType.AttackRate, 1f, applications), 0.0001f);
    }

    [TestCase(1, 6f)]
    [TestCase(3, 8f)]
    [TestCase(12, 15f)]
    public void Empowered_OneDamagePerStack_UpTo10(int applications, float damage)
    {
        Assert.AreEqual(damage, ValueAfter("Empowered", AttributeType.Damage, 5f, applications), 0.0001f);
    }

    [TestCase(1, 1f)]
    [TestCase(2, 2f)]
    [TestCase(5, 3f)]
    public void Fortified_OneArmorPerStack_UpTo3(int applications, float armor)
    {
        Assert.AreEqual(armor, ValueAfter("Fortified", AttributeType.FlatArmor, 0f, applications), 0.0001f);
    }

    [Test]
    public void Regenerating_HealsOneHpPerStackEverySecond()
    {
        BuffHandlerFactory buff = LoadStatus("Regenerating").buffHandlerFactory as BuffHandlerFactory;

        Assert.IsTrue(buff.data.isPeriodic);
        Assert.AreEqual(1f, buff.data.periodDuration, 0.0001f);
        ApplyConsumerBuffFactory heal = buff.buffFactoryList.OfType<ApplyConsumerBuffFactory>().Single();
        ConsumerFactory consumer = heal.data.consumerFactory as ConsumerFactory;
        Assert.IsNotNull(consumer);
        // A negative change of the health: a heal
        Assert.AreEqual(-1f, (consumer.data.value as FlatValue).data.value, 0.0001f);
    }

    [Test]
    public void Invulnerable_GivenByTheGuardian_Halo_SecondWind()
    {
        ApplyBuffOnTargetSkillFactory guardian = LoadAsset<ApplyBuffOnTargetSkillFactory>("Entities/GuardianEntity/ApplyBuffOnTargetSkillFactory");
        AssertStacks(guardian.data.buffHandlerFactory, "Invulnerable", 1);
        // Only on the allies not Invulnerable yet
        BuffHandlerValidatorFactory notYet = LoadAsset<BuffHandlerValidatorFactory>("Entities/GuardianEntity/BuffHandlerValidatorFactory");
        Assert.AreSame(guardian.data.buffHandlerFactory, notYet.data.buffHandlerFactory);

        AssertStacks(LoadAsset<ReviveOnDeathBuffFactory>("EntityItems/HaloItem/ReviveOnDeathBuffFactory").data.buffHandlerFactory, "Invulnerable", 1);
        AssertStacks(LoadAsset<ApplyBuffBelowHealthBuffFactory>("EntityItems/SecondWindItem/ApplyBuffBelowHealthBuffFactory").data.buffHandlerFactory, "Invulnerable", 1);
    }

    [Test]
    public void Hasted_GivenByTheWarDrum_2_OnlyToTheAlliesNotHastedYet()
    {
        ApplyBuffOnTargetSkillFactory drum = LoadAsset<ApplyBuffOnTargetSkillFactory>("Entities/WarDrumEntity/ApplyBuffOnTargetSkillFactory");
        AssertStacks(drum.data.buffHandlerFactory, "Hasted", 2);
        BuffHandlerValidatorFactory notYet = LoadAsset<BuffHandlerValidatorFactory>("Entities/WarDrumEntity/BuffHandlerValidatorFactory");
        Assert.AreSame(drum.data.buffHandlerFactory, notYet.data.buffHandlerFactory);
        Assert.IsFalse(notYet.data.mustHave);
    }
}

}

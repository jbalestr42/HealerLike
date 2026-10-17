using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// The debuffs are statuses with a fixed effect, a giver choosing only how many stacks it applies (user decision
// 2026-10-17): Vulnerable, Poisoned, Chilled, Blighted and Sundered
public class DebuffStatusesDataTests
{
    const string Data = "Assets/Data/";

    static StatusData LoadStatus(string name)
    {
        StatusData status = AssetDatabase.LoadAssetAtPath<StatusData>($"{Data}Statuses/{name}Status/{name}Status.asset");
        Assert.IsNotNull(status, name);
        return status;
    }

    static T LoadAsset<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(Data + path + ".asset");
        Assert.IsNotNull(asset, path);
        return asset;
    }

    static void AssertStacks(ABuffHandlerFactory effect, string statusName, int stacks)
    {
        StatusStacksFactory statusStacks = effect as StatusStacksFactory;
        Assert.IsNotNull(statusStacks, $"stacks of {statusName}");
        Assert.AreSame(LoadStatus(statusName), statusStacks.data.status);
        Assert.AreEqual(stacks, statusStacks.data.stacks, statusName);
    }

    static void AssertOnHitStacks(string itemPath, string statusName, int stacks)
    {
        ItemFactory item = LoadAsset<ItemFactory>(itemPath);
        AssertStacks(item.data.onHitEffects.Single(), statusName, stacks);
    }

    // Every status: its title, a description, its own tag given by its buff, a debuff cleansed by the Purifier, an
    // icon, stacking on the unit whoever gives it, gone at the end of the fight
    [TestCase("Vulnerable")]
    [TestCase("Poisoned")]
    [TestCase("Chilled")]
    [TestCase("Blighted")]
    [TestCase("Sundered")]
    public void Status_ADebuffWithItsTagAndIcon_SharedByEverySource(string name)
    {
        StatusData status = LoadStatus(name);

        Assert.AreEqual(name, status.title);
        Assert.IsFalse(string.IsNullOrEmpty(status.description));
        Assert.AreEqual(name, status.tag.name);
        Assert.IsTrue(status.GivesItsTag());
        ABuffHandlerFactory buff = status.buffHandlerFactory;
        Assert.IsNotNull(buff.icon);
        Assert.IsTrue(buff.stackAcrossSources);
        Assert.IsTrue(buff.HasTag(TagNames.Debuff));
        Assert.IsFalse(buff.HasTag(TagNames.Permanent));
    }

    [Test]
    public void Poisoned_OneDamagePerStackEverySecond_IgnoringArmor_6s_UpTo20()
    {
        BuffHandlerFactory buff = LoadStatus("Poisoned").buffHandlerFactory as BuffHandlerFactory;

        Assert.AreEqual(DurationType.Duration, buff.durationType);
        Assert.AreEqual(6f, buff.duration, 0.0001f);
        Assert.IsTrue(buff.data.isPeriodic);
        Assert.AreEqual(1f, buff.data.periodDuration, 0.0001f);
        Assert.AreEqual(20, buff.maxStacks);
        ApplyConsumerBuffFactory poison = buff.buffFactoryList.OfType<ApplyConsumerBuffFactory>().Single();
        ConsumerFactory damage = poison.data.consumerFactory as ConsumerFactory;
        Assert.IsNotNull(damage);
        Assert.IsTrue(damage.data.ignoreDamageReduction);
        Assert.AreEqual(1f, (damage.data.value as FlatValue).data.value, 0.0001f);
    }

    [Test]
    public void Chilled_Attacks50PercentSlower_3s_NoStacking()
    {
        ABuffHandlerFactory buff = LoadStatus("Chilled").buffHandlerFactory;

        Assert.AreEqual(DurationType.Duration, buff.durationType);
        Assert.AreEqual(3f, buff.duration, 0.0001f);
        Assert.AreEqual(1, buff.maxStacks);
        SlowModifierFactory slow = buff.buffFactoryList.OfType<SlowModifierFactory>().Single();
        Assert.AreEqual(AttributeType.AttackRate, slow.data.type);
        Assert.AreEqual(0.5f, slow.data.value, 0.0001f);
    }

    // Hit again while chilled: the slow is back at its full value for 3 more seconds, not still fading from the first hit
    [Test]
    public void Chilled_HitAgain_TheSlowStartsOverAtFull()
    {
        Entities.TestUnits units = new Entities.TestUnits();
        GameObject source = new GameObject("Frost");
        try
        {
            Entity unit = units.Create(100f, 100f);
            BuffManager buffManager = unit.GetComponent<BuffManager>();
            buffManager.isEnabled = true;
            AttributeManager attributes = unit.GetComponent<AttributeManager>();
            attributes.Add(AttributeType.AttackRate, new Attribute(1f));
            StatusData chilled = LoadStatus("Chilled");

            unit.ApplyStatus(chilled, source);
            buffManager.ForceUpdate();
            float fullSlow = AttackRate(attributes);
            Assert.Greater(fullSlow, 1f, "slowed: more time between two attacks");
            BuffManager.BuffHandlerData given = buffManager.GetActiveHandlers().Find(handler => handler.buffHandlerFactory == chilled.buffHandlerFactory);
            ((BuffHandler)given.buffHandler).durationTimer = 2f;
            Assert.Less(AttackRate(attributes), fullSlow, "fading");

            unit.ApplyStatus(chilled, source);
            buffManager.ForceUpdate();

            Assert.AreEqual(fullSlow, AttackRate(attributes), 0.01f);
            Assert.AreEqual(1, given.currentStacks);
        }
        finally
        {
            units.DestroyAll();
            Object.DestroyImmediate(source);
        }
    }

    static float AttackRate(AttributeManager attributes)
    {
        Attribute attackRate = attributes.Get(AttributeType.AttackRate);
        attackRate.Update();
        return attackRate.Value;
    }

    [Test]
    public void Blighted_HalfTheHealingReceived_4s_NoStacking()
    {
        ABuffHandlerFactory buff = LoadStatus("Blighted").buffHandlerFactory;

        Assert.AreEqual(DurationType.Duration, buff.durationType);
        Assert.AreEqual(4f, buff.duration, 0.0001f);
        Assert.AreEqual(1, buff.maxStacks);
        FlatModifierFactory healing = buff.buffFactoryList.OfType<FlatModifierFactory>().Single();
        Assert.AreEqual(AttributeType.HealingReceived, healing.data.type);
        Assert.AreEqual(-0.5f, healing.data.value, 0.0001f);
    }

    [Test]
    public void Sundered_MinusOneArmorPerStack_UntilTheEndOfTheFight()
    {
        ABuffHandlerFactory buff = LoadStatus("Sundered").buffHandlerFactory;

        Assert.AreEqual(DurationType.Infinite, buff.durationType);
        Assert.AreEqual(0, buff.maxStacks, "no limit");
        FlatModifierFactory armor = buff.buffFactoryList.OfType<FlatModifierFactory>().Single();
        Assert.AreEqual(AttributeType.FlatArmor, armor.data.type);
        Assert.AreEqual(-1f, armor.data.value, 0.0001f);
    }

    [Test]
    public void Vulnerable_GivenByTheRavager1_TheHexAcolyte1_TheHexer2_HuntersMark1()
    {
        ShootProjectileSkillFactory hexAcolyte = LoadAsset<ShootProjectileSkillFactory>("Entities/HexAcolyteEntity/ShootProjectileSkillFactory");
        AssertStacks(hexAcolyte.data.projectiles.Single().onHitEffects.Single(), "Vulnerable", 1);
        ShootProjectileSkillStepFactory ravager = LoadAsset<ShootProjectileSkillStepFactory>("Entities/RavagerEntity/StrikeSkillStepFactory");
        AssertStacks(ravager.data.projectiles.Single().onHitEffects.Single(), "Vulnerable", 1);
        AssertOnHitStacks("EntityItems/HuntersMarkItem/HuntersMarkItem", "Vulnerable", 1);

        ApplyBuffOnTargetSkillFactory hexer = LoadAsset<ApplyBuffOnTargetSkillFactory>("Entities/HexerEntity/ApplyBuffOnTargetSkillFactory");
        AssertStacks(hexer.data.buffHandlerFactory, "Vulnerable", 2);
        // Only on the units not Vulnerable yet
        BuffHandlerValidatorFactory notYet = LoadAsset<BuffHandlerValidatorFactory>("Entities/HexerEntity/BuffHandlerValidatorFactory");
        Assert.AreSame(hexer.data.buffHandlerFactory, notYet.data.buffHandlerFactory);
        Assert.IsFalse(notYet.data.mustHave);
    }

    [Test]
    public void Poisoned_GivenByVenom1_Poison5_Curse5()
    {
        AssertOnHitStacks("EntityItems/VenomItem/VenomItem", "Poisoned", 1);
        AssertOnHitStacks("EntityItems/PoisonItem/PoisonItem", "Poisoned", 5);
        BuffCharacterSkillFactory curse = LoadAsset<BuffCharacterSkillFactory>("CharacterSkills/Curse/Curse");
        AssertStacks(curse.data.buffHandlerFactory.Single(), "Poisoned", 5);
    }

    [Test]
    public void Chilled_GivenByFrostAndSlow_Blighted_ByPlague_Sundered_ByArmorBreaker()
    {
        AssertOnHitStacks("EntityItems/FrostItem/FrostItem", "Chilled", 1);
        AssertOnHitStacks("EntityItems/SlowItem/SlowItem", "Chilled", 1);
        AssertOnHitStacks("EntityItems/PlagueItem/PlagueItem", "Blighted", 1);
        AssertOnHitStacks("EntityItems/ArmorBreakerItem/ArmorBreakerItem", "Sundered", 1);
    }
}

}

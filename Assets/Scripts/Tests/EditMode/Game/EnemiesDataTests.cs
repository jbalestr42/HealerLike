using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Game
{

// Checks the enemies of the project data and the waves fielding them
public class EnemiesDataTests
{
    const string EntitiesPath = "Assets/Data/Entities/";
    const string WavesPath = "Assets/Data/WavePatterns/";

    static EntityData LoadEnemy(string name)
    {
        EntityData enemy = AssetDatabase.LoadAssetAtPath<EntityData>(EntitiesPath + name + "Entity/" + name + "Entity.asset");
        Assert.IsNotNull(enemy, name);
        return enemy;
    }

    static WavePatternData LoadWave(string name)
    {
        WavePatternData wave = AssetDatabase.LoadAssetAtPath<WavePatternData>(WavesPath + name + ".asset");
        Assert.IsNotNull(wave, name);
        return wave;
    }

    // Titles of the enemies of a rank of the wave, 0 being the front one, empty slots left out
    static List<string> GetRank(WavePatternData wave, int rank)
    {
        List<string> titles = new List<string>();
        for (int i = 0; i < wave.slots.GetLength(1); i++)
        {
            EntityData entity = wave.slots[rank, i].entity;
            if (entity != null)
            {
                titles.Add(entity.title);
            }
        }
        return titles;
    }

    // Damage of a flat consumer
    static float GetFlatDamage(AConsumerFactory consumer)
    {
        ConsumerFactory factory = consumer as ConsumerFactory;
        Assert.IsNotNull(factory);
        FlatValue value = factory.data.value as FlatValue;
        Assert.IsNotNull(value);
        return value.GetValue(null);
    }

    // The buff of the single always-on buff handler of the single item of the enemy
    static T GetItemBuff<T>(EntityData enemy) where T : ABuffFactory
    {
        Assert.AreEqual(1, enemy.items.Count, enemy.title);
        ItemFactory item = enemy.items[0] as ItemFactory;
        Assert.IsNotNull(item, enemy.title);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.name), enemy.title);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.description), enemy.title);
        ABuffHandlerFactory handler = item.data.buffs[0];
        Assert.AreEqual(DurationType.Infinite, handler.durationType, enemy.title);
        T buff = handler.buffFactoryList[0] as T;
        Assert.IsNotNull(buff, enemy.title);
        return buff;
    }

    [Test]
    public void Kamikaze_HasNoFuse()
    {
        EntityData kamikaze = LoadEnemy("Kamikaze");

        Assert.IsFalse(kamikaze.skillFactories.Exists(factory => factory is FuseSkillFactory));
    }

    [Test]
    public void LitKamikaze_BlowsUpByItselfAfter10To13sFor20DamageInAll()
    {
        EntityData kamikaze = LoadEnemy("LitKamikaze");
        FuseSkillFactory fuse = (FuseSkillFactory)kamikaze.skillFactories.Find(factory => factory is FuseSkillFactory);
        Assert.IsNotNull(fuse, "the Lit Kamikaze has no fuse");
        DamageAllEntityOnEntityDieBuffFactory selfDestruct = GetItemBuff<DamageAllEntityOnEntityDieBuffFactory>(kamikaze);

        Assert.AreEqual("Lit Kamikaze", kamikaze.title);
        Assert.AreEqual(10f, fuse.data.minDelay, 0.0001f);
        Assert.AreEqual(13f, fuse.data.maxDelay, 0.0001f);
        // Its death, caused by the fuse, also triggers its Self-Destruct
        Assert.AreEqual(12f, GetFlatDamage(selfDestruct.data.damageToAllEntity), 0.0001f);
        Assert.AreEqual(20f, GetFlatDamage(fuse.data.explosion) + GetFlatDamage(selfDestruct.data.damageToAllEntity), 0.0001f);
        StringAssert.Contains("20", kamikaze.description);
    }

    [Test]
    public void VengefulSniper_ShootsLikeTheSniper()
    {
        EntityData sniper = LoadEnemy("Sniper");
        EntityData vengeful = LoadEnemy("VengefulSniper");

        Assert.AreEqual("Vengeful Sniper", vengeful.title);
        Assert.IsFalse(string.IsNullOrEmpty(vengeful.description));
        Assert.IsNotNull(vengeful.model);
        Assert.AreEqual(sniper.targetBehaviourType, vengeful.targetBehaviourType);
        CollectionAssert.AreEquivalent(sniper.attributes, vengeful.attributes);
        Assert.AreEqual(1, vengeful.skillFactories.Count, "it attacks");
    }

    [Test]
    public void VengefulSniper_EachAllyDeathGivesPlus100PercentAttackSpeedFor6s_UpTo200()
    {
        ABuffHandlerFactory vengeance = GetItemBuff<ApplyBuffOnAllyDeathBuffFactory>(LoadEnemy("VengefulSniper")).data.buffHandlerFactory;

        Assert.AreEqual(DurationType.Duration, vengeance.durationType);
        Assert.AreEqual(6f, vengeance.duration, 0.0001f);
        // A second death stacks it, a third one only refreshes it
        Assert.AreEqual(2, vengeance.maxStacks);
        Assert.AreEqual(1, vengeance.buffFactoryList.Count);
        AttackSpeedModifierFactory modifier = vengeance.buffFactoryList[0] as AttackSpeedModifierFactory;
        Assert.IsNotNull(modifier);
        Assert.AreEqual(AttributeType.AttackRate, modifier.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, modifier.data.modifierType);
        // +100% per stack: twice then three times as many attacks
        Assert.AreEqual(1f, modifier.data.value, 0.0001f);
        Assert.IsEmpty(vengeance.tags, "removed at the end of the fight");
    }

    [Test]
    public void Fuse_FieldsThreeLitKamikazesInFrontAndTwoVengefulSnipersBehind()
    {
        WavePatternData wave = LoadWave("Wave_Fuse");

        CollectionAssert.AreEqual(new[] { "Lit Kamikaze", "Lit Kamikaze", "Lit Kamikaze" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Vengeful Sniper", "Vengeful Sniper" }, GetRank(wave, 1));
    }

    [Test]
    public void ArcMage_LightningChainsThroughFourUnits()
    {
        BounceProjectileBehaviourFactory bounce = AssetDatabase.LoadAssetAtPath<BounceProjectileBehaviourFactory>("Assets/Data/EntityItems/ArcItem/BounceProjectileBehaviourFactory.asset");
        Assert.IsNotNull(bounce);

        // The first target, then 3 jumps
        Assert.AreEqual(3, bounce.data.bounce);
        StringAssert.Contains("4", LoadEnemy("ArcMage").description);
    }

    [Test]
    public void Storm_FieldsThreeSoldiersInFrontAndTheWarDrumBetweenTwoArcMages()
    {
        WavePatternData wave = LoadWave("Wave_Storm");

        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Arc Mage", "War Drum", "Arc Mage" }, GetRank(wave, 1));
    }

    [Test]
    public void Bannerman_GivesPlus2DamageToTheAlliesOnThe8CellsAroundIt()
    {
        EntityData bannerman = LoadEnemy("Bannerman");
        BoostEntitiesOnRelativeCellBuffFactory banner = GetItemBuff<BoostEntitiesOnRelativeCellBuffFactory>(bannerman);

        Assert.AreEqual("Bannerman", bannerman.title);
        Assert.IsFalse(string.IsNullOrEmpty(bannerman.description));
        Assert.IsNotNull(bannerman.model);
        Assert.IsEmpty(bannerman.skillFactories, "it doesn't attack");
        Assert.IsFalse(bannerman.HasTag(TagNames.Reward), "an enemy, never a reward");
        Assert.AreEqual(RelativeCellPatternType.Adjacent, banner.data.pattern);
        Assert.AreEqual(1, banner.data.range);
        FlatModifierFactory modifier = banner.data.buffHandlerFactory.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(modifier);
        Assert.AreEqual(AttributeType.Damage, modifier.data.type);
        Assert.AreEqual(AttributeModifierType.Add, modifier.data.modifierType);
        Assert.AreEqual(2f, modifier.data.value, 0.0001f);
    }

    [Test]
    public void EliteBastion_FieldsTheBannermanBetweenTwoMortarsBehindTheColossusAndTheShamanBehindIt()
    {
        WavePatternData wave = LoadWave("Wave_Elite_Bastion");

        Assert.AreEqual(3, wave.slots.GetLength(0));
        CollectionAssert.AreEqual(new[] { "Hit Armor Buffer", "Colossus" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Mortar", "Bannerman", "Mortar" }, GetRank(wave, 1));
        CollectionAssert.AreEqual(new[] { "Shaman" }, GetRank(wave, 2));
        // In the middle, the Bannerman boosts every unit of the wave
        Assert.AreEqual("Bannerman", wave.slots[1, 1].entity.title);
    }

    [Test]
    public void BloodWarden_GivesPlus50PercentArmorToItsAlliesUnderHalfHealth()
    {
        EntityData warden = LoadEnemy("BloodWarden");
        ApplyBuffToAlliesBuffFactory ward = GetItemBuff<ApplyBuffToAlliesBuffFactory>(warden);

        Assert.AreEqual("Blood Warden", warden.title);
        Assert.IsFalse(string.IsNullOrEmpty(warden.description));
        Assert.IsNotNull(warden.model);
        Assert.IsEmpty(warden.skillFactories, "it doesn't attack");
        Assert.IsFalse(warden.HasTag(TagNames.Reward), "an enemy, never a reward");
        Assert.AreEqual(DurationType.Infinite, ward.data.buffHandlerFactory.durationType);
        HealthThresholdModifierFactory modifier = ward.data.buffHandlerFactory.buffFactoryList[0] as HealthThresholdModifierFactory;
        Assert.IsNotNull(modifier);
        Assert.AreEqual(AttributeType.PercentArmor, modifier.data.type);
        Assert.AreEqual(AttributeModifierType.Add, modifier.data.modifierType);
        Assert.AreEqual(0.5f, modifier.data.value, 0.0001f);
        Assert.AreEqual(0.5f, modifier.data.threshold, 0.0001f);
        Assert.IsTrue(modifier.data.isBelow);
    }

    [Test]
    public void Berserker_Deals5Damage()
    {
        Assert.AreEqual(5f, LoadEnemy("Berserker").attributes[AttributeType.Damage]);
    }

    [Test]
    public void EliteCult_FieldsThreeBerserkersInFrontProtectedByTheGuardianAndTheBloodWarden()
    {
        WavePatternData wave = LoadWave("Wave_Elite_Cult");

        Assert.AreEqual(3, wave.slots.GetLength(0));
        CollectionAssert.AreEqual(new[] { "Berserker", "Berserker", "Berserker" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Hexer", "Guardian", "Hexer" }, GetRank(wave, 1));
        CollectionAssert.AreEqual(new[] { "War Drum", "Blood Warden" }, GetRank(wave, 2));
    }

    [Test]
    public void Necromancer_KeepsUpTo2FrailSkeletonsAtATime()
    {
        EntityData necromancer = LoadEnemy("Necromancer");
        SummonSkillFactory summon = (SummonSkillFactory)necromancer.skillFactories.Find(factory => factory is SummonSkillFactory);
        Assert.IsNotNull(summon, "the Necromancer raises nothing");

        Assert.AreEqual(2, summon.data.maxAlive);
        Assert.AreEqual("Skeleton", summon.data.entity.title);
        Assert.AreEqual(15f, summon.data.entity.attributes[AttributeType.HealthMax]);
        StringAssert.Contains("up to 2", necromancer.description);
    }

    [Test]
    public void Siphoner_EachHitDrains1Mana()
    {
        ItemFactory siphon = LoadEnemy("Siphoner").items[0] as ItemFactory;
        Assert.IsNotNull(siphon);
        DrainCharacterManaBuffFactory drain = siphon.data.onHitEffects[0].buffFactoryList[0] as DrainCharacterManaBuffFactory;
        Assert.IsNotNull(drain);

        Assert.AreEqual(1f, GetFlatDamage(drain.data.consumerFactory), 0.0001f);
        StringAssert.Contains("drain 1 ", siphon.data.description);
    }

    [Test]
    public void Archer_ShootsWeakShotsAtYourFarthestUnit()
    {
        EntityData archer = LoadEnemy("Archer");

        Assert.AreEqual("Archer", archer.title);
        Assert.IsFalse(string.IsNullOrEmpty(archer.description));
        Assert.IsNotNull(archer.model);
        Assert.AreEqual(TargetBehaviourType.Farest, archer.targetBehaviourType);
        Assert.AreEqual(100f, archer.attributes[AttributeType.HealthMax]);
        Assert.AreEqual(3f, archer.attributes[AttributeType.Damage]);
        // A shot every second
        Assert.AreEqual(1f, archer.attributes[AttributeType.AttackRate]);
        Assert.AreEqual(1, archer.skillFactories.Count, "it attacks");
    }

    [Test]
    public void Crossbowman_ShootsSlowHeavyBoltsAtYourFarthestUnit()
    {
        EntityData crossbowman = LoadEnemy("Crossbowman");

        Assert.AreEqual("Crossbowman", crossbowman.title);
        Assert.IsFalse(string.IsNullOrEmpty(crossbowman.description));
        Assert.IsNotNull(crossbowman.model);
        Assert.AreEqual(TargetBehaviourType.Farest, crossbowman.targetBehaviourType);
        Assert.AreEqual(100f, crossbowman.attributes[AttributeType.HealthMax]);
        Assert.AreEqual(8f, crossbowman.attributes[AttributeType.Damage]);
        // A bolt every 2.5s
        Assert.AreEqual(2.5f, crossbowman.attributes[AttributeType.AttackRate]);
        Assert.AreEqual(1, crossbowman.skillFactories.Count, "it attacks");
    }

    [Test]
    public void HiddenHealer_FieldsThreeSoldiersInFrontAndTheShamanBetweenTwoCrossbowmen()
    {
        WavePatternData wave = LoadWave("Wave_HiddenHealer");

        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Crossbowman", "Shaman", "Crossbowman" }, GetRank(wave, 1));
    }

    [Test]
    public void FrontLine_FieldsTwoSoldiersInFrontAndTwoArchersBehind()
    {
        WavePatternData wave = LoadWave("Wave_FrontLine");

        Assert.AreEqual(2, wave.slots.GetLength(0));
        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Archer", "Archer" }, GetRank(wave, 1));
    }
}

}

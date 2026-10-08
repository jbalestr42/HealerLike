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

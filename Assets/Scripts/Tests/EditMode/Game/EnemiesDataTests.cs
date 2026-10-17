using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// Checks the enemies of the project data and the waves fielding them
public class EnemiesDataTests
{
    const string EntitiesPath = "Assets/Data/Entities/";
    const string WavesPath = "Assets/Data/WavePatterns/";
    const string VulnerableStatusPath = "Assets/Data/Statuses/VulnerableStatus/VulnerableStatus.asset";

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
        foreach ((int rank, int y, EntityData entity) unit in wave.GetUnitsByRank())
        {
            if (unit.rank == rank)
            {
                titles.Add(unit.entity.title);
            }
        }
        return titles;
    }

    // Ranks from the front one to the last one holding a unit
    static int GetRankCount(WavePatternData wave)
    {
        int count = 0;
        foreach ((int rank, int y, EntityData entity) unit in wave.GetUnitsByRank())
        {
            count = Mathf.Max(count, unit.rank + 1);
        }
        return count;
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

    // Armored, strikes twice then raises its guard: blocks 2 damage of each hit, then the next one whole
    [Test]
    public void ArmoredFootman_StrikesTwiceThenRaisesItsGuard()
    {
        EntityData footman = LoadEnemy("ArmoredFootman");

        Assert.AreEqual("Armored Footman", footman.title);
        Assert.IsFalse(string.IsNullOrEmpty(footman.description));
        Assert.IsNotNull(footman.model);
        Assert.AreEqual(2f, footman.attributes[AttributeType.FlatArmor]);
        Assert.AreEqual(TargetBehaviourType.Nearest, footman.targetBehaviourType);
        Assert.AreEqual(1, footman.skillFactories.Count);
        ConfigurableSkillFactory skill = footman.skillFactories[0] as ConfigurableSkillFactory;
        Assert.IsNotNull(skill);
        List<ASkillStepFactory> steps = skill.data.skillStepFactories;
        Assert.AreEqual(3, steps.Count);

        // Two strikes, each followed by a wait of half its attack rate (0.5s)
        RepeatSkillStepFactory strikes = steps[0] as RepeatSkillStepFactory;
        Assert.IsNotNull(strikes);
        Assert.AreEqual(2, strikes.data.count);
        Assert.IsTrue(strikes.shootsAtTargets);
        DurationSkillStepFactory strikeDelay = strikes.data.skillStepFactories[1] as DurationSkillStepFactory;
        Assert.IsNotNull(strikeDelay);
        AttributeValue delay = strikeDelay.data.duration as AttributeValue;
        Assert.IsNotNull(delay);
        Assert.AreEqual(AttributeType.AttackRate, delay.data.type);
        Assert.AreEqual(0.5f, delay.data.multiplier * footman.attributes[AttributeType.AttackRate], 0.0001f);

        // The guard: the next hit blocked, a single one however long it is not hit
        ApplyBuffSkillStepFactory guard = steps[1] as ApplyBuffSkillStepFactory;
        Assert.IsNotNull(guard);
        Assert.AreEqual(DurationType.Instant, guard.data.buffHandlerFactory.durationType);
        FlatModifierFactory hitArmor = guard.data.buffHandlerFactory.buffFactoryList[0] as FlatModifierFactory;
        Assert.IsNotNull(hitArmor);
        Assert.AreEqual(AttributeType.HitArmor, hitArmor.data.type);
        Assert.AreEqual(AttributeModifierType.Override, hitArmor.data.modifierType);
        Assert.AreEqual(1f, hitArmor.data.value);

        // Then a pause in guard
        Assert.IsInstanceOf<DurationSkillStepFactory>(steps[2]);
    }

    // Goes for the unit with the most health (the tank), strikes it 3 times in a row, then pauses for 2s
    [Test]
    public void Duelist_StrikesTheHealthiestUnitThreeTimesThenPauses()
    {
        EntityData duelist = LoadEnemy("Duelist");

        Assert.AreEqual("Duelist", duelist.title);
        Assert.IsFalse(string.IsNullOrEmpty(duelist.description));
        Assert.IsNotNull(duelist.model);
        Assert.AreEqual(TargetBehaviourType.HighestHealth, duelist.targetBehaviourType);
        Assert.AreEqual(1, duelist.skillFactories.Count);
        ConfigurableSkillFactory skill = duelist.skillFactories[0] as ConfigurableSkillFactory;
        Assert.IsNotNull(skill);
        List<ASkillStepFactory> steps = skill.data.skillStepFactories;
        Assert.AreEqual(2, steps.Count);

        // Three strikes in a row, a short wait after each one
        RepeatSkillStepFactory strikes = steps[0] as RepeatSkillStepFactory;
        Assert.IsNotNull(strikes);
        Assert.AreEqual(3, strikes.data.count);
        Assert.IsTrue(strikes.shootsAtTargets);
        AttributeValue strikeDelay = (strikes.data.skillStepFactories[1] as DurationSkillStepFactory).data.duration as AttributeValue;
        Assert.IsNotNull(strikeDelay);
        Assert.AreEqual(AttributeType.AttackRate, strikeDelay.data.type);
        Assert.Less(strikeDelay.data.multiplier * duelist.attributes[AttributeType.AttackRate], 0.5f);

        // Then the pause of 2s, shorter when its attacks are sped up (e.g. by a War Drum)
        DurationSkillStepFactory pause = steps[1] as DurationSkillStepFactory;
        Assert.IsNotNull(pause);
        AttributeValue pauseDuration = pause.data.duration as AttributeValue;
        Assert.IsNotNull(pauseDuration);
        Assert.AreEqual(AttributeType.AttackRate, pauseDuration.data.type);
        Assert.AreEqual(2f, pauseDuration.data.multiplier * duelist.attributes[AttributeType.AttackRate], 0.0001f);
    }

    // Elite alone on its floors: 3 random strikes making the units hit Vulnerable, then a slam on the whole team, then
    // a pause
    [Test]
    public void Ravager_StrikesThreeRandomUnitsThenChargesASlamOnTheWholeTeam()
    {
        EntityData ravager = LoadEnemy("Ravager");

        Assert.AreEqual("Ravager", ravager.title);
        Assert.IsFalse(string.IsNullOrEmpty(ravager.description));
        Assert.IsNotNull(ravager.model);
        Assert.AreEqual(TargetBehaviourType.Random, ravager.targetBehaviourType);
        Assert.AreEqual(0.3f, ravager.attributes[AttributeType.PercentArmor], 0.0001f);
        ConfigurableSkillFactory skill = ravager.skillFactories.Single() as ConfigurableSkillFactory;
        Assert.IsNotNull(skill);
        List<ASkillStepFactory> phases = skill.data.skillStepFactories;
        Assert.AreEqual(2, phases.Count);

        // The strikes phase: three strikes at its random target, each one making it Vulnerable, 0.5s after each one
        PhaseSkillStepFactory strikesPhase = phases[0] as PhaseSkillStepFactory;
        AssertPhase(strikesPhase, "Strikes");
        RepeatSkillStepFactory strikes = strikesPhase.data.skillStepFactories.Single() as RepeatSkillStepFactory;
        Assert.IsNotNull(strikes);
        Assert.AreEqual(3, strikes.data.count);
        ShootProjectileSkillStepFactory strike = strikes.data.skillStepFactories[0] as ShootProjectileSkillStepFactory;
        Assert.IsNotNull(strike);
        Assert.IsFalse(strike.data.atEveryTarget);
        CollectionAssert.AreEqual(new[] { AssetDatabase.LoadAssetAtPath<StatusData>(VulnerableStatusPath).buffHandlerFactory }, strike.data.projectiles.Single().onHitEffects);
        AttributeValue strikeDelay = (strikes.data.skillStepFactories[1] as DurationSkillStepFactory).data.duration as AttributeValue;
        Assert.AreEqual(AttributeType.AttackRate, strikeDelay.data.type);
        Assert.AreEqual(0.5f, strikeDelay.data.multiplier * ravager.attributes[AttributeType.AttackRate], 0.0001f);

        // The slam phase: charged for 2.5s, shown filling up above it, then every unit at once, harder than a strike,
        // making none Vulnerable
        PhaseSkillStepFactory slamPhase = phases[1] as PhaseSkillStepFactory;
        AssertPhase(slamPhase, "Slam");
        Assert.AreEqual(2, slamPhase.data.skillStepFactories.Count);
        AssertWindUp(slamPhase.data.skillStepFactories[0], ravager, 2.5f);
        ShootProjectileSkillStepFactory slam = slamPhase.data.skillStepFactories[1] as ShootProjectileSkillStepFactory;
        Assert.IsNotNull(slam);
        Assert.IsTrue(slam.data.atEveryTarget);
        ProjectileData slamProjectile = slam.data.projectiles.Single();
        CollectionAssert.IsEmpty(slamProjectile.onHitEffects);
        AttributeValue strikeDamage = (strike.data.projectiles.Single().onHitConsumer.Single() as ConsumerFactory).data.value as AttributeValue;
        AttributeValue slamDamage = (slamProjectile.onHitConsumer.Single() as ConsumerFactory).data.value as AttributeValue;
        Assert.AreEqual(AttributeType.Damage, strikeDamage.data.type);
        Assert.AreEqual(AttributeType.Damage, slamDamage.data.type);
        Assert.Greater(slamDamage.data.multiplier, strikeDamage.data.multiplier);
    }

    // A phase shown above its unit: a name and an icon
    static void AssertPhase(PhaseSkillStepFactory phase, string title)
    {
        Assert.IsNotNull(phase, title);
        Assert.AreEqual(title, phase.data.title);
        Assert.IsNotNull(phase.data.icon, title);
    }

    // A wait of that many seconds, shorter when the attacks of the unit are sped up
    static void AssertWindUp(ASkillStepFactory step, EntityData unit, float seconds)
    {
        AttributeValue windUp = (step as DurationSkillStepFactory)?.data.duration as AttributeValue;
        Assert.IsNotNull(windUp);
        Assert.AreEqual(AttributeType.AttackRate, windUp.data.type);
        Assert.AreEqual(seconds, windUp.data.multiplier * unit.attributes[AttributeType.AttackRate], 0.0001f);
    }

    // Stacks on every hit, from any source, until the end of the fight: +15% damage taken per stack
    [Test]
    public void Vulnerable_StacksForTheWholeFight_15PercentDamageTakenPerStack()
    {
        StatusData status = AssetDatabase.LoadAssetAtPath<StatusData>(VulnerableStatusPath);

        Assert.AreEqual("Vulnerable", status.title);
        Assert.IsFalse(string.IsNullOrEmpty(status.description));
        Assert.AreEqual("Vulnerable", status.tag.name);
        Assert.IsTrue(status.GivesItsTag());
        ABuffHandlerFactory vulnerable = status.buffHandlerFactory;
        Assert.AreEqual(DurationType.Infinite, vulnerable.durationType);
        Assert.AreEqual(0, vulnerable.maxStacks, "no limit");
        Assert.IsTrue(vulnerable.stackAcrossSources);
        Assert.IsNotNull(vulnerable.icon);
        Assert.IsTrue(vulnerable.HasTag(AssetDatabase.LoadAssetAtPath<GameplayTag>("Assets/Prefabs/Tags/Debuff.asset")), "a debuff, removed by the cleanses");
        Assert.IsFalse(vulnerable.HasTag(AssetDatabase.LoadAssetAtPath<GameplayTag>("Assets/Prefabs/Tags/Permanent.asset")), "gone at the end of the fight");

        Entities.TestUnits units = new Entities.TestUnits();
        try
        {
            Entity unit = units.Create(100f, 100f);
            unit.GetComponent<BuffManager>().isEnabled = true;
            Attribute vulnerability = unit.GetComponent<AttributeManager>().GetOrAdd(AttributeType.Vulnerability);
            GameObject source = new GameObject("Source");
            GameObject otherSource = new GameObject("Other Source");
            try
            {
                unit.ApplyStatus(status, source);
                unit.GetComponent<BuffManager>().ForceUpdate();
                vulnerability.Update();
                Assert.AreEqual(0.15f, vulnerability.Value, 0.0001f);

                unit.ApplyStatus(status, source);
                unit.ApplyStatus(status, otherSource);
                unit.GetComponent<BuffManager>().ForceUpdate();
                vulnerability.Update();
                Assert.AreEqual(0.45f, vulnerability.Value, 0.0001f);
                Assert.IsTrue(unit.HasStatus(status));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(otherSource);
            }
        }
        finally
        {
            units.DestroyAll();
        }
    }

    // How a Twin Blade raises its fallen twin
    static ReviveAllyOnDeathBuffFactory LoadTwinBladeRaising(EntityData twinBlade)
    {
        ItemFactory item = twinBlade.items.Single() as ItemFactory;
        Assert.IsNotNull(item);
        Assert.IsFalse(string.IsNullOrEmpty(item.data.description));
        BuffHandlerFactory permanent = item.data.buffs.Single() as BuffHandlerFactory;
        Assert.IsNotNull(permanent);
        Assert.AreEqual(DurationType.Infinite, permanent.durationType);
        ReviveAllyOnDeathBuffFactory revive = permanent.data.buffFactoryList.Single() as ReviveAllyOnDeathBuffFactory;
        Assert.IsNotNull(revive);
        return revive;
    }

    // Elite duo: two Duelists tougher than one, each raising the other when it falls
    [Test]
    public void TwinBlade_FightsLikeATougherDuelist_AndRaisesItsTwin()
    {
        EntityData twinBlade = LoadEnemy("TwinBlade");
        EntityData duelist = LoadEnemy("Duelist");

        Assert.AreEqual("Twin Blade", twinBlade.title);
        Assert.IsFalse(string.IsNullOrEmpty(twinBlade.description));
        Assert.IsNotNull(twinBlade.model);
        Assert.AreEqual(TargetBehaviourType.HighestHealth, twinBlade.targetBehaviourType);
        CollectionAssert.AreEqual(duelist.skillFactories, twinBlade.skillFactories, "3 strikes then a pause, as the Duelist");
        Assert.AreEqual(duelist.attributes[AttributeType.AttackRate], twinBlade.attributes[AttributeType.AttackRate]);
        Assert.AreEqual(duelist.attributes[AttributeType.Damage], twinBlade.attributes[AttributeType.Damage]);
        Assert.Greater(twinBlade.attributes[AttributeType.HealthMax], duelist.attributes[AttributeType.HealthMax]);

        // 6s of raising with +50% attack speed, the twin back with half its health
        ReviveAllyOnDeathBuffFactory revive = LoadTwinBladeRaising(twinBlade);
        Assert.AreEqual(0.5f, revive.data.healthRatio, 0.0001f);
        BuffHandlerFactory raising = revive.data.raisingBuffHandlerFactory as BuffHandlerFactory;
        Assert.IsNotNull(raising);
        Assert.AreEqual(DurationType.Duration, raising.durationType);
        Assert.AreEqual(6f, raising.data.duration, 0.0001f);
        Assert.AreEqual(1, raising.maxStacks);
        Assert.IsNotNull(raising.icon, "shows the raising on the twin left");
        AttackSpeedModifierFactory speed = raising.data.buffFactoryList.Single() as AttackSpeedModifierFactory;
        Assert.IsNotNull(speed);
        Assert.AreEqual(AttributeType.AttackRate, speed.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, speed.data.modifierType);
        Assert.AreEqual(0.5f, speed.data.value, 0.0001f);
    }

    // A shield gain read on the max health of its target, given at once
    static GainResourceBuffFactory LoadShieldGain(ABuffHandlerFactory handlerFactory)
    {
        BuffHandlerFactory handler = handlerFactory as BuffHandlerFactory;
        Assert.IsNotNull(handler);
        Assert.AreEqual(DurationType.Instant, handler.durationType);
        GainResourceBuffFactory gain = handler.data.buffFactoryList.Single() as GainResourceBuffFactory;
        Assert.IsNotNull(gain);
        Assert.AreEqual(ResourceType.Shield, gain.data.resourceType);
        Assert.AreEqual(ConsumerValueOwner.Target, gain.data.valueOwner);
        Assert.IsInstanceOf<MaxHealthValue>(gain.data.value);
        return gain;
    }

    static float MaxHealthRatio(GainResourceBuffFactory gain)
    {
        return ((MaxHealthValue)gain.data.value).data.multiplier;
    }

    // Elite tank: armored, starting without any shield (only her Shield Warden gives her one)
    [Test]
    public void IronMatron_IsArmored_AndStartsWithoutShield()
    {
        EntityData matron = LoadEnemy("IronMatron");

        Assert.AreEqual("Iron Matron", matron.title);
        Assert.IsFalse(string.IsNullOrEmpty(matron.description));
        Assert.IsNotNull(matron.model);
        Assert.AreEqual(TargetBehaviourType.Nearest, matron.targetBehaviourType);
        Assert.AreEqual(400f, matron.attributes[AttributeType.HealthMax]);
        Assert.IsFalse(matron.attributes.ContainsKey(AttributeType.FlatArmor), "no flat armor");
        Assert.AreEqual(0.1f, matron.attributes[AttributeType.PercentArmor], 0.0001f);
        Assert.IsNotEmpty(matron.skillFactories, "she attacks");
        Assert.IsTrue(matron.tags.Exists(tag => tag != null && tag.name == "IronMatron"), "found by her Wardens through her tag");
        Assert.IsEmpty(matron.items, "no item, so no starting shield");
    }

    // Doesn't attack, recharges the shield of the Iron Matron alone up to 10% of her max health every 5s, never piling up
    [Test]
    public void ShieldWarden_RechargesTheShieldOfTheIronMatronOnly()
    {
        EntityData warden = LoadEnemy("ShieldWarden");
        EntityData matron = LoadEnemy("IronMatron");

        Assert.AreEqual("Shield Warden", warden.title);
        Assert.IsFalse(string.IsNullOrEmpty(warden.description));
        Assert.IsNotNull(warden.model);
        Assert.AreEqual(0f, warden.attributes[AttributeType.Damage]);
        Assert.Less(warden.attributes[AttributeType.HealthMax], matron.attributes[AttributeType.HealthMax]);

        ApplyBuffOnTargetSkillFactory recharge = warden.skillFactories.Single() as ApplyBuffOnTargetSkillFactory;
        Assert.IsNotNull(recharge);
        Assert.IsTrue(recharge.data.targetAlly);
        Assert.IsFalse(recharge.data.singleTimeUse);
        Assert.AreEqual(5f, recharge.data.rate, 0.0001f);
        Assert.GreaterOrEqual(recharge.data.range, 100f, "reaches her anywhere on the board");
        GainResourceBuffFactory gain = LoadShieldGain(recharge.data.buffHandlerFactory);
        Assert.AreEqual(0.1f, MaxHealthRatio(gain), 0.0001f);
        Assert.IsTrue(gain.data.upToValue, "tops the shield up, never piles it up");

        TagValidatorFactory validator = recharge.data.targetValidators.Single() as TagValidatorFactory;
        Assert.IsNotNull(validator);
        Assert.IsTrue(validator.data.mustHave);
        CollectionAssert.Contains(matron.tags, validator.data.tag);
    }

    // Doesn't attack, gives +2 damage to the Iron Matron alone every 5s, stacking up to 5 times until the end of the fight
    [Test]
    public void WarDrummer_RaisesTheDamageOfTheIronMatronOnly_UpTo5Times()
    {
        EntityData drummer = LoadEnemy("WarDrummer");
        EntityData matron = LoadEnemy("IronMatron");

        Assert.AreEqual("War Drummer", drummer.title);
        Assert.IsFalse(string.IsNullOrEmpty(drummer.description));
        Assert.IsNotNull(drummer.model);
        Assert.AreEqual(0f, drummer.attributes[AttributeType.Damage]);
        Assert.Less(drummer.attributes[AttributeType.HealthMax], matron.attributes[AttributeType.HealthMax]);

        ApplyBuffOnTargetSkillFactory drum = drummer.skillFactories.Single() as ApplyBuffOnTargetSkillFactory;
        Assert.IsNotNull(drum);
        Assert.IsTrue(drum.data.targetAlly);
        Assert.IsFalse(drum.data.singleTimeUse);
        Assert.AreEqual(5f, drum.data.rate, 0.0001f);
        Assert.GreaterOrEqual(drum.data.range, 100f, "reaches her anywhere on the board");

        BuffHandlerFactory handler = drum.data.buffHandlerFactory as BuffHandlerFactory;
        Assert.IsNotNull(handler);
        Assert.AreEqual(DurationType.Infinite, handler.durationType, "kept until the end of the fight");
        Assert.AreEqual(5, handler.maxStacks);
        Assert.IsTrue(handler.stackAcrossSources, "outlives the drummer");
        FlatModifierFactory damage = handler.data.buffFactoryList.Single() as FlatModifierFactory;
        Assert.IsNotNull(damage);
        Assert.AreEqual(AttributeType.Damage, damage.data.type);
        Assert.AreEqual(AttributeModifierType.Add, damage.data.modifierType);
        Assert.AreEqual(2f, damage.data.value, 0.0001f);

        TagValidatorFactory validator = drum.data.targetValidators.Single() as TagValidatorFactory;
        Assert.IsNotNull(validator);
        Assert.IsTrue(validator.data.mustHave);
        CollectionAssert.Contains(matron.tags, validator.data.tag);
    }

    [Test]
    public void EliteIronMatron_FieldsTheMatronBetweenHerWardenAndHerDrummerInFront()
    {
        WavePatternData wave = LoadWave("Wave_Elite_IronMatron");

        Assert.AreEqual(1, GetRankCount(wave));
        CollectionAssert.AreEqual(new[] { "Shield Warden", "Iron Matron", "War Drummer" }, GetRank(wave, 0));
    }

    // Elite master: lightning on the nearest unit, +2 damage for the fight each time one of its Acolytes falls, up to 4
    [Test]
    public void Archon_StrikesWithLightning_AndGrowsWithEachFallenAcolyte()
    {
        EntityData archon = LoadEnemy("Archon");

        Assert.AreEqual("Archon", archon.title);
        Assert.IsFalse(string.IsNullOrEmpty(archon.description));
        Assert.IsNotNull(archon.model);
        Assert.AreEqual(TargetBehaviourType.Nearest, archon.targetBehaviourType);
        Assert.AreEqual(500f, archon.attributes[AttributeType.HealthMax]);
        Assert.IsTrue(archon.tags.Exists(tag => tag != null && tag.name == "Archon"), "found by his Acolytes through his tag");
        Assert.IsTrue(ShootsLightning(archon), "a lightning bolt, not a projectile");

        ApplyBuffOnAllyDeathBuffFactory onAllyDeath = GetItemBuff<ApplyBuffOnAllyDeathBuffFactory>(archon);
        ItemFactory item = archon.items[0] as ItemFactory;
        Assert.IsEmpty(item.data.projectileBehaviours, "a single bolt until the Spark Acolyte falls");
        BuffHandlerFactory growth = onAllyDeath.data.buffHandlerFactory as BuffHandlerFactory;
        Assert.IsNotNull(growth);
        Assert.AreEqual(DurationType.Infinite, growth.durationType, "kept until the end of the fight");
        Assert.AreEqual(4, growth.maxStacks, "one stack per Acolyte");
        FlatModifierFactory damage = growth.data.buffFactoryList.Single() as FlatModifierFactory;
        Assert.IsNotNull(damage);
        Assert.AreEqual(AttributeType.Damage, damage.data.type);
        Assert.AreEqual(AttributeModifierType.Add, damage.data.modifierType);
        Assert.AreEqual(2f, damage.data.value, 0.0001f);
    }

    static bool ShootsLightning(EntityData enemy)
    {
        ShootProjectileSkillFactory skill = enemy.skillFactories.Single() as ShootProjectileSkillFactory;
        Assert.IsNotNull(skill, enemy.title);
        return skill.data.projectiles.Single().projectilePrefab.GetComponent<ChainLightningProjectile>() != null;
    }

    // The buff an Acolyte hands down to the Archon when it dies, kept by him until the end of the fight
    static BuffHandlerFactory LoadLegacy(EntityData acolyte)
    {
        EntityData archon = LoadEnemy("Archon");
        Assert.AreEqual(TargetBehaviourType.Random, acolyte.targetBehaviourType, acolyte.title);
        Assert.IsFalse(string.IsNullOrEmpty(acolyte.description), acolyte.title);
        Assert.IsNotNull(acolyte.model, acolyte.title);
        Assert.Less(acolyte.attributes[AttributeType.HealthMax], archon.attributes[AttributeType.HealthMax], acolyte.title);
        Assert.Less(acolyte.attributes[AttributeType.Damage], archon.attributes[AttributeType.Damage], "small damage");

        List<ApplyBuffToAlliesOnDeathBuffFactory> legacies = acolyte.items
            .OfType<ItemFactory>()
            .SelectMany(item => item.data.buffs)
            .SelectMany(handler => handler.buffFactoryList)
            .OfType<ApplyBuffToAlliesOnDeathBuffFactory>()
            .ToList();
        Assert.AreEqual(1, legacies.Count, acolyte.title);
        CollectionAssert.Contains(archon.tags, legacies[0].data.tag, "given to the Archon only");
        BuffHandlerFactory legacy = legacies[0].data.buffHandlerFactory as BuffHandlerFactory;
        Assert.IsNotNull(legacy);
        Assert.AreEqual(DurationType.Infinite, legacy.durationType, "kept until the end of the fight");
        Assert.IsNotNull(legacy.icon, "shown on the Archon");
        return legacy;
    }

    static AItemFactory FindItem(EntityData enemy, string itemName)
    {
        return enemy.items.Find(item => item != null && item.title == itemName);
    }

    // Judgment: 3 strikes at the unit with the lowest share of health. Balance, charged for 2s: every unit set to the
    // average share of health. Verdict, charged for 2s: a slam on the whole team. Alone, armored
    [Test]
    public void Arbiter_JudgesTheWeakest_ThenBalancesTheTeam_ThenSlamsIt()
    {
        EntityData arbiter = LoadEnemy("Arbiter");

        Assert.AreEqual("Arbiter", arbiter.title);
        Assert.IsFalse(string.IsNullOrEmpty(arbiter.description));
        Assert.IsNotNull(arbiter.model);
        Assert.AreEqual(TargetBehaviourType.LowestHealth, arbiter.targetBehaviourType);
        Assert.AreEqual(0.3f, arbiter.attributes[AttributeType.PercentArmor], 0.0001f);
        Assert.Greater(arbiter.attributes[AttributeType.HealthMax], 500f, "the last elite before the boss");
        ConfigurableSkillFactory skill = arbiter.skillFactories.Single() as ConfigurableSkillFactory;
        Assert.IsNotNull(skill);
        List<ASkillStepFactory> phases = skill.data.skillStepFactories;
        Assert.AreEqual(3, phases.Count);

        PhaseSkillStepFactory judgment = phases[0] as PhaseSkillStepFactory;
        AssertPhase(judgment, "Judgment");
        RepeatSkillStepFactory strikes = judgment.data.skillStepFactories.Single() as RepeatSkillStepFactory;
        Assert.IsNotNull(strikes);
        Assert.AreEqual(3, strikes.data.count);
        ShootProjectileSkillStepFactory strike = strikes.data.skillStepFactories.OfType<ShootProjectileSkillStepFactory>().Single();
        Assert.IsFalse(strike.data.atEveryTarget, "at its own target: the weakest unit");

        PhaseSkillStepFactory balance = phases[1] as PhaseSkillStepFactory;
        AssertPhase(balance, "Balance");
        Assert.AreEqual(2, balance.data.skillStepFactories.Count);
        AssertWindUp(balance.data.skillStepFactories[0], arbiter, 2f);
        Assert.IsInstanceOf<EqualizeHealthSkillStepFactory>(balance.data.skillStepFactories[1]);

        PhaseSkillStepFactory verdict = phases[2] as PhaseSkillStepFactory;
        AssertPhase(verdict, "Verdict");
        Assert.AreEqual(2, verdict.data.skillStepFactories.Count);
        AssertWindUp(verdict.data.skillStepFactories[0], arbiter, 2f);
        ShootProjectileSkillStepFactory slam = verdict.data.skillStepFactories[1] as ShootProjectileSkillStepFactory;
        Assert.IsNotNull(slam);
        Assert.IsTrue(slam.data.atEveryTarget);
        AttributeValue slamDamage = (slam.data.projectiles.Single().onHitConsumer.Single() as ConsumerFactory).data.value as AttributeValue;
        Assert.AreEqual(AttributeType.Damage, slamDamage.data.type);
    }

    // Freezing shots at random units, the Archon attacking 30% faster once it falls
    [Test]
    public void FrostAcolyte_FreezesRandomUnits_ThenSpeedsUpTheArchon()
    {
        EntityData acolyte = LoadEnemy("FrostAcolyte");

        Assert.AreEqual("Frost Acolyte", acolyte.title);
        Assert.IsNotNull(FindItem(acolyte, "Frost"));
        AttackSpeedModifierFactory speed = LoadLegacy(acolyte).data.buffFactoryList.Single() as AttackSpeedModifierFactory;
        Assert.IsNotNull(speed);
        Assert.AreEqual(AttributeType.AttackRate, speed.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, speed.data.modifierType);
        Assert.AreEqual(0.3f, speed.data.value, 0.0001f);
    }

    // Shots halving the heals random units receive for 4s (the Plague of the Plague Bearer), the hits of the Archon
    // doing so once it falls
    [Test]
    public void BlightAcolyte_BlightsRandomUnits_ThenTheHitsOfTheArchonBlight()
    {
        EntityData acolyte = LoadEnemy("BlightAcolyte");
        AItemFactory plague = FindItem(acolyte, "Plague");

        Assert.AreEqual("Blight Acolyte", acolyte.title);
        Assert.AreEqual(TargetBehaviourType.Random, acolyte.targetBehaviourType);
        Assert.IsNotNull(plague);
        BuffHandlerFactory blighted = ((ItemFactory)plague).data.onHitEffects.Single() as BuffHandlerFactory;
        Assert.IsNotNull(blighted);
        Assert.AreEqual(DurationType.Duration, blighted.data.durationType);
        Assert.AreEqual(4f, blighted.data.duration, 0.0001f);
        Assert.AreEqual(1, blighted.data.maxStacks, "hit again, the target stays blighted without stacking");
        FlatModifierFactory healing = blighted.data.buffFactoryList.Single() as FlatModifierFactory;
        Assert.IsNotNull(healing);
        Assert.AreEqual(AttributeType.HealingReceived, healing.data.type);
        Assert.AreEqual(AttributeModifierType.Multiply, healing.data.modifierType);
        Assert.AreEqual(-0.5f, healing.data.value, 0.0001f);
        Assert.IsNull(FindItem(acolyte, "Venom"), "the Acolytes no longer poison");
        EquipItemBuffFactory equip = LoadLegacy(acolyte).data.buffFactoryList.Single() as EquipItemBuffFactory;
        Assert.IsNotNull(equip);
        Assert.AreSame(plague, equip.data.item);
    }

    // Shots making random units Vulnerable, the Archon getting 20% armor once it falls
    [Test]
    public void HexAcolyte_MakesRandomUnitsVulnerable_ThenArmorsTheArchon()
    {
        EntityData acolyte = LoadEnemy("HexAcolyte");
        StatusData vulnerable = AssetDatabase.LoadAssetAtPath<StatusData>(VulnerableStatusPath);

        Assert.AreEqual("Hex Acolyte", acolyte.title);
        ShootProjectileSkillFactory skill = acolyte.skillFactories.Single() as ShootProjectileSkillFactory;
        Assert.IsNotNull(skill);
        CollectionAssert.Contains(skill.data.projectiles.Single().onHitEffects, vulnerable.buffHandlerFactory);
        FlatModifierFactory armor = LoadLegacy(acolyte).data.buffFactoryList.Single() as FlatModifierFactory;
        Assert.IsNotNull(armor);
        Assert.AreEqual(AttributeType.PercentArmor, armor.data.type);
        Assert.AreEqual(AttributeModifierType.Add, armor.data.modifierType);
        Assert.AreEqual(0.2f, armor.data.value, 0.0001f);
    }

    // Lightning on a random unit chaining to 1 more, the lightning of the Archon chaining too once it falls
    [Test]
    public void SparkAcolyte_ChainsLightning_ThenTheLightningOfTheArchonChains()
    {
        EntityData acolyte = LoadEnemy("SparkAcolyte");
        ItemFactory spark = FindItem(acolyte, "Spark") as ItemFactory;

        Assert.AreEqual("Spark Acolyte", acolyte.title);
        Assert.IsTrue(ShootsLightning(acolyte));
        Assert.IsNotNull(spark);
        BuffHandlerFactory chain = spark.data.projectileBehaviours.Single() as BuffHandlerFactory;
        Assert.IsNotNull(chain);
        ProjectileBehaviourBuffFactory behaviour = chain.data.buffFactoryList.Single() as ProjectileBehaviourBuffFactory;
        Assert.IsNotNull(behaviour);
        BounceProjectileBehaviourFactory bounce = behaviour.data.projectileBehaviour as BounceProjectileBehaviourFactory;
        Assert.IsNotNull(bounce);
        Assert.AreEqual(1, bounce.data.bounce, "1 more unit");
        EquipItemBuffFactory equip = LoadLegacy(acolyte).data.buffFactoryList.Single() as EquipItemBuffFactory;
        Assert.IsNotNull(equip);
        Assert.AreSame(spark, equip.data.item);
    }

    // The Archon in the middle of a cross: an Acolyte in front of him, one on each side, one behind
    [Test]
    public void EliteArchon_FieldsTheArchonInTheMiddleOfACrossOfAcolytes()
    {
        WavePatternData wave = LoadWave("Wave_Elite_Archon");
        List<(int rank, int y, EntityData entity)> units = wave.GetUnitsByRank().ToList();
        int archonLane = units.Single(unit => unit.entity.title == "Archon").y;

        Assert.AreEqual(3, GetRankCount(wave));
        CollectionAssert.AreEqual(new[] { "Frost Acolyte" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Blight Acolyte", "Archon", "Hex Acolyte" }, GetRank(wave, 1));
        CollectionAssert.AreEqual(new[] { "Spark Acolyte" }, GetRank(wave, 2));
        Assert.AreEqual(archonLane, units.Single(unit => unit.entity.title == "Frost Acolyte").y, "in front of him");
        Assert.AreEqual(archonLane, units.Single(unit => unit.entity.title == "Spark Acolyte").y, "behind him");
    }

    // Strikes the most wounded unit, twice as hard when it is under half health
    [Test]
    public void Headsman_HitsTheMostWoundedUnit_TwiceAsHardUnderHalfHealth()
    {
        EntityData headsman = LoadEnemy("Headsman");

        Assert.AreEqual("Headsman", headsman.title);
        Assert.IsFalse(string.IsNullOrEmpty(headsman.description));
        Assert.IsNotNull(headsman.model);
        Assert.AreEqual(TargetBehaviourType.LowestHealth, headsman.targetBehaviourType);
        Assert.AreEqual(1, headsman.skillFactories.Count);
        ShootProjectileSkillFactory skill = headsman.skillFactories[0] as ShootProjectileSkillFactory;
        Assert.IsNotNull(skill);

        WoundedTargetConsumerFactory hit = skill.data.projectiles[0].onHitConsumer[0] as WoundedTargetConsumerFactory;
        Assert.IsNotNull(hit);
        Assert.AreEqual(0.5f, hit.data.healthThreshold, 0.0001f);
        Assert.AreEqual(2f, hit.data.multiplier, 0.0001f);
        AttributeValue damage = hit.data.value as AttributeValue;
        Assert.IsNotNull(damage);
        Assert.AreEqual(AttributeType.Damage, damage.data.type);
        Assert.AreEqual(ConsumerValueOwner.Source, hit.data.valueOwner);
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
    public void Fuse_FieldsOneLitKamikazeInFrontAndTwoVengefulSnipersBehind()
    {
        WavePatternData wave = LoadWave("Wave_Fuse");

        CollectionAssert.AreEqual(new[] { "Lit Kamikaze" }, GetRank(wave, 0));
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
    public void EliteBastion_FieldsTheBannermanNextToAMortarBehindTheColossusAndTheShamanBehindIt()
    {
        WavePatternData wave = LoadWave("Wave_Elite_Bastion");

        Assert.AreEqual(3, GetRankCount(wave));
        CollectionAssert.AreEqual(new[] { "Soldier", "Colossus" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Bannerman", "Mortar" }, GetRank(wave, 1));
        CollectionAssert.AreEqual(new[] { "Shaman" }, GetRank(wave, 2));
        // In the middle, the Bannerman boosts every unit of the wave
        Assert.AreEqual("Bannerman", wave.GetEntity(wave.frontColumn + 1, 1).title);
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

        Assert.AreEqual(3, GetRankCount(wave));
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
        Assert.AreEqual(7f, crossbowman.attributes[AttributeType.Damage]);
        // A bolt every 2.5s
        Assert.AreEqual(2.5f, crossbowman.attributes[AttributeType.AttackRate]);
        Assert.AreEqual(1, crossbowman.skillFactories.Count, "it attacks");
    }

    [Test]
    public void HiddenHealer_FieldsTwoSoldiersInFrontAndTheShamanBetweenTwoCrossbowmen()
    {
        WavePatternData wave = LoadWave("Wave_HiddenHealer");

        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Crossbowman", "Shaman", "Crossbowman" }, GetRank(wave, 1));
    }

    [Test]
    public void FrontLine_FieldsTwoSoldiersInFrontAndThreeArchersBehind()
    {
        WavePatternData wave = LoadWave("Wave_FrontLine");

        Assert.AreEqual(2, GetRankCount(wave));
        CollectionAssert.AreEqual(new[] { "Soldier", "Soldier" }, GetRank(wave, 0));
        CollectionAssert.AreEqual(new[] { "Archer", "Archer", "Archer" }, GetRank(wave, 1));
    }
}

}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// The items rebalanced on 2026-10-18 that no other test covers: Poison spread at random, Growing Seed capped,
// Conclave and Health Regen made worth it, Increase Damage Per Hit doubled (the Hex Weaver's too)
public class ItemRebalanceDataTests
{
    const string EntityItems = "Assets/Data/EntityItems/";

    static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, path);
        return asset;
    }

    static List<T> GetBuffs<T>(List<ABuffHandlerFactory> handlers) where T : ABuffFactory
    {
        List<T> buffs = new List<T>();
        foreach (ABuffHandlerFactory handler in handlers)
        {
            foreach (ABuffFactory buff in handler.buffFactoryList)
            {
                if (buff is T typed)
                {
                    buffs.Add(typed);
                }
            }
        }
        return buffs;
    }

    // Its stacks land on a random enemy each attack, so they can't pile up on the toughest one
    [Test]
    public void Poison_OneStackPerHit_AndTheHolderAttacksARandomEnemy()
    {
        ItemFactory poison = Load<ItemFactory>(EntityItems + "PoisonItem/PoisonItem.asset");

        Assert.AreEqual(1, poison.data.onHitEffects[0].appliedStacks);
        List<ForceTargetBehaviourBuffFactory> forces = GetBuffs<ForceTargetBehaviourBuffFactory>(poison.data.buffs);
        Assert.AreEqual(1, forces.Count);
        Assert.AreEqual(TargetBehaviourType.Random, forces[0].data.targetBehaviourType);
        foreach (ABuffHandlerFactory handler in poison.data.buffs)
        {
            Assert.AreEqual(DurationType.Infinite, handler.durationType, "As long as the item is held");
        }
    }

    // The player must know why the holder no longer shoots where it used to
    [Test]
    public void Poison_DescriptionSaysTheHolderAttacksAtRandom()
    {
        ItemFactory poison = Load<ItemFactory>(EntityItems + "PoisonItem/PoisonItem.asset");

        StringAssert.Contains("random enemy", poison.data.description);
        StringAssert.Contains("1 {status:Poisoned} stack", poison.data.description);
    }

    [Test]
    public void GrowingSeed_GrowsAtMost8Battles()
    {
        GrowingItemFactory seed = Load<GrowingItemFactory>(EntityItems + "GrowingSeedItem/GrowingSeedItem.asset");

        Assert.AreEqual(8, seed.data.maxGrowth);
    }

    // Both halves of its cycle are bonuses: +20% damage, then -30% attack cooldown
    [Test]
    public void Conclave_AlternatesPlus20PercentDamageAndMinus30PercentAttackCooldown()
    {
        ScriptableObject skill = Load<ScriptableObject>(EntityItems + "ConclaveItem/ApplyBuffPeriodicallySkillFactory.asset");
        SerializedProperty periodic = new SerializedObject(skill).FindProperty("data.periodicBuff");
        List<ABuffHandlerFactory> handlers = new List<ABuffHandlerFactory>();
        for (int i = 0; i < periodic.arraySize; i++)
        {
            handlers.Add((ABuffHandlerFactory)periodic.GetArrayElementAtIndex(i).objectReferenceValue);
        }

        List<FlatModifierFactory> modifiers = GetBuffs<FlatModifierFactory>(handlers);
        FlatModifierFactory damage = modifiers.Find(modifier => modifier.data.type == AttributeType.Damage);
        FlatModifierFactory attackRate = modifiers.Find(modifier => modifier.data.type == AttributeType.AttackRate);

        Assert.AreEqual(2, modifiers.Count);
        Assert.AreEqual(0.2f, damage.data.value, 0.0001f);
        Assert.AreEqual(-0.3f, attackRate.data.value, 0.0001f);
        Assert.AreEqual(AttributeModifierType.Multiply, attackRate.data.modifierType);
    }

    [Test]
    public void HealthRegen_Heals3HPEverySecond()
    {
        ItemFactory regen = Load<ItemFactory>(EntityItems + "RegenHpItem/RegenHpItem.asset");
        BuffHandlerFactory handler = (BuffHandlerFactory)regen.data.buffs[0];
        ApplyConsumerBuffFactory heal = GetBuffs<ApplyConsumerBuffFactory>(regen.data.buffs)[0];
        GameObject go = new GameObject("Holder");
        try
        {
            Assert.IsTrue(handler.data.isPeriodic);
            Assert.AreEqual(1f, handler.data.periodDuration, 0.0001f);
            Assert.AreEqual(3f, Mathf.Abs(heal.data.consumerFactory.GetConsumer(go, go).GetValue()), 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IncreaseDamagePerHit_Plus2PercentPerHit()
    {
        ItemFactory item = Load<ItemFactory>(EntityItems + "IncreaseDamagePerHitItem/IncreaseDamagePerHitItem.asset");

        FlatModifierFactory vulnerability = (FlatModifierFactory)item.data.onHitEffects[0].buffFactoryList[0];

        Assert.AreEqual(AttributeType.Vulnerability, vulnerability.data.type);
        Assert.AreEqual(0.02f, vulnerability.data.value, 0.0001f);
    }

    // The Hex Weaver holds the reward item itself, so it got the +2% too
    [Test]
    public void HexWeaver_HoldsTheRewardItem()
    {
        ItemFactory reward = Load<ItemFactory>(EntityItems + "IncreaseDamagePerHitItem/IncreaseDamagePerHitItem.asset");
        EntityData hexWeaver = Load<EntityData>("Assets/Data/Entities/HexWeaverEntity/HexWeaverEntity.asset");

        Assert.IsTrue(hexWeaver.items.Contains(reward));
    }
}

}

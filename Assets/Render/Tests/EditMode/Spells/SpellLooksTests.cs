using System.Collections.Generic;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class SpellLooksTests
{
    readonly List<Object> _objects = new List<Object>();

    static SpellLooks LoadShipped()
    {
        return AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    [Test]
    public void GetLook_MappedBuff_ReturnsTheRow()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();
        BuffHandlerFactory factory = CreateTracked<BuffHandlerFactory>();
        SpellLook look = new SpellLook { element = EffectElement.ManaUp };
        looks.buffs[factory] = look;

        Assert.AreSame(look, looks.GetLook(factory, null, null));
    }

    [Test]
    public void GetLook_UnmappedBuff_DerivesItFromTheHandlerAndTheSides()
    {
        SpellLooks looks = CreateTracked<SpellLooks>();
        BuffHandlerFactory harm = SpellSinkFixture.Modifier(AttributeType.Damage, -2f, _objects);
        GameObject enemyGo = new GameObject("Enemy");
        GameObject allyGo = new GameObject("Ally");
        _objects.Add(enemyGo);
        _objects.Add(allyGo);
        Entity enemy = null;
        TestHelpers.WithLoggingDisabled(() =>
        {
            enemy = enemyGo.AddComponent<Entity>();
            allyGo.AddComponent<Entity>().entityType = Entity.EntityType.Player;
        });
        enemy.entityType = Entity.EntityType.Computer;

        SpellLook look = looks.GetLook(harm, enemyGo, allyGo);

        Assert.AreEqual(EffectFamily.Bane, look.family);
        Assert.AreEqual(EffectElement.Press, look.element);
        Assert.IsNotNull(looks.GetLook(null, null, null));
    }

    // Only rows where the grammar is wrong for the handler stay: the three listener items on the healer, whose
    // Infinite handler would loop their look for the whole run
    [TestCase("PlayerItems/ManaOnRoundEndItem/ManaOnRoundEndItem_BuffHandlerFactory", EffectElement.ManaUp)]
    [TestCase("PlayerItems/DamageAllEnemyItem/BuffHandlerFactory", EffectElement.Burst)]
    [TestCase("PlayerItems/HealAllEntitiesOnRoundEndItem/HealAllEntitiesOnRoundEndItem_BuffHandlerFactory",
              EffectElement.Rise)]
    public void GetLook_ShippedKeptBuffRow_DrawsOnceWithItsElement(string path, EffectElement expected)
    {
        SpellLooks looks = LoadShipped();
        string handlerPath = "Assets/Data/" + path + ".asset";
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(handlerPath);

        SpellLook row = looks.GetLook(handler, null, null);

        Assert.AreEqual(expected, row.element);
        Assert.AreEqual(EffectTempo.Once, row.tempo);
    }

    [Test]
    public void GetLook_ShippedBuffRows_AreInfiniteHandlersDrawnOnce()
    {
        SpellLooks looks = LoadShipped();

        foreach (KeyValuePair<ABuffHandlerFactory, SpellLook> row in looks.buffs)
        {
            BuffHandlerFactory handler = row.Key as BuffHandlerFactory;
            Assert.IsNotNull(handler, row.Key.name);
            Assert.AreEqual(DurationType.Infinite, handler.data.durationType, row.Key.name);
            Assert.AreEqual(EffectTempo.Once, row.Value.tempo, row.Key.name);
        }
    }
}

}

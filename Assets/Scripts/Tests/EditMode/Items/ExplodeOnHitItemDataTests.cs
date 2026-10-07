using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// Explode On Hit: the shots of the unit explode on the target, paid by a slower attack and no bonus damage
public class ExplodeOnHitItemDataTests
{
    const string ItemPath = "Assets/Data/EntityItems/ExplodeOnHitItem/ExplodeOnHitItem.asset";

    GameObject _go;
    AttributeManager _attributes;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Unit");
        _attributes = TestHelpers.CreateAttributeManager(_go);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    static ItemFactory Load()
    {
        ItemFactory item = AssetDatabase.LoadAssetAtPath<ItemFactory>(ItemPath);
        Assert.IsNotNull(item, ItemPath);
        return item;
    }

    // Adds every buff of the item, as equipping it does
    void AddItemBuffs(ItemFactory item)
    {
        foreach (ABuffHandlerFactory handler in item.data.buffs)
        {
            Assert.AreEqual(DurationType.Infinite, handler.durationType);
            foreach (ABuffFactory buffFactory in handler.buffFactoryList)
            {
                buffFactory.GetBuff(null).Add(_go, _go);
            }
        }
    }

    float Get(AttributeType type)
    {
        Attribute attribute = _attributes.GetOrAdd(type);
        attribute.Update();
        return attribute.Value;
    }

    [Test]
    public void ExplodeOnHit_DescriptionNoLongerPromisesBonusDamage()
    {
        ItemFactory item = Load();

        Assert.AreEqual("Explode On Hit", item.data.name);
        StringAssert.Contains("+50% attack cooldown", item.data.description);
        StringAssert.DoesNotContain("damage", item.data.description);
    }

    [Test]
    public void ExplodeOnHit_GivesNoBonusDamage()
    {
        _attributes.Add(AttributeType.Damage, new Attribute(10f));

        AddItemBuffs(Load());

        Assert.AreEqual(10f, Get(AttributeType.Damage), 0.0001f);
    }

    [Test]
    public void ExplodeOnHit_Adds50PercentAttackCooldown()
    {
        _attributes.Add(AttributeType.AttackRate, new Attribute(2f));

        AddItemBuffs(Load());

        Assert.AreEqual(3f, Get(AttributeType.AttackRate), 0.0001f);
    }

    [Test]
    public void ExplodeOnHit_ShotsExplodeWithin1CellOfTheTarget()
    {
        ItemFactory item = Load();

        Assert.AreEqual(1, item.data.projectileBehaviours.Count);
        ProjectileBehaviourBuffFactory buff = (ProjectileBehaviourBuffFactory)item.data.projectileBehaviours[0].buffFactoryList[0];
        AreaOfEffectProjectileBehaviourFactory explosion = buff.data.projectileBehaviour as AreaOfEffectProjectileBehaviourFactory;
        Assert.IsNotNull(explosion);
        Assert.AreEqual(1f, explosion.data.radius, 0.0001f);
    }
}

}

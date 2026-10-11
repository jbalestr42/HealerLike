using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Balance
{

public class ReferenceTeamGeneratorTests
{
    readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    EntityData CreateUnit(string title, float maxHealth = 100f, string role = null, float damage = 0f, float attackRate = 0f)
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        unit.title = title;
        unit.attributes[AttributeType.HealthMax] = maxHealth;
        unit.attributes[AttributeType.Damage] = damage;
        unit.attributes[AttributeType.AttackRate] = attackRate;
        if (role != null)
        {
            unit.tags.Add(CreateTag(role));
        }
        _created.Add(unit);
        return unit;
    }

    AItemFactory CreateItem(string role = null)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        item.data = new ItemData();
        if (role != null)
        {
            item.data.tags.Add(CreateTag(role));
        }
        _created.Add(item);
        return item;
    }

    GameplayTag CreateTag(string tagName)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = tagName;
        _created.Add(tag);
        return tag;
    }

    // Floors of one room each, linked in a line, then the boss
    static RunMap CreateLine(params MapNodeType[] types)
    {
        List<List<MapNode>> floors = new List<List<MapNode>>();
        MapNode previous = null;
        for (int floor = 0; floor < types.Length; floor++)
        {
            MapNode node = new MapNode(floor, 0, types[floor]);
            previous?.Connect(node);
            previous = node;
            floors.Add(new List<MapNode> { node });
        }
        MapNode boss = new MapNode(types.Length, 0, MapNodeType.Boss);
        previous.Connect(boss);
        return new RunMap(floors, boss, 1);
    }

    RewardPools CreatePools(float unitChance, float playerItemChance)
    {
        return new RewardPools
        {
            units = new List<EntityData> { CreateUnit("Recruit") },
            unitItems = new List<AItemFactory> { CreateItem() },
            playerItems = new List<AItemFactory> { CreateItem() },
            unitChance = unitChance,
            playerItemChance = playerItemChance,
        };
    }

    [Test]
    public void GivesReward_FightsTreasuresAndEvents_NotRestsNorBoss()
    {
        Assert.IsTrue(ReferenceTeamGenerator.GivesReward(MapNodeType.Combat));
        Assert.IsTrue(ReferenceTeamGenerator.GivesReward(MapNodeType.Elite));
        Assert.IsTrue(ReferenceTeamGenerator.GivesReward(MapNodeType.Treasure));
        Assert.IsTrue(ReferenceTeamGenerator.GivesReward(MapNodeType.Event));
        Assert.IsFalse(ReferenceTeamGenerator.GivesReward(MapNodeType.Rest));
        Assert.IsFalse(ReferenceTeamGenerator.GivesReward(MapNodeType.Boss));
    }

    [Test]
    public void PickPath_FromAStartRoomToTheBoss_OneRoomPerFloor()
    {
        RunMap map = MapGenerator.GenerateLayout(8, 5, 4, new System.Random(3));

        List<MapNode> path = ReferenceTeamGenerator.PickPath(map, new System.Random(5));

        Assert.AreEqual(9, path.Count);
        CollectionAssert.Contains(map.startNodes, path[0]);
        Assert.AreSame(map.boss, path[path.Count - 1]);
        for (int i = 1; i < path.Count; i++)
        {
            Assert.AreEqual(i, path[i].floor);
            Assert.IsTrue(path[i - 1].IsConnectedTo(path[i]));
        }
    }

    [Test]
    public void GenerateRun_OneTeamPerRoom_RewardsOnlyAfterTheRoomsGivingOne()
    {
        RunMap map = CreateLine(MapNodeType.Combat, MapNodeType.Rest, MapNodeType.Elite, MapNodeType.Treasure);
        EntityData knight = CreateUnit("Knight");

        List<ReferenceTeam> teams = ReferenceTeamGenerator.GenerateRun(ReferenceTeamGenerator.PickPath(map, new System.Random(1)), new List<EntityData> { knight }, CreatePools(0.5f, 0.5f), new System.Random(1));

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, teams.ConvertAll(team => team.floor));
        // Combat, Rest (nothing), Elite, Treasure, then the boss
        CollectionAssert.AreEqual(new[] { 0, 1, 1, 2, 3 }, teams.ConvertAll(team => team.rewardCount));
        Assert.AreSame(knight, teams[0].units[0]);
        Assert.AreEqual(1, teams[0].units.Count);
        Assert.IsEmpty(teams[0].unitItems);
        Assert.IsEmpty(teams[0].playerItems);
    }

    [Test]
    public void GenerateRun_EachRewardIsAUnitOrAnItem()
    {
        RunMap map = CreateLine(MapNodeType.Combat, MapNodeType.Combat, MapNodeType.Combat, MapNodeType.Combat, MapNodeType.Combat, MapNodeType.Combat);

        List<ReferenceTeam> teams = ReferenceTeamGenerator.GenerateRun(ReferenceTeamGenerator.PickPath(map, new System.Random(2)), new List<EntityData> { CreateUnit("Knight") }, CreatePools(0.3f, 0.3f), new System.Random(2));

        ReferenceTeam boss = teams[teams.Count - 1];
        Assert.AreEqual(6, boss.rewardCount);
        Assert.AreEqual(6, boss.units.Count - 1 + boss.unitItems.Count + boss.playerItems.Count);
    }

    [Test]
    public void GenerateRun_EarlierTeamsDontChange()
    {
        RunMap map = CreateLine(MapNodeType.Combat, MapNodeType.Combat, MapNodeType.Combat);

        List<ReferenceTeam> teams = ReferenceTeamGenerator.GenerateRun(ReferenceTeamGenerator.PickPath(map, new System.Random(4)), new List<EntityData> { CreateUnit("Knight") }, CreatePools(1f, 0f), new System.Random(4));

        // Always a unit
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, teams.ConvertAll(team => team.units.Count));
    }

    [Test]
    public void GenerateRun_SameSeed_SameTeams()
    {
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Data/Run/MapGenerationSettings.asset");
        Assert.IsNotNull(settings);
        RewardPools pools = CreatePools(0.3f, 0.3f);
        List<EntityData> starting = new List<EntityData> { CreateUnit("Knight") };

        List<ReferenceTeam> first = ReferenceTeamGenerator.GenerateRun(settings, starting, pools, 42);
        List<ReferenceTeam> second = ReferenceTeamGenerator.GenerateRun(settings, starting, pools, 42);

        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].floor, second[i].floor);
            Assert.AreEqual(first[i].rewardCount, second[i].rewardCount);
            CollectionAssert.AreEqual(first[i].units, second[i].units);
            Assert.AreEqual(first[i].unitItems.Count, second[i].unitItems.Count);
            Assert.AreEqual(first[i].playerItems.Count, second[i].playerItems.Count);
        }
    }

    [Test]
    public void AddRandomReward_AlwaysAUnit_WhenTheChanceIsOne()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Knight") } };
        RewardPools pools = CreatePools(1f, 0f);

        ReferenceTeamGenerator.AddRandomReward(team, pools, new System.Random(0));

        Assert.AreEqual(2, team.units.Count);
        Assert.AreSame(pools.units[0], team.units[1]);
        Assert.AreEqual(1, team.rewardCount);
    }

    [Test]
    public void AddRandomReward_NoUnitToOffer_GivesAnItem()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Knight") } };
        RewardPools pools = CreatePools(1f, 0f);
        pools.units.Clear();

        ReferenceTeamGenerator.AddRandomReward(team, pools, new System.Random(0));

        Assert.AreEqual(1, team.units.Count);
        Assert.AreEqual(1, team.unitItems.Count);
    }

    [Test]
    public void AddRandomReward_PlayerItem_WhenTheChanceIsOne()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Knight") } };
        RewardPools pools = CreatePools(0f, 1f);

        ReferenceTeamGenerator.AddRandomReward(team, pools, new System.Random(0));

        CollectionAssert.AreEqual(pools.playerItems, team.playerItems);
        Assert.IsEmpty(team.unitItems);
    }

    [Test]
    public void AddRandomReward_UnitItems_SpreadOverTheTeam()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Knight"), CreateUnit("Archer"), CreateUnit("Mage") } };
        RewardPools pools = CreatePools(0f, 0f);

        for (int i = 0; i < 4; i++)
        {
            ReferenceTeamGenerator.AddRandomReward(team, pools, new System.Random(i));
        }

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, team.unitItems.ConvertAll(unitItem => unitItem.unit));
    }

    [Test]
    public void GetUnitFor_TankItem_TheTankWithTheMostHealth()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData>
        {
            // More health, but not a tank
            CreateUnit("Brute", 400f, TagNames.Damage, 4f, 1f),
            CreateUnit("Soldier", 150f, TagNames.Tank),
            CreateUnit("Treant", 300f, TagNames.Tank),
        } };

        Assert.AreEqual(2, ReferenceTeamGenerator.GetUnitFor(team, CreateItem(TagNames.Tank)));
    }

    [Test]
    public void GetUnitFor_TankItemWithoutATank_TheUnitWithTheMostHealth()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Archer", 80f), CreateUnit("Knight", 200f), CreateUnit("Mage", 90f) } };

        Assert.AreEqual(1, ReferenceTeamGenerator.GetUnitFor(team, CreateItem(TagNames.Tank)));
    }

    [Test]
    public void GetUnitFor_DamageItem_TheDamageDealerWithTheMostDamagePerSecond()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData>
        {
            // 10 per second, but not a damage dealer
            CreateUnit("Treant", 300f, TagNames.Tank, 10f, 1f),
            // 4 per second
            CreateUnit("Normal", 100f, TagNames.Damage, 4f, 1f),
            // 6 per second
            CreateUnit("Fast Shot", 100f, TagNames.Damage, 3f, 0.5f),
            CreateUnit("Grove Keeper", 100f, TagNames.Support),
        } };

        Assert.AreEqual(2, ReferenceTeamGenerator.GetUnitFor(team, CreateItem(TagNames.Damage)));
    }

    [Test]
    public void GetUnitFor_DamageItemWithoutADamageDealer_TheUnitWithTheMostDamagePerSecond()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData>
        {
            CreateUnit("Buff", 100f, TagNames.Support),
            CreateUnit("Treant", 300f, TagNames.Tank, 3f, 0.8f),
        } };

        Assert.AreEqual(1, ReferenceTeamGenerator.GetUnitFor(team, CreateItem(TagNames.Damage)));
    }

    [Test]
    public void GetUnitFor_EveryRoleItemOnTheSameBestUnit()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Normal", 100f, TagNames.Damage, 4f, 1f), CreateUnit("Treant", 300f, TagNames.Tank) } };
        RewardPools pools = CreatePools(0f, 0f);
        pools.unitItems = new List<AItemFactory> { CreateItem(TagNames.Tank) };

        for (int i = 0; i < 3; i++)
        {
            ReferenceTeamGenerator.AddRandomReward(team, pools, new System.Random(i));
        }

        CollectionAssert.AreEqual(new[] { 1, 1, 1 }, team.unitItems.ConvertAll(unitItem => unitItem.unit));
    }

    [Test]
    public void GetUnitFor_SupportItem_SpreadLikeTheItemsWithoutARole()
    {
        ReferenceTeam team = new ReferenceTeam { units = new List<EntityData> { CreateUnit("Treant", 300f, TagNames.Tank), CreateUnit("Grove Keeper", 100f, TagNames.Support) } };
        team.unitItems.Add(new ReferenceTeam.UnitItem { unit = 0, item = CreateItem() });

        Assert.AreEqual(1, ReferenceTeamGenerator.GetUnitFor(team, CreateItem(TagNames.Support)));
    }

    [Test]
    public void GetDamagePerSecond_DamageOverTheTimeBetweenTwoAttacks()
    {
        Assert.AreEqual(6f, ReferenceTeamGenerator.GetDamagePerSecond(CreateUnit("Fast Shot", 100f, null, 3f, 0.5f)), 0.001f);
        Assert.AreEqual(0f, ReferenceTeamGenerator.GetDamagePerSecond(CreateUnit("Buff", 100f)));
        Assert.AreEqual(0f, ReferenceTeamGenerator.GetDamagePerSecond(null));
    }

    [Test]
    public void GetPlacementOrder_MostMaxHealthInFront_TiesInTeamOrder()
    {
        List<EntityData> units = new List<EntityData>
        {
            CreateUnit("Archer", 60f),
            CreateUnit("Knight", 200f),
            CreateUnit("Mage", 60f),
            CreateUnit("Guardian", 150f),
        };

        CollectionAssert.AreEqual(new[] { 1, 3, 0, 2 }, ReferenceTeamGenerator.GetPlacementOrder(units));
    }

    [Test]
    public void GetMaxHealth_NoHealthAttribute_IsZero()
    {
        EntityData unit = CreateUnit("Ghost");
        unit.attributes.Clear();

        Assert.AreEqual(0f, ReferenceTeamGenerator.GetMaxHealth(unit));
        Assert.AreEqual(0f, ReferenceTeamGenerator.GetMaxHealth(null));
    }
}

}

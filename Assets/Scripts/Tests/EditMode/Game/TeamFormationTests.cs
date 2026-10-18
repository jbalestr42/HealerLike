using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game
{

public class TeamFormationTests
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

    EntityData CreateUnit(string role, float maxHealth = 100f)
    {
        EntityData unit = ScriptableObject.CreateInstance<EntityData>();
        unit.attributes[AttributeType.HealthMax] = maxHealth;
        if (role != null)
        {
            GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
            tag.name = role;
            _created.Add(tag);
            unit.tags.Add(tag);
        }
        _created.Add(unit);
        return unit;
    }

    [Test]
    public void GetRowOffset_FromTheCenterOutwards()
    {
        CollectionAssert.AreEqual(new[] { 0, 1, -1, 2, -2 }, new List<int> { 0, 1, 2, 3, 4 }.ConvertAll(TeamFormation.GetRowOffset));
    }

    [Test]
    public void GetRoleRank_TanksThenSupportsThenTheOthers()
    {
        Assert.AreEqual(0, TeamFormation.GetRoleRank(CreateUnit(TagNames.Tank)));
        Assert.AreEqual(1, TeamFormation.GetRoleRank(CreateUnit(TagNames.Support)));
        Assert.AreEqual(2, TeamFormation.GetRoleRank(CreateUnit(TagNames.Damage)));
        Assert.AreEqual(2, TeamFormation.GetRoleRank(CreateUnit(null)));
    }

    [Test]
    public void GetCells_TanksOnTheRight_SupportsInTheMiddle_DamageOnTheLeft()
    {
        List<EntityData> units = new List<EntityData>
        {
            CreateUnit(TagNames.Damage),
            CreateUnit(TagNames.Support),
            CreateUnit(TagNames.Tank),
        };

        Dictionary<int, Vector2Int> cells = TeamFormation.GetCells(units, 6, 8, 5);

        Assert.AreEqual(new Vector2Int(6, 8), cells[2]);
        Assert.AreEqual(new Vector2Int(5, 8), cells[1]);
        Assert.AreEqual(new Vector2Int(4, 8), cells[0]);
    }

    [Test]
    public void GetCells_SameRole_FromTheCenterOutwards_MostHealthFirst()
    {
        List<EntityData> units = new List<EntityData>
        {
            CreateUnit(TagNames.Damage, 80f),
            CreateUnit(TagNames.Damage, 200f),
            CreateUnit(TagNames.Damage, 100f),
        };

        Dictionary<int, Vector2Int> cells = TeamFormation.GetCells(units, 6, 8, 5);

        Assert.AreEqual(new Vector2Int(6, 8), cells[1]);
        Assert.AreEqual(new Vector2Int(6, 9), cells[2]);
        Assert.AreEqual(new Vector2Int(6, 7), cells[0]);
    }

    [Test]
    public void GetCells_NoTank_TheSupportsInFront()
    {
        List<EntityData> units = new List<EntityData> { CreateUnit(TagNames.Damage), CreateUnit(TagNames.Support) };

        Dictionary<int, Vector2Int> cells = TeamFormation.GetCells(units, 6, 8, 5);

        Assert.AreEqual(new Vector2Int(6, 8), cells[1]);
        Assert.AreEqual(new Vector2Int(5, 8), cells[0]);
    }

    [Test]
    public void GetCells_FullColumn_GoesOnInTheNextOneOnTheLeft_BeforeTheNextRole()
    {
        List<EntityData> units = new List<EntityData>
        {
            CreateUnit(TagNames.Tank),
            CreateUnit(TagNames.Tank),
            CreateUnit(TagNames.Tank),
            CreateUnit(TagNames.Damage),
        };

        Dictionary<int, Vector2Int> cells = TeamFormation.GetCells(units, 6, 8, 2);

        Assert.AreEqual(new Vector2Int(6, 8), cells[0]);
        Assert.AreEqual(new Vector2Int(6, 9), cells[1]);
        Assert.AreEqual(new Vector2Int(5, 8), cells[2]);
        Assert.AreEqual(new Vector2Int(4, 8), cells[3]);
    }

    [Test]
    public void GetCells_NoUnit_NoCell()
    {
        Assert.IsEmpty(TeamFormation.GetCells(new List<EntityData>(), 6, 8, 5));
    }

    [Test]
    public void GetCells_EveryUnitPlaced_EveryCellDifferent()
    {
        List<EntityData> units = new List<EntityData>();
        for (int i = 0; i < 12; i++)
        {
            units.Add(CreateUnit(i % 3 == 0 ? TagNames.Tank : i % 3 == 1 ? TagNames.Support : TagNames.Damage));
        }

        Dictionary<int, Vector2Int> cells = TeamFormation.GetCells(units, 6, 8, 3);

        Assert.AreEqual(12, cells.Count);
        CollectionAssert.AllItemsAreUnique(cells.Values);
    }

    WavePatternData CreatePattern(int width, int height)
    {
        WavePatternData pattern = ScriptableObject.CreateInstance<WavePatternData>();
        pattern.Resize(width, height);
        _created.Add(pattern);
        return pattern;
    }

    [Test]
    public void GetPatternCells_AnX_PlacedAsDrawnAroundTheCenterRow()
    {
        WavePatternData pattern = CreatePattern(3, 3);
        EntityData dummy = CreateUnit(null);
        foreach (Vector2Int slot in new[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0), new Vector2Int(2, 2) })
        {
            pattern.SetEntity(slot.x, slot.y, dummy);
        }

        Dictionary<int, Vector2Int> cells = TeamFormation.GetPatternCells(pattern, 6, 8);

        // Corners and center of the 3x3 square spanning columns 4-6 and rows 7-9
        CollectionAssert.AreEquivalent(new[] { new Vector2Int(6, 7), new Vector2Int(6, 9), new Vector2Int(5, 8), new Vector2Int(4, 7), new Vector2Int(4, 9) }, cells.Values);
    }

    // The keys match the order of ReferenceTeam.FromWave, so each unit of the team gets the cell of its slot
    [Test]
    public void GetPatternCells_KeyedByTheIndexOfTheUnitInTheTeam()
    {
        WavePatternData pattern = CreatePattern(2, 1);
        EntityData back = CreateUnit(null);
        EntityData front = CreateUnit(null);
        pattern.SetEntity(0, 0, front);
        pattern.SetEntity(1, 0, back);

        Dictionary<int, Vector2Int> cells = TeamFormation.GetPatternCells(pattern, 6, 8);
        ReferenceTeam team = ReferenceTeam.FromWave(pattern);

        // The first column of the pattern on the front column
        Assert.AreEqual(new Vector2Int(6, 8), cells[team.units.IndexOf(front)]);
        Assert.AreEqual(new Vector2Int(5, 8), cells[team.units.IndexOf(back)]);
    }

    [Test]
    public void GetPatternCells_NoPattern_Empty()
    {
        Assert.IsEmpty(TeamFormation.GetPatternCells(null, 6, 8));
    }

    // The default board: the player on the 4 left columns of 4 rows, the enemies on the right
    BoardLayout CreateDefaultLayout()
    {
        BoardLayout layout = ScriptableObject.CreateInstance<BoardLayout>();
        _created.Add(layout);
        return layout;
    }

    [Test]
    public void GetCells_OnABoard_TheTankOnThePlayerColumnNearestTheEnemiesInTheMiddleRow()
    {
        EntityData damage = CreateUnit(TagNames.Damage);
        EntityData tank = CreateUnit(TagNames.Tank);

        Dictionary<int, Vector2Int> cells = TeamFormation.GetCells(new List<EntityData> { damage, tank }, CreateDefaultLayout());

        Assert.AreEqual(new Vector2Int(3, 1), cells[1]);
        Assert.AreEqual(new Vector2Int(2, 1), cells[0]);
    }

    [Test]
    public void GetCenterRow_EvenHeight_TheLowerMiddleRow()
    {
        Assert.AreEqual(1, TeamFormation.GetCenterRow(CreateDefaultLayout()));
    }

    // The sandbox places them as soon as the character is played: each on its own player cell
    [Test]
    public void StartingUnitsOfEverySandboxCharacter_EachOnItsOwnPlayerCell()
    {
        SandboxData sandbox = UnityEditor.AssetDatabase.LoadAssetAtPath<SandboxData>("Assets/Data/SandboxData.asset");
        BoardLayout layout = CreateDefaultLayout();
        Assert.IsNotEmpty(sandbox.characters);
        foreach (CharacterData character in sandbox.characters)
        {
            List<EntityData> units = character.entities.FindAll(unit => unit != null);
            Dictionary<int, Vector2Int> cells = TeamFormation.GetCells(units, layout);

            Assert.AreEqual(units.Count, cells.Count, character.title);
            CollectionAssert.AllItemsAreUnique(cells.Values, character.title);
            foreach (Vector2Int cell in cells.Values)
            {
                Assert.AreEqual(BoardCellType.Player, layout.GetCell(cell), $"{character.title}: {cell}");
            }
        }
    }
}

}

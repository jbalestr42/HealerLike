using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Balance
{

public class BalanceReportTests
{
    static CombatStats Fight(string wave, int floor, string bot, float manaSpent, bool won = true, string roomType = "Combat", int deaths = 0)
    {
        CombatStats fight = new CombatStats
        {
            wave = wave,
            floor = floor,
            bot = bot,
            roomType = roomType,
            won = won,
            manaMax = 100f,
            manaSpent = manaSpent,
            allyHealthMax = 500f,
            allyHealthStart = 500f,
            allyHealthEnd = 400f,
            duration = 20f,
        };
        for (int i = 0; i < deaths; i++)
        {
            fight.deadAllies.Add("Unit");
        }
        return fight;
    }

    [Test]
    public void Parse_ReadsOneFightPerLine_SkippingBlankLines()
    {
        CombatStats written = Fight("Wave_A", 3, "ClericBot", 42f);
        List<string> lines = new List<string> { JsonUtility.ToJson(written), "", JsonUtility.ToJson(written) };

        List<CombatStats> fights = BalanceReport.Parse(lines);

        Assert.AreEqual(2, fights.Count);
        Assert.AreEqual("Wave_A", fights[0].wave);
        Assert.AreEqual(3, fights[0].floor);
        Assert.AreEqual(42f, fights[0].manaSpent);
    }

    [Test]
    public void ManaTarget_AboutHalfTheManaForACombat_AllOfItForAnEliteOrABoss()
    {
        Assert.AreEqual(0.6f, BalanceReport.GetManaTarget("Combat"));
        Assert.AreEqual(1f, BalanceReport.GetManaTarget("Elite"));
        Assert.AreEqual(1f, BalanceReport.GetManaTarget("Boss"));
    }

    [Test]
    public void GetCell_AveragesTheFightsOfTheWaveOnTheFloor()
    {
        BalanceReport report = new BalanceReport(new List<CombatStats>
        {
            Fight("Wave_A", 3, "ClericBot", 40f, deaths: 1),
            Fight("Wave_A", 3, "DruidBot", 80f, won: false, deaths: 3),
            Fight("Wave_A", 4, "ClericBot", 100f),
            Fight("Wave_B", 3, "ClericBot", 100f),
        });

        BalanceReport.Cell cell = report.GetCell("Wave_A", 3);

        Assert.AreEqual(2, cell.fights);
        Assert.AreEqual(1, cell.wins);
        Assert.AreEqual(0.5f, cell.winRate);
        Assert.AreEqual(0.6f, cell.manaSpent, 0.0001f);
        Assert.AreEqual(0.2f, cell.healthLost, 0.0001f);
        Assert.AreEqual(2f, cell.deaths, 0.0001f);
        Assert.AreEqual(20f, cell.duration, 0.0001f);
    }

    [Test]
    public void GetCell_OfOneBot_KeepsOnlyItsFights()
    {
        BalanceReport report = new BalanceReport(new List<CombatStats>
        {
            Fight("Wave_A", 3, "ClericBot", 40f),
            Fight("Wave_A", 3, "DruidBot", 80f),
        });

        BalanceReport.Cell cell = report.GetCell("Wave_A", 3, "DruidBot");

        Assert.AreEqual(1, cell.fights);
        Assert.AreEqual(0.8f, cell.manaSpent, 0.0001f);
    }

    [Test]
    public void GetBotCell_AveragesTheBotFightsInTheRoomType_OnOneFloorOrAll()
    {
        BalanceReport report = new BalanceReport(new List<CombatStats>
        {
            Fight("Wave_A", 1, "ClericBot", 20f),
            Fight("Wave_B", 2, "ClericBot", 40f),
            Fight("Wave_E", 2, "ClericBot", 100f, roomType: "Elite"),
            Fight("Wave_A", 1, "DruidBot", 90f),
        });

        Assert.AreEqual(0.3f, report.GetBotCell("ClericBot", "Combat").manaSpent, 0.0001f);
        Assert.AreEqual(0.4f, report.GetBotCell("ClericBot", "Combat", 2).manaSpent, 0.0001f);
        Assert.AreEqual(1, report.GetBotCell("ClericBot", "Elite").fights);
    }

    [Test]
    public void Fits_EveryFightWon_AndTheManaWithinTheToleranceOfTheTarget()
    {
        BalanceReport.Cell onTarget = new BalanceReport.Cell { fights = 2, wins = 2, manaSpent = 0.7f };
        BalanceReport.Cell tooEasy = new BalanceReport.Cell { fights = 2, wins = 2, manaSpent = 0.3f };
        BalanceReport.Cell lost = new BalanceReport.Cell { fights = 2, wins = 1, manaSpent = 0.6f };

        Assert.IsTrue(BalanceReport.Fits(onTarget, "Combat"));
        Assert.IsFalse(BalanceReport.Fits(tooEasy, "Combat"));
        Assert.IsFalse(BalanceReport.Fits(lost, "Combat"));
        Assert.IsFalse(BalanceReport.Fits(onTarget, "Elite"));
        Assert.AreEqual(-0.3f, BalanceReport.GetManaGap(onTarget, "Elite"), 0.0001f);
    }

    [Test]
    public void RecommendedFloors_AreTheFloorsWhereTheWaveFitsForEveryBot()
    {
        BalanceReport report = new BalanceReport(new List<CombatStats>
        {
            // Floor 1 too easy, floors 2 and 3 on target, floor 4 lost by one bot
            Fight("Wave_A", 1, "ClericBot", 20f), Fight("Wave_A", 1, "DruidBot", 30f),
            Fight("Wave_A", 2, "ClericBot", 50f), Fight("Wave_A", 2, "DruidBot", 60f),
            Fight("Wave_A", 3, "ClericBot", 60f), Fight("Wave_A", 3, "DruidBot", 70f),
            Fight("Wave_A", 4, "ClericBot", 70f), Fight("Wave_A", 4, "DruidBot", 70f, won: false),
        });

        CollectionAssert.AreEqual(new List<int> { 2, 3 }, report.GetRecommendedFloors("Wave_A"));
    }

    [Test]
    public void ListsTheWavesBotsAndFloorsOfTheFights()
    {
        BalanceReport report = new BalanceReport(new List<CombatStats>
        {
            Fight("Wave_B", 5, "ClericBot", 0f),
            Fight("Wave_A", 2, "DruidBot", 0f),
            Fight("Wave_B", 2, "ClericBot", 0f),
        });

        CollectionAssert.AreEqual(new[] { "Wave_B", "Wave_A" }, report.waves);
        CollectionAssert.AreEqual(new[] { "ClericBot", "DruidBot" }, report.bots);
        CollectionAssert.AreEqual(new[] { 2, 5 }, report.floors);
        Assert.AreEqual("Combat", report.GetRoomType("Wave_A"));
    }

    [Test]
    public void FormatFloors_GroupsFollowingFloorsIntoRanges()
    {
        Assert.AreEqual("-", BalanceReport.FormatFloors(new List<int>()));
        Assert.AreEqual("4", BalanceReport.FormatFloors(new List<int> { 4 }));
        Assert.AreEqual("3-5, 7, 9-10", BalanceReport.FormatFloors(new List<int> { 9, 3, 4, 5, 7, 10 }));
    }
}

}

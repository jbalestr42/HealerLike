using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace Game.Balance
{

// The healer bots of the project data answer the telegraphed strikes of the boss like a player would: the
// anticipation spell of their character on the marked unit, right before the strike, before any other rule
public class HealerBotProfileDataTests
{
    [TestCase("ClericHealerBot", "Shield")]
    [TestCase("DruidHealerBot", "ThickBark")]
    [TestCase("WarlockHealerBot", "SoulLink")]
    public void HealerBot_FirstCastsItsAnticipationSpellOnTheMarkedUnitRightBeforeTheStrike(string botName, string skillName)
    {
        HealerBotProfile bot = AssetDatabase.LoadAssetAtPath<HealerBotProfile>($"Assets/Data/Balance/Bots/{botName}.asset");
        Assert.IsNotNull(bot, botName);

        HealerBotRule rule = bot.rules[0];

        Assert.IsNotNull(rule.skill);
        Assert.AreEqual(skillName, rule.skill.name);
        Assert.IsTrue(bot.character.skills.Contains(rule.skill), "a skill of its character");
        Assert.AreEqual(HealerBotTarget.MarkedAlly, rule.target);
        // Only waiting for the strike, and for what the spell spends (e.g. the Devotion of the Shield)
        HealerBotCondition strike = rule.conditions.Find(condition => condition.type == HealerBotConditionType.StrikeWithin);
        Assert.AreEqual(1, rule.conditions.FindAll(condition => condition.type == HealerBotConditionType.StrikeWithin).Count);
        Assert.IsTrue(rule.conditions.TrueForAll(condition => condition.type == HealerBotConditionType.StrikeWithin || condition.type == HealerBotConditionType.ResourceAtLeast));
        // Within the 3s between the mark and the strike, late enough for the shorter spells to still be on
        Assert.Greater(strike.value, 0f);
        Assert.LessOrEqual(strike.value, 1f);
    }

    static HealerBotProfile LoadCleric()
    {
        HealerBotProfile bot = AssetDatabase.LoadAssetAtPath<HealerBotProfile>("Assets/Data/Balance/Bots/ClericHealerBot.asset");
        Assert.IsNotNull(bot);
        return bot;
    }

    // Every spell of the Cleric is played, Mending Light included, but the Mark: choosing the enemy the units attack
    // first is the player's tactic, not a heal the bot measures
    [Test]
    public void ClericHealerBot_CastsEverySpellOfTheCleric()
    {
        HealerBotProfile bot = LoadCleric();

        foreach (ACharacterSkillFactory skill in bot.character.skills)
        {
            if (skill is MarkCharacterSkillFactory)
            {
                continue;
            }
            Assert.IsTrue(bot.rules.Exists(rule => rule.skill == skill), skill.name);
        }
    }

    // Shield and Judgment spend all the Devotion and scale with it: never cast with too little (0 Devotion: no armor)
    [TestCase("Shield")]
    [TestCase("Judgment")]
    public void ClericHealerBot_SpendsTheDevotionOnlyWhenThereIsSome(string skillName)
    {
        HealerBotProfile bot = LoadCleric();
        List<HealerBotRule> rules = bot.rules.FindAll(rule => rule.skill != null && rule.skill.name == skillName);

        Assert.IsNotEmpty(rules);
        foreach (HealerBotRule rule in rules)
        {
            Assert.IsTrue(rule.conditions.Exists(condition => condition.type == HealerBotConditionType.ResourceAtLeast
                && condition.resource == ResourceType.Devotion && condition.value >= 0.4f), $"{skillName}: 2 Devotion out of 5 or more");
        }
    }

    // Several wounded allies: Mending Light (one Devotion per ally healed) before the single Heal
    [Test]
    public void ClericHealerBot_MendingLightBeforeHeal_WhenSeveralAlliesAreWounded()
    {
        HealerBotProfile bot = LoadCleric();
        int mendingLight = bot.rules.FindIndex(rule => rule.skill != null && rule.skill.name == "MendingLight");
        int heal = bot.rules.FindIndex(rule => rule.skill != null && rule.skill.name == "HealSingleTarget");

        Assert.GreaterOrEqual(mendingLight, 0);
        Assert.Less(mendingLight, heal);
        HealerBotRule rule = bot.rules[mendingLight];
        Assert.AreEqual(HealerBotTarget.LowestHealthAlly, rule.target);
        Assert.IsTrue(rule.conditions.Exists(condition => condition.type == HealerBotConditionType.AlliesBelowHealth && condition.count >= 2));
    }
}

}

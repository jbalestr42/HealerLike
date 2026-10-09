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
        Assert.AreEqual(1, rule.conditions.Count);
        Assert.AreEqual(HealerBotConditionType.StrikeWithin, rule.conditions[0].type);
        // Within the 3s between the mark and the strike, late enough for the shorter spells to still be on
        Assert.Greater(rule.conditions[0].value, 0f);
        Assert.LessOrEqual(rule.conditions[0].value, 1f);
    }
}

}

using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageCompactBudgetTests
    {
        const string SheetPath = "Render/Resources/RenderSpellSpacing.uss";

        [Test]
        public void Fits_RowsExactlyAtTheBudget_True()
        {
            Assert.That(StageCompactBudget.Fits(100f, StageCompactBudget.RowsMax - 100f), Is.True);
        }

        [Test]
        public void Fits_RowsOneUnitOverTheBudget_False()
        {
            Assert.That(StageCompactBudget.Fits(100f, StageCompactBudget.RowsMax - 100f + 1f), Is.False);
        }

        [Test]
        public void Describe_AnyRows_NamesBothMeasuredHeightsAndTheirSum()
        {
            string text = StageCompactBudget.Describe(100f, 116f);

            Assert.That(text, Does.Contain("party-panel 100"));
            Assert.That(text, Does.Contain("command-dock 116"));
            Assert.That(text, Does.Contain("= 216"));
        }

        [Test]
        public void Describe_FractionalHeights_KeepOneDecimalWithAPoint()
        {
            Assert.That(StageCompactBudget.Describe(100.5f, 116f), Does.Contain("party-panel 100.5"));
        }

        // The sheet that wins on the document root is the layout the capture measures
        [Test]
        public void Fits_AuthoredRowHeightsOfTheRenderSheet_True()
        {
            string sheet = File.ReadAllText(Path.Combine(Application.dataPath, SheetPath));

            float party = RuleHeight(sheet, "#hud-root.compact-hud .party-panel");
            float dock = RuleHeight(sheet, "#hud-root.compact-hud .command-dock");

            Assert.That(StageCompactBudget.Fits(party, dock), Is.True, StageCompactBudget.Describe(party, dock));
        }

        static float RuleHeight(string sheet, string selector)
        {
            Match match = Regex.Match(sheet, Regex.Escape(selector) + @"\s*\{[^}]*?(?<![-\w])height:\s*(\d+)px");
            Assert.That(match.Success, Is.True, "No height declared for " + selector + " in " + SheetPath);
            return float.Parse(match.Groups[1].Value);
        }
    }
}

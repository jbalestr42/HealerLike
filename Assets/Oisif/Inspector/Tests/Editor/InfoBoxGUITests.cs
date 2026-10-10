using NUnit.Framework;

namespace Oisif.Editor.Tests
{
    public class InfoBoxGUITests
    {
        [Test]
        public void ToEditorRichText_QuotedColor_Unquoted()
        {
            Assert.AreEqual("Deals <color=red>12</color> damage", InfoBoxGUI.ToEditorRichText("Deals <color=\"red\">12</color> damage"));
        }

        [Test]
        public void ToEditorRichText_HexColor_Unchanged()
        {
            Assert.AreEqual("Cost: <color=#8AB5FF>8</color> mana", InfoBoxGUI.ToEditorRichText("Cost: <color=#8AB5FF>8</color> mana"));
        }

        [Test]
        public void ToEditorRichText_EveryQuotedTag()
        {
            Assert.AreEqual("<color=green>6s</color> and <size=12>small</size>", InfoBoxGUI.ToEditorRichText("<color=\"green\">6s</color> and <size=\"12\">small</size>"));
        }

        [Test]
        public void ToEditorRichText_QuotesOutsideOfTags_Unchanged()
        {
            Assert.AreEqual("A \"quoted\" word, <b>bold</b>", InfoBoxGUI.ToEditorRichText("A \"quoted\" word, <b>bold</b>"));
        }
    }
}

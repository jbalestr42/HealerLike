using NUnit.Framework;

namespace UI.Toolkit
{

    // What the Toolkit event panel reads back out of the choice buttons of Julien's EventView
    public class ToolkitEventChoiceTextTests
    {
        static string Baked(string label, string description)
        {
            return EventView.GetChoiceText(new EventChoice { label = label, description = description });
        }

        [Test]
        public void Label_ChoiceObjectName_IsTheNameAfterThePrefix()
        {
            Assert.AreEqual("Heal", ToolkitEventChoiceText.Label("Choice Heal", Baked("Heal", "d")));
        }

        [Test]
        public void Label_NameWithoutThePrefix_IsTheFirstLineOfTheText()
        {
            Assert.AreEqual("Heal", ToolkitEventChoiceText.Label("Button", Baked("Heal", "Every unit recovers")));
        }

        [Test]
        public void Label_NothingToRead_IsEmpty()
        {
            Assert.AreEqual(string.Empty, ToolkitEventChoiceText.Label("Button", null));
        }

        [Test]
        public void Description_HisFormatterWithADescription_IsThePlainDescription()
        {
            Assert.AreEqual("Every unit recovers 30% of its max health.",
                ToolkitEventChoiceText.Description("Heal", Baked("Heal", "Every unit recovers 30% of its max health.")));
        }

        [Test]
        public void Description_HisFormatterWithoutADescription_IsEmpty()
        {
            Assert.AreEqual(string.Empty, ToolkitEventChoiceText.Description("Back", Baked("Back", null)));
        }

        [Test]
        public void Description_MultilineDescription_KeepsItsLines()
        {
            Assert.AreEqual("Zealot\nHits harder when healthy",
                ToolkitEventChoiceText.Description("Zealot", Baked("Zealot", "Zealot\nHits harder when healthy")));
        }

        [Test]
        public void Description_RelativeSizeTagInside_IsRemoved()
        {
            string text = Baked("Zealot", "<size=+6>Zealot</size>\nHits harder");

            string description = ToolkitEventChoiceText.Description("Zealot", text);

            StringAssert.DoesNotContain("<size", description);
            StringAssert.Contains("Hits harder", description);
        }

        [Test]
        public void Description_FormatWithoutTheBoldLabel_DropsTheLabelLineOnly()
        {
            Assert.AreEqual("Take nothing.", ToolkitEventChoiceText.Description("Leave", "Leave\nTake nothing."));
        }

        [Test]
        public void Normalise_NullOrEmpty_IsEmpty()
        {
            Assert.AreEqual(string.Empty, ToolkitEventChoiceText.Normalise(null));
            Assert.AreEqual(string.Empty, ToolkitEventChoiceText.Normalise(string.Empty));
        }

        [Test]
        public void Normalise_BoldAndColorTags_AreKept()
        {
            Assert.AreEqual("<b>a</b> <color=#fff>b</color>",
                ToolkitEventChoiceText.Normalise("<b>a</b> <color=#fff>b</color>"));
        }
    }
}

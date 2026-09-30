using NUnit.Framework;

namespace UI
{

// The panel shown when a unit is clicked in game: its name, then its description
public class PanelEntityTests
{
    [Test]
    public void FormatDescription_ShowsTheNameInBoldThenTheDescription()
    {
        Assert.AreEqual("<b>Treant</b>\nTaunts the enemies around it.", PanelEntity.FormatDescription("Treant", "Taunts the enemies around it."));
    }

    [Test]
    public void FormatDescription_WithoutDescription_ShowsOnlyTheName()
    {
        Assert.AreEqual("<b>Treant</b>", PanelEntity.FormatDescription("Treant", ""));
        Assert.AreEqual("<b>Treant</b>", PanelEntity.FormatDescription("Treant", null));
    }
}

}

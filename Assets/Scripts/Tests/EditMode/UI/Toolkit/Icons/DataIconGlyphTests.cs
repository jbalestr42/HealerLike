using NUnit.Framework;

namespace UI.Toolkit.Icons
{

public class DataIconGlyphTests
{
    [Test]
    public void Contains_HealCenter_ReturnsTrue()
    {
        Assert.IsTrue(DataIconGlyph.Contains(DataIconKind.Spell, DataIconSymbol.Heal, 0f, 0f));
    }

    [Test]
    public void Contains_HealBetweenArms_ReturnsFalse()
    {
        Assert.IsFalse(DataIconGlyph.Contains(DataIconKind.Spell, DataIconSymbol.Heal, 0.3f, 0.3f));
    }

    [Test]
    public void Contains_GenericSymbol_FallsBackToKindEmblem()
    {
        // The spell emblem is a diamond ring: its band holds (0.4, 0), the data emblem's square ring does not
        Assert.IsTrue(DataIconGlyph.Contains(DataIconKind.Spell, DataIconSymbol.Generic, 0.4f, 0f));
        Assert.IsFalse(DataIconGlyph.Contains(DataIconKind.Data, DataIconSymbol.Generic, 0.4f, 0f));
    }
}
}

using NUnit.Framework;

namespace Game
{

// The kind of each reward choice, drawn by chance
public class RewardChoiceTests
{
    [TestCase(0.1f, 0.9f, RewardChoiceType.Unit)]
    [TestCase(0.3f, 0.1f, RewardChoiceType.PlayerItem)]
    [TestCase(0.3f, 0.9f, RewardChoiceType.EntityItem)]
    public void PickChoiceType_AUnitFirst_ThenAPlayerItem_OtherwiseAnEntityItem(float unitRoll, float itemRoll, RewardChoiceType expected)
    {
        // 25% of units, then half of the items for the player
        Assert.AreEqual(expected, RewardChoice.PickChoiceType(unitRoll, itemRoll, 0.25f, 0.5f, true));
    }

    [Test]
    public void PickChoiceType_WithoutUnitToOffer_IsAnItem()
    {
        Assert.AreEqual(RewardChoiceType.PlayerItem, RewardChoice.PickChoiceType(0f, 0.1f, 1f, 0.5f, false));
        Assert.AreEqual(RewardChoiceType.EntityItem, RewardChoice.PickChoiceType(0f, 0.9f, 1f, 0.5f, false));
    }

    [Test]
    public void PickChoiceType_WithoutUnitChance_NeverOffersAUnit()
    {
        Assert.AreNotEqual(RewardChoiceType.Unit, RewardChoice.PickChoiceType(0f, 0.9f, 0f, 0.5f, true));
    }

    [Test]
    public void ExcludedTags_AreTheEventOnlyItems()
    {
        // Library and cursed items only come from events
        CollectionAssert.AreEquivalent(new[] { TagNames.Cursed, TagNames.Library }, RewardChoice.ExcludedTags);
    }
}

}

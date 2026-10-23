using NUnit.Framework;

namespace Game.Events
{

// The choices of an event: one pick only, never an unavailable choice
public class EventPromptTests
{
    [Test]
    public void Select_AvailableChoice_AppliesIt()
    {
        int picked = 0;
        EventChoice heal = new EventChoice { label = "Heal", onSelected = () => picked++ };
        EventPrompt prompt = new EventPrompt("Rest", "", new[] { heal });

        Assert.IsTrue(prompt.Select(heal));

        Assert.AreEqual(1, picked);
        Assert.IsTrue(prompt.hasChosen);
    }

    [Test]
    public void Select_Twice_AppliesTheFirstPickOnly()
    {
        int picked = 0;
        EventChoice heal = new EventChoice { label = "Heal", onSelected = () => picked++ };
        EventChoice leave = new EventChoice { label = "Leave", onSelected = () => picked += 10 };
        EventPrompt prompt = new EventPrompt("Rest", "", new[] { heal, leave });

        prompt.Select(heal);

        Assert.IsFalse(prompt.Select(leave));
        Assert.AreEqual(1, picked);
    }

    [Test]
    public void Select_UnavailableChoice_AppliesNothingAndKeepsTheChoiceOpen()
    {
        int picked = 0;
        EventChoice resurrect = new EventChoice { label = "Resurrect", isAvailable = false, onSelected = () => picked++ };
        EventPrompt prompt = new EventPrompt("Rest", "", new[] { resurrect });

        Assert.IsFalse(prompt.Select(resurrect));

        Assert.AreEqual(0, picked);
        Assert.IsFalse(prompt.hasChosen);
    }

    [Test]
    public void Select_ChoiceOfAnotherEvent_AppliesNothing()
    {
        int picked = 0;
        EventPrompt prompt = new EventPrompt("Rest", "", new[] { new EventChoice { label = "Heal" } });

        Assert.IsFalse(prompt.Select(new EventChoice { label = "Heal", onSelected = () => picked++ }));

        Assert.AreEqual(0, picked);
    }
}

}

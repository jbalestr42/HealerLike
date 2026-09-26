using NUnit.Framework;

namespace UI.Toolkit
{

public class ToolkitGameContextTests
{
    [Test]
    public void IsPreparing_NoAscension_ReturnsTrue()
    {
        ToolkitGameContext context = new ToolkitGameContext();
        Assert.IsTrue(context.IsPreparing());
    }

    [Test]
    public void IsCurrentView_Menu_ReturnsFalse()
    {
        ToolkitGameContext context = new ToolkitGameContext();
        context.isMenu = true;
        Assert.IsFalse(context.IsCurrentView(ViewType.Map));
    }

    [Test]
    public void HasInteraction_NoInteractionManager_ReturnsFalse()
    {
        ToolkitGameContext context = new ToolkitGameContext();
        Assert.IsFalse(context.hasInteraction);
    }
}
}

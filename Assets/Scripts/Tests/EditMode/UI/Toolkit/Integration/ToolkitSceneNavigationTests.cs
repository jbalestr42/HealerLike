using NUnit.Framework;

namespace UI.Toolkit.Integration
{

public class ToolkitSceneNavigationTests
{
    [Test]
    public void TryLoad_UnknownScene_ReturnsFalse()
    {
        bool isLoaded = ToolkitSceneNavigation.TryLoad("NoSuchToolkitScene");

        Assert.IsFalse(isLoaded);
    }
}
}

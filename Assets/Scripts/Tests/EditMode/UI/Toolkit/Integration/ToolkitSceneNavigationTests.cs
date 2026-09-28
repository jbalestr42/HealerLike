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

    [Test]
    public void GetEditorPath_Sandbox_IsJuliensSandboxScene()
    {
        Assert.AreEqual("Assets/Scenes/Sandbox.unity", ToolkitSceneNavigation.GetEditorPath("Sandbox"));
    }

    [Test]
    public void GetEditorPath_ToolkitScenes_KeepTheirPaths()
    {
        Assert.AreEqual(ToolkitSceneNavigation.MenuPath, ToolkitSceneNavigation.GetEditorPath("MenuToolkit"));
        Assert.AreEqual(ToolkitSceneNavigation.GameplayPath, ToolkitSceneNavigation.GetEditorPath("MainToolkit"));
        Assert.IsNull(ToolkitSceneNavigation.GetEditorPath("NoSuchToolkitScene"));
    }
}
}

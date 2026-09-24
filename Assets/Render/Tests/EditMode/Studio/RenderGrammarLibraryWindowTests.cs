using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{

public class RenderGrammarLibraryWindowTests
{
    RenderGrammarLibraryWindow _window;
    UnityEditor.Editor _inspector;

    [TearDown]
    public void TearDown()
    {
        if (_inspector != null)
        {
            Object.DestroyImmediate(_inspector);
        }

        if (_window != null)
        {
            Object.DestroyImmediate(_window);
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void CreateNativeInspector_ShippedAsset_DrawsItWithOdin(int index)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(RenderGrammarLibraryWindow.AssetPaths[index]);

        _inspector = RenderGrammarLibraryWindow.CreateNativeInspector(asset);

        Assert.NotNull(asset, RenderGrammarLibraryWindow.AssetPaths[index]);
        Assert.IsNotEmpty(RenderGrammarLibraryWindow.Summary(asset));
        StringAssert.Contains("Odin", _inspector.GetType().FullName); // the dictionaries stay editable
    }

    [Test]
    public void SelectAsset_ShippedVocabularyThenNone_LeavesTheAssetClean()
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(RenderGrammarLibraryWindow.AssetPaths[0]);
        bool isDirty = EditorUtility.IsDirty(asset);
        _window = ScriptableObject.CreateInstance<RenderGrammarLibraryWindow>();

        _window.SelectAsset(asset);
        Assert.AreSame(asset, _window.selectedAsset);
        _window.SelectAsset(null);

        Assert.IsNull(_window.selectedAsset);
        Assert.AreEqual(isDirty, EditorUtility.IsDirty(asset));
    }

    [Test]
    public void Summary_NoAsset_SaysSo()
    {
        Assert.AreEqual("No asset selected", RenderGrammarLibraryWindow.Summary(null));
    }
}

}

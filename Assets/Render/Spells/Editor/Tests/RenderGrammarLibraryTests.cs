using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells.Editor.Studio;

namespace HealerLike.Render.Spells.Editor.Tests
{
    public class RenderGrammarLibraryTests
    {
        [Test]
        public void CatalogUsesRealRendererAssetsAndNativeDictionaryInspectors()
        {
            foreach (string path in RenderGrammarLibraryWindow.AssetPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                Assert.IsNotNull(asset, path);
                Assert.IsNotEmpty(RenderGrammarLibraryWindow.Summary(asset));
                var inspector = RenderGrammarLibraryWindow.CreateNativeInspector(asset);
                try
                {
                    Assert.IsNotNull(inspector);
                    if (path.Contains("Vocabulary") || path.Contains("Looks"))
                        Assert.That(inspector.GetType().FullName, Does.Contain("Odin"),
                            "The native dictionaries must not disappear behind a default SerializedObject inspector: " + path);
                }
                finally { Object.DestroyImmediate(inspector); }
            }
        }

        [Test]
        public void SelectingAssetDoesNotDirtySharedRendererData()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(RenderGrammarLibraryWindow.AssetPaths[0]);
            bool dirty = EditorUtility.IsDirty(asset);
            var window = ScriptableObject.CreateInstance<RenderGrammarLibraryWindow>();
            try
            {
                window.SelectAsset(asset);
                Assert.AreSame(asset, window.SelectedAsset);
                window.SelectAsset(null);
                Assert.IsNull(window.SelectedAsset);
                Assert.AreEqual(dirty, EditorUtility.IsDirty(asset));
            }
            finally { Object.DestroyImmediate(window); }
        }
    }
}

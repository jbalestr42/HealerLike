using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public class StageCaptureThemeTests
    {
        PanelSettings _panel;
        StyleSheet _local;
        VisualElement _root;
        ThemeStyleSheet _forest;
        ThemeStyleSheet _moon;

        [SetUp]
        public void SetUp()
        {
            _forest = StageCaptureTheme.Resolve(null);
            _moon = StageCaptureTheme.Resolve("Assets/Resources/UI/Toolkit/MoonTheme.tss");
            _panel = ScriptableObject.CreateInstance<PanelSettings>();
            _panel.themeStyleSheet = _forest;
            _local = ScriptableObject.CreateInstance<StyleSheet>();
            _root = new VisualElement();
            _root.styleSheets.Add(_local);
            ToolkitTheme.Apply(_root, _forest);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_local);
            Object.DestroyImmediate(_panel);
        }

        [Test]
        public void Resolve_AbsentOption_UsesForestAndRejectsMissingOrWrongAssetTypes()
        {
            Assert.AreEqual("Assets/Resources/UI/Toolkit/RuntimeTheme.tss", AssetDatabase.GetAssetPath(_forest));
            Assert.Throws<ArgumentException>(() => StageCaptureTheme.Resolve("Assets/MissingTheme.tss"));
            Assert.Throws<ArgumentException>(() => StageCaptureTheme.Resolve(
                "Assets/Resources/UI/Toolkit/GameUI.uxml"));
        }

        [Test]
        public void Dispose_FailedCapture_RestoresPanelAndRootWhilePreservingLocalStyles()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (StageCaptureTheme scope = new StageCaptureTheme(_panel, _root, _moon))
                {
                    Assert.AreSame(_moon, _panel.themeStyleSheet);
                    Assert.IsTrue(_root.styleSheets.Contains(_moon));
                    Assert.IsFalse(_root.styleSheets.Contains(_forest));
                    throw new InvalidOperationException("Capture failed after applying Moon");
                }
            });

            Assert.AreSame(_forest, _panel.themeStyleSheet);
            Assert.IsTrue(_root.styleSheets.Contains(_forest));
            Assert.IsFalse(_root.styleSheets.Contains(_moon));
            Assert.IsTrue(_root.styleSheets.Contains(_local));
            Assert.AreEqual(2, _root.styleSheets.count);
        }

        [Test]
        public void Dispose_RepeatedOrNestedScopes_RestoresTheThemeEachScopeBorrowed()
        {
            StageCaptureTheme outer = new StageCaptureTheme(_panel, _root, _moon);
            try
            {
                using (StageCaptureTheme inner = new StageCaptureTheme(_panel, _root, _forest))
                {
                    Assert.AreSame(_forest, _panel.themeStyleSheet);
                }

                Assert.AreSame(_moon, _panel.themeStyleSheet);
                Assert.IsTrue(_root.styleSheets.Contains(_moon));
            }
            finally
            {
                outer.Dispose();
                outer.Dispose();
            }

            Assert.AreSame(_forest, _panel.themeStyleSheet);
            Assert.AreEqual(2, _root.styleSheets.count);
        }

        [Test]
        public void Describe_ActualPanelTheme_RecordsSavedAssetIdentity()
        {
            using (StageCaptureTheme scope = new StageCaptureTheme(_panel, _root, _moon))
            {
                StageCaptureTheme.Identity identity = StageCaptureTheme.Describe(_panel.themeStyleSheet);
                Assert.AreEqual(_moon.name, identity.name);
                Assert.AreEqual("Assets/Resources/UI/Toolkit/MoonTheme.tss", identity.path);
                Assert.AreEqual(AssetDatabase.AssetPathToGUID(identity.path), identity.guid);
                Assert.IsNotEmpty(identity.guid);
            }
        }

        [Test]
        public void Dispose_PreviouslyUnthemedRoot_RemovesOnlyTheBorrowedThemeClassAndSheet()
        {
            _root.styleSheets.Remove(_forest);
            _root.RemoveFromClassList("toolkit-theme");
            using (StageCaptureTheme scope = new StageCaptureTheme(_panel, _root, _moon))
            {
                Assert.IsTrue(_root.ClassListContains("toolkit-theme"));
            }

            Assert.IsFalse(_root.ClassListContains("toolkit-theme"));
            Assert.IsTrue(_root.styleSheets.Contains(_local));
            Assert.AreEqual(1, _root.styleSheets.count);
        }
    }
}

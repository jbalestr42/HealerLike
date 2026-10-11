using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    public class AssetSectionTests
    {
        const string Folder = "Assets/Oisif/Inspector/Tests/TempAssetSection";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.CreateFolder("Assets/Oisif/Inspector/Tests", "TempAssetSection");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
        }

        static TestBrowserData CreateAsset(string path, string label)
        {
            TestBrowserData asset = ScriptableObject.CreateInstance<TestBrowserData>();
            asset.label = label;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        [Test]
        public void FindAssets_TheAssetsOfTheTypeInTheFolderAndBelow_ByName()
        {
            CreateAsset($"{Folder}/Zeta.asset", "z");
            CreateAsset($"{Folder}/Sub/Alpha.asset", "a");

            List<ScriptableObject> assets = new AssetSection("Test", Folder, typeof(TestBrowserData)).FindAssets();

            CollectionAssert.AreEqual(new[] { "Alpha", "Zeta" }, assets.Select(asset => asset.name));
        }

        [Test]
        public void FindAssets_NotRecursive_OnlyTheFolder()
        {
            CreateAsset($"{Folder}/Zeta.asset", "z");
            CreateAsset($"{Folder}/Sub/Alpha.asset", "a");

            List<ScriptableObject> assets = new AssetSection("Test", Folder, typeof(TestBrowserData)) { isRecursive = false }.FindAssets();

            CollectionAssert.AreEqual(new[] { "Zeta" }, assets.Select(asset => asset.name));
        }

        [Test]
        public void FindAssets_OnlyTheOnesTheFilterAccepts()
        {
            CreateAsset($"{Folder}/Kept.asset", "keep");
            CreateAsset($"{Folder}/Left.asset", "leave");
            AssetSection section = new AssetSection("Test", Folder, typeof(TestBrowserData)) { filter = asset => ((TestBrowserData)asset).label == "keep" };

            CollectionAssert.AreEqual(new[] { "Kept" }, section.FindAssets().Select(asset => asset.name));
        }

        [Test]
        public void FindAssets_NoSuchFolder_Nothing()
        {
            Assert.IsEmpty(new AssetSection("Test", Folder + "/Missing", typeof(TestBrowserData)).FindAssets());
        }

        [Test]
        public void SavePath_InAFolderOfItsName_OrDirectlyInTheSectionFolder()
        {
            AssetSection section = new AssetSection("Test", Folder + "/", typeof(TestBrowserData));

            Assert.AreEqual($"{Folder}/Fire Ball/Fire Ball.asset", section.GetSavePath("Fire/Ball"));
            section.createFolder = false;
            Assert.AreEqual($"{Folder}/Fire Ball.asset", section.GetSavePath("Fire/Ball"));
        }

        [Test]
        public void CreateDraft_OfTheSectionType_FilledByInitDraft()
        {
            AssetSection section = new AssetSection("Test", Folder, typeof(TestBrowserData)) { initDraft = draft => ((TestBrowserData)draft).label = "filled" };

            TestBrowserData draft = (TestBrowserData)section.CreateDraft();
            try
            {
                Assert.AreEqual("filled", draft.label);
                Assert.IsFalse(AssetDatabase.Contains(draft), "not saved yet");
            }
            finally
            {
                Object.DestroyImmediate(draft);
            }
        }

        [Test]
        public void Save_AtThePathOfItsName_NeverOverwritingAnother()
        {
            AssetSection section = new AssetSection("Test", Folder, typeof(TestBrowserData));

            ScriptableObject first = section.Save(ScriptableObject.CreateInstance<TestBrowserData>(), "Thing");
            ScriptableObject second = section.Save(ScriptableObject.CreateInstance<TestBrowserData>(), "Thing");

            Assert.AreEqual($"{Folder}/Thing/Thing.asset", AssetDatabase.GetAssetPath(first));
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(first), AssetDatabase.GetAssetPath(second));
            Assert.IsTrue(AssetDatabase.GetAssetPath(second).StartsWith($"{Folder}/Thing/"));
        }
    }
}

using System.Collections.Generic;
using Oisif.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    public class DataAssetsTests
    {
        public abstract class TestBaseData : ScriptableObject {}
        public class TestFirstData : TestBaseData {}
        public class TestSecondData : TestBaseData {}
        public abstract class TestAbstractData : TestBaseData {}
        public class TestGenericData<T> : TestBaseData {}

        [Test]
        public void CreatableTypes_TheConcreteDerivedOnes_ByName()
        {
            List<System.Type> types = DataAssets.GetCreatableTypes(typeof(TestBaseData));

            CollectionAssert.AreEqual(new[] { typeof(TestFirstData), typeof(TestSecondData) }, types);
        }

        [Test]
        public void CreatableTypes_AConcreteTypeItself()
        {
            CollectionAssert.AreEqual(new[] { typeof(TestFirstData) }, DataAssets.GetCreatableTypes(typeof(TestFirstData)));
        }

        [TestCase(typeof(TestFirstData), "Test First Data")]
        [TestCase(typeof(DataAssetsTests), "Data Assets Tests")]
        public void NiceName_WordsSplitOnCapitals(System.Type type, string expected)
        {
            Assert.AreEqual(expected, DataAssets.GetNiceName(type));
        }

        [Test]
        public void Folder_WithoutHostNorSelection_Assets()
        {
            UnityEditor.Selection.activeObject = null;

            Assert.AreEqual("Assets", DataAssets.GetFolder(null));
        }
    }
}

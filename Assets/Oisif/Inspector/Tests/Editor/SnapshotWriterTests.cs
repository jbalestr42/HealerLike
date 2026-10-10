using System;
using System.Collections.Generic;
using Oisif.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    public class SnapshotWriterTests
    {
        // Marks a private field as saved by another serializer
        [AttributeUsage(AttributeTargets.Field)]
        class OtherSerializeAttribute : Attribute {}

        class SerializationData
        {
            public string nodes = "raw";
        }

        [Serializable]
        class Inner
        {
            public int amount;
        }

        class DerivedInner : Inner
        {
            public string extra = "x";
        }

        class Holder
        {
            public int number = 3;
            public float ratio = 0.5f;
            public string text = "a\nb";
            public Vector3 position = new Vector3(1f, 2f, 3f);
            public Inner inner = new Inner { amount = 7 };
            public Inner polymorphic = new DerivedInner { amount = 1 };
            public List<int> list = new List<int> { 4, 5 };
            public Dictionary<string, int> dictionary = new Dictionary<string, int>();
            public int[,] grid = new int[2, 3];
            public Holder self;
            public ScriptableObject reference;
            [SerializeField] int _saved = 1;
            int _notSaved = 2;
            [NonSerialized] public int publicNotSaved = 3;
            [OtherSerialize] int _savedByOther = 4;
            public SerializationData rawData = new SerializationData();

            public int Sum() => _saved + _notSaved + _savedByOther;
        }

        SnapshotWriter _writer;

        [SetUp]
        public void SetUp()
        {
            _writer = new SnapshotWriter();
            _writer.serializedAttributeNames.Add(nameof(OtherSerializeAttribute));
            _writer.ignoredTypeNames.Add(nameof(SerializationData));
        }

        [Test]
        public void SimpleValues_OneLineEach_Invariant()
        {
            List<string> lines = _writer.Write(new Holder());

            CollectionAssert.Contains(lines, "number = 3");
            CollectionAssert.Contains(lines, "ratio = 0.5");
            CollectionAssert.Contains(lines, "text = \"a\\nb\"");
            CollectionAssert.Contains(lines, "position = (1, 2, 3)");
        }

        [Test]
        public void Vectors_KeepEveryDigit()
        {
            Holder holder = new Holder { position = new Vector3(0.123456f, 0f, 0f) };

            CollectionAssert.Contains(_writer.Write(holder), "position = (0.123456, 0, 0)");
        }

        [Test]
        public void NestedObjects_ThroughTheirFields_WithTheirConcreteType()
        {
            List<string> lines = _writer.Write(new Holder());

            CollectionAssert.Contains(lines, "inner : Inner");
            CollectionAssert.Contains(lines, "inner.amount = 7");
            CollectionAssert.Contains(lines, "polymorphic : DerivedInner");
            CollectionAssert.Contains(lines, "polymorphic.amount = 1");
            CollectionAssert.Contains(lines, "polymorphic.extra = \"x\"");
        }

        [Test]
        public void Lists_CountThenEachElement()
        {
            List<string> lines = _writer.Write(new Holder());

            CollectionAssert.Contains(lines, "list.Count = 2");
            CollectionAssert.Contains(lines, "list[0] = 4");
            CollectionAssert.Contains(lines, "list[1] = 5");
        }

        [Test]
        public void Dictionaries_SameEntriesInAnyOrder_SameLines()
        {
            Holder first = new Holder();
            first.dictionary["b"] = 2;
            first.dictionary["a"] = 1;
            Holder second = new Holder();
            second.dictionary["a"] = 1;
            second.dictionary["b"] = 2;

            List<string> lines = _writer.Write(first);

            CollectionAssert.AreEqual(lines, _writer.Write(second));
            CollectionAssert.Contains(lines, "dictionary.Count = 2");
            // Keys written like values: a text in quotes
            CollectionAssert.Contains(lines, "dictionary[\"a\"] = 1");
        }

        [Test]
        public void MultiDimensionalArrays_SizeThenEachCellByItsIndices()
        {
            Holder holder = new Holder();
            holder.grid[1, 2] = 9;

            List<string> lines = _writer.Write(holder);

            CollectionAssert.Contains(lines, "grid.Size = 2x3");
            CollectionAssert.Contains(lines, "grid[1,2] = 9");
            CollectionAssert.Contains(lines, "grid[0,0] = 0");
        }

        [Test]
        public void SavedFieldsOnly()
        {
            List<string> lines = _writer.Write(new Holder());

            CollectionAssert.Contains(lines, "_saved = 1");
            CollectionAssert.Contains(lines, "_savedByOther = 4");
            Assert.IsFalse(lines.Exists(line => line.StartsWith("_notSaved")));
            Assert.IsFalse(lines.Exists(line => line.StartsWith("publicNotSaved")));
        }

        [Test]
        public void IgnoredTypes_LeftOut()
        {
            Assert.IsFalse(_writer.Write(new Holder()).Exists(line => line.StartsWith("rawData")));
        }

        [Test]
        public void Cycles_WrittenOnce()
        {
            Holder holder = new Holder();
            holder.self = holder;

            CollectionAssert.Contains(_writer.Write(holder), "self = <cycle>");
        }

        [Test]
        public void UnityReferences_DescribedNotEntered_NullWhenMissing()
        {
            Holder holder = new Holder { reference = ScriptableObject.CreateInstance<ScriptableObject>() };
            holder.reference.name = "Referenced";
            _writer.describeReference = obj => "described " + obj.name;
            try
            {
                CollectionAssert.Contains(_writer.Write(holder), "reference = &described Referenced (ScriptableObject)");

                UnityEngine.Object.DestroyImmediate(holder.reference);
                CollectionAssert.Contains(_writer.Write(holder), "reference = null");
            }
            finally
            {
                if (holder.reference != null)
                {
                    UnityEngine.Object.DestroyImmediate(holder.reference);
                }
            }
        }

        [Test]
        public void Prefix_StartsEveryPath()
        {
            CollectionAssert.Contains(_writer.Write(new Holder(), "root"), "root.number = 3");
        }

        [Test]
        public void Diff_TheLinesOnlyOnOneSide()
        {
            List<string> diff = DataSnapshot.Diff(new[] { "a = 1", "b = 2" }, new[] { "b = 2", "a = 3" });

            CollectionAssert.AreEqual(new[] { "- a = 1", "+ a = 3" }, diff);
            Assert.IsEmpty(DataSnapshot.Diff(new[] { "x", "y" }, new[] { "y", "x" }));
        }
    }
}

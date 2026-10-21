using System.Collections.Generic;
using NUnit.Framework;
using Oisif.Inspector;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    public class SerializableDictionaryTests
    {
        public enum Kind
        {
            First,
            Second,
            Third,
        }

        [System.Serializable]
        public class Payload
        {
            public int amount;
        }

        class Holder : ScriptableObject
        {
            public SerializableDictionary<Kind, Payload> payloads = new SerializableDictionary<Kind, Payload>();
            public SerializableDictionary<string, float> scales = new SerializableDictionary<string, float>();
        }

        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        Holder CreateHolder()
        {
            Holder holder = ScriptableObject.CreateInstance<Holder>();
            _created.Add(holder);
            return holder;
        }

        Holder Copy(Holder source)
        {
            Holder copy = Object.Instantiate(source);
            _created.Add(copy);
            return copy;
        }

        [Test]
        public void Copy_KeepsEveryPairWithItsValue()
        {
            Holder holder = CreateHolder();
            holder.payloads[Kind.First] = new Payload { amount = 3 };
            holder.payloads[Kind.Third] = new Payload { amount = 7 };
            holder.scales["Heavy"] = 1.3f;

            Holder copy = Copy(holder);

            Assert.AreEqual(2, copy.payloads.Count);
            Assert.AreEqual(3, copy.payloads[Kind.First].amount);
            Assert.AreEqual(7, copy.payloads[Kind.Third].amount);
            Assert.IsFalse(copy.payloads.ContainsKey(Kind.Second));
            Assert.AreEqual(1.3f, copy.scales["Heavy"]);
        }

        [Test]
        public void Copy_AfterAPairIsRemovedByCode_LeavesItOut()
        {
            Holder holder = CreateHolder();
            holder.payloads[Kind.First] = new Payload { amount = 1 };
            holder.payloads[Kind.Second] = new Payload { amount = 2 };
            Holder saved = Copy(holder);

            saved.payloads.Remove(Kind.First);
            Holder copy = Copy(saved);

            Assert.AreEqual(1, copy.payloads.Count);
            Assert.AreEqual(2, copy.payloads[Kind.Second].amount);
        }

        [Test]
        public void OnAfterDeserialize_DuplicateKey_KeepsTheFirstEntryAndTheEntryList()
        {
            SerializableDictionary<string, float> dictionary = new SerializableDictionary<string, float>();
            dictionary["Light"] = 1f;
            dictionary.OnBeforeSerialize();
            List<SerializableDictionary<string, float>.Entry> entries = new List<SerializableDictionary<string, float>.Entry>(dictionary.entries)
            {
                // What the inspector does when a new element is added to the list: a copy of the last one
                new SerializableDictionary<string, float>.Entry("Light", 2f),
            };
            TestEntries(dictionary, entries);

            dictionary.OnAfterDeserialize();
            dictionary.OnBeforeSerialize();

            Assert.AreEqual(1f, dictionary["Light"]);
            Assert.AreEqual(2, dictionary.entries.Count, "The entry being edited is not dropped");
        }

        [Test]
        public void OnAfterDeserialize_NullKey_IsLeftOutOfTheDictionary()
        {
            SerializableDictionary<string, float> dictionary = new SerializableDictionary<string, float>();
            TestEntries(dictionary, new List<SerializableDictionary<string, float>.Entry>
            {
                new SerializableDictionary<string, float>.Entry(null, 1f),
                new SerializableDictionary<string, float>.Entry("Solid", 1.15f),
            });

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(1.15f, dictionary["Solid"]);
        }

        [Test]
        public void Constructor_FromADictionary_HoldsItsPairs()
        {
            Dictionary<Kind, float> source = new Dictionary<Kind, float> { { Kind.First, 1f }, { Kind.Second, 2f } };

            SerializableDictionary<Kind, float> dictionary = new SerializableDictionary<Kind, float>(source);

            Assert.AreEqual(2, dictionary.Count);
            Assert.AreEqual(2f, dictionary[Kind.Second]);
        }

        // The serialized list, as Unity fills it before OnAfterDeserialize
        static void TestEntries<TKey, TValue>(SerializableDictionary<TKey, TValue> dictionary,
            List<SerializableDictionary<TKey, TValue>.Entry> entries)
        {
            typeof(SerializableDictionary<TKey, TValue>)
                .GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(dictionary, entries);
        }
    }
}

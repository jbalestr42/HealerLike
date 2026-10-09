using System;
using System.Collections.Generic;
using Oisif.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    public class PropertyReflectionTests
    {
        [Serializable]
        class Part
        {
            public int amount = 2;
        }

        [Serializable]
        class BigPart : Part
        {
            public string extra = "x";
        }

        class Root
        {
            public Part part = new Part();
            public List<Part> parts = new List<Part> { new Part { amount = 5 }, new BigPart { amount = 6 } };
            [SerializeField] float _hidden = 1.5f;
            public bool isOn = true;
            int count => 3;
            string Describe() => "described";

            public float hidden => _hidden;
        }

        [Test]
        public void Find_AField_ItsOwnerAndValue()
        {
            Root root = new Root();

            PropertyInfo info = PropertyReflection.Find(root, "part.amount");

            Assert.AreEqual("amount", info.field.Name);
            Assert.AreSame(root.part, info.owner);
            Assert.AreEqual(2, info.value);
        }

        [Test]
        public void Find_AnElement_ThenTheFieldOfItsActualType()
        {
            Root root = new Root();

            PropertyInfo element = PropertyReflection.Find(root, "parts.Array.data[1]");
            PropertyInfo extra = PropertyReflection.Find(root, "parts.Array.data[1].extra");

            Assert.IsNull(element.field);
            Assert.AreSame(root.parts, element.owner);
            Assert.AreEqual(typeof(BigPart), element.type);
            Assert.AreEqual("x", extra.value);
            Assert.AreSame(root.parts[1], extra.owner);
        }

        [Test]
        public void Find_AList_KeepsItsDeclaredType()
        {
            PropertyInfo info = PropertyReflection.Find(new Root(), "parts");

            Assert.AreEqual(typeof(List<Part>), info.type);
            Assert.AreEqual(typeof(Part), PropertyReflection.GetElementType(info.type));
        }

        [Test]
        public void Find_APrivateSerializedField()
        {
            Assert.AreEqual(1.5f, PropertyReflection.Find(new Root(), "_hidden").value);
        }

        [Test]
        public void Find_NoSuchField_Nothing()
        {
            Assert.IsNull(PropertyReflection.Find(new Root(), "part.missing").field);
        }

        [Test]
        public void GetElementType_OfArraysAndLists()
        {
            Assert.AreEqual(typeof(int), PropertyReflection.GetElementType(typeof(int[])));
            Assert.AreEqual(typeof(string), PropertyReflection.GetElementType(typeof(List<string>)));
            Assert.IsNull(PropertyReflection.GetElementType(typeof(string)));
        }

        [Test]
        public void GetMemberValue_FieldsPropertiesAndMethods_EvenPrivate()
        {
            Root root = new Root();

            Assert.AreEqual(true, PropertyReflection.GetMemberValue(root, "isOn", out bool foundField));
            Assert.AreEqual(3, PropertyReflection.GetMemberValue(root, "count", out bool foundProperty));
            Assert.AreEqual("described", PropertyReflection.GetMemberValue(root, "Describe", out bool foundMethod));
            Assert.IsTrue(foundField && foundProperty && foundMethod);

            PropertyReflection.GetMemberValue(root, "nothing", out bool found);
            Assert.IsFalse(found);
        }

        enum Mode
        {
            None,
            Some,
        }

        [Test]
        public void IsTruthy_TrueBoolsObjectsAndNonZeroNumbers()
        {
            Assert.IsTrue(PropertyReflection.IsTruthy(true));
            Assert.IsFalse(PropertyReflection.IsTruthy(false));
            Assert.IsFalse(PropertyReflection.IsTruthy(null));
            Assert.IsTrue(PropertyReflection.IsTruthy(2));
            Assert.IsFalse(PropertyReflection.IsTruthy(0f));
            Assert.IsTrue(PropertyReflection.IsTruthy(Mode.Some));
            Assert.IsFalse(PropertyReflection.IsTruthy(Mode.None));
            Assert.IsTrue(PropertyReflection.IsTruthy("text"));
        }
    }
}

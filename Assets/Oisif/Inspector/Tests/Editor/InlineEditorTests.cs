using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Oisif.Inspector;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    // An object is drawn inline (with a foldout) when its field or its class asks for it, never under HideInlineEditor
    public class InlineEditorTests
    {
        [InlineEditor]
        class InlineData : ScriptableObject {}

        class PlainData : ScriptableObject {}

        class Owner
        {
            public PlainData plain = null;
            [InlineEditor] public PlainData inlineField = null;
            public InlineData inlineClass = null;
            [HideInlineEditor] public InlineData hidden = null;
            [HideInlineEditor] public List<InlineData> hiddenList = new List<InlineData>();
        }

        InlineData _inline;
        PlainData _plain;

        [SetUp]
        public void SetUp()
        {
            _inline = ScriptableObject.CreateInstance<InlineData>();
            _plain = ScriptableObject.CreateInstance<PlainData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_inline);
            Object.DestroyImmediate(_plain);
        }

        static FieldInfo Field(string name)
        {
            return typeof(Owner).GetField(name);
        }

        [Test]
        public void CanDrawInline_InlineEditorOnTheFieldOrTheClass_True()
        {
            Assert.IsTrue(AttributeEditor.CanDrawInline(_plain, Field("inlineField"), false));
            Assert.IsTrue(AttributeEditor.CanDrawInline(_inline, Field("inlineClass"), false));
            // An element of a list has no field of its own
            Assert.IsTrue(AttributeEditor.CanDrawInline(_inline, null, false));
        }

        [Test]
        public void CanDrawInline_NoInlineEditorOrNoObject_False()
        {
            Assert.IsFalse(AttributeEditor.CanDrawInline(_plain, Field("plain"), false));
            Assert.IsFalse(AttributeEditor.CanDrawInline(null, Field("inlineField"), false));
        }

        [Test]
        public void CanDrawInline_HideInlineEditorOnTheField_False()
        {
            Assert.IsFalse(AttributeEditor.CanDrawInline(_inline, Field("hidden"), false));
        }

        [Test]
        public void CanDrawInline_ElementOfAHideInlineEditorList_False()
        {
            Assert.IsFalse(AttributeEditor.CanDrawInline(_inline, null, true));
        }
    }
}

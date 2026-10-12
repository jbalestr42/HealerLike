using System.Reflection;
using NUnit.Framework;
using Oisif.Inspector;
using UnityEngine;

namespace Oisif.Editor.Tests
{
    // The attributes read once are the ones reflection gives, inherited ones included
    public class AttributeCacheTests
    {
        [InlineEditor]
        class BaseData {}

        class DerivedData : BaseData {}

        class Owner
        {
            [ReadOnly] public int readOnly = 0;
            [ShowIf("a")] [HideIf("b")] public int conditions = 0;
            [Header("Title")] public int plain = 0;
        }

        static FieldInfo Field(string name)
        {
            return typeof(Owner).GetField(name);
        }

        [Test]
        public void Has_TheAttributeOfTheField()
        {
            Assert.IsTrue(AttributeCache.Has<ReadOnlyAttribute>(Field("readOnly")));
            Assert.IsFalse(AttributeCache.Has<ReadOnlyAttribute>(Field("plain")));
        }

        [Test]
        public void Has_TheAttributeOfABaseClass()
        {
            Assert.IsTrue(AttributeCache.Has<InlineEditorAttribute>(typeof(DerivedData)));
        }

        [Test]
        public void Get_TheAttributeOrNull()
        {
            Assert.AreEqual("Title", AttributeCache.Get<HeaderAttribute>(Field("plain")).header);
            Assert.IsNull(AttributeCache.Get<HeaderAttribute>(Field("readOnly")));
        }

        // A HideIf is a ShowIf: both found as ShowIf, like with reflection
        [Test]
        public void GetAll_TheAttributesOfTheTypeAndOfItsDerivedTypes()
        {
            ShowIfAttribute[] conditions = AttributeCache.GetAll<ShowIfAttribute>(Field("conditions"));

            Assert.AreEqual(2, conditions.Length);
            Assert.AreEqual(1, AttributeCache.GetAll<HideIfAttribute>(Field("conditions")).Length);
        }

        [Test]
        public void GetAll_NullMember_None()
        {
            Assert.IsEmpty(AttributeCache.GetAll<ReadOnlyAttribute>(null));
            Assert.IsFalse(AttributeCache.Has<ReadOnlyAttribute>(null));
        }

        // Asked twice, the same attributes (read once)
        [Test]
        public void GetAll_Twice_TheSameArray()
        {
            Assert.AreSame(AttributeCache.GetAll<ShowIfAttribute>(Field("conditions")), AttributeCache.GetAll<ShowIfAttribute>(Field("conditions")));
        }
    }
}

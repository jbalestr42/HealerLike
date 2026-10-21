using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio.Editor
{
    public class StudioOwnershipTests
    {
        EntityData _first;
        EntityData _second;
        StudioSelection<EntityData> _selection;
        float _labelWidth;

        [SetUp]
        public void SetUp()
        {
            _first = ScriptableObject.CreateInstance<EntityData>();
            _second = ScriptableObject.CreateInstance<EntityData>();
            _selection = new StudioSelection<EntityData>();
            _labelWidth = EditorGUIUtility.labelWidth;
        }

        [TearDown]
        public void TearDown()
        {
            _selection.Dispose();
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
            EditorGUIUtility.labelWidth = _labelWidth;
        }

        [Test]
        public void Selection_ReplacementAppliesPendingEditsAndKeepsBorrowedAssetsAlive()
        {
            _selection.Select(_first);
            _selection.serialized.FindProperty("m_Name").stringValue = "Pending creature";
            _selection.Select(_second);
            Assert.AreEqual("Pending creature", _first.name);
            Assert.AreSame(_second, _selection.asset);
            Assert.AreSame(_second, _selection.serialized.targetObject);
            _selection.Dispose();
            _selection.Dispose();
            Assert.IsTrue(_first);
            Assert.IsTrue(_second);
            Assert.IsNull(_selection.asset);
            Assert.IsNull(_selection.serialized);
        }

        [Test]
        public void LabelWidth_NestedDrawFailure_RestoresEachBorrowedValue()
        {
            EditorGUIUtility.labelWidth = 73f;
            using (new StudioLabelWidthScope(112f))
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (new StudioLabelWidthScope(65f))
                    {
                        throw new InvalidOperationException("draw ended early");
                    }
                });
                Assert.AreEqual(112f, EditorGUIUtility.labelWidth);
            }

            Assert.AreEqual(73f, EditorGUIUtility.labelWidth);
        }
    }
}

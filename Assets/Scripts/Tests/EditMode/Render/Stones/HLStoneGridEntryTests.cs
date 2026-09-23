using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneGridEntryTests
    {
        [Test]
        public void DefaultRefusesBeforeInspectingGameplayInputs()
        {
            GameObject go = new GameObject("HLDefaultEntry");
            try
            {
                HLStoneGridEntry entry = go.AddComponent<HLStoneGridEntry>();
                System.InvalidOperationException error =
                    Assert.Throws<System.InvalidOperationException>(() => entry.Generate(null, null, 1));
                StringAssert.Contains("demo-scene-only", error.Message);
                Assert.IsFalse(entry.isGenerating);
                Assert.AreEqual(0, go.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RefusesUnwiredGenerationBeforeGameplay()
        {
            GameObject go = new GameObject("HLEntry");
            try
            {
                HLStoneGridEntry entry = go.AddComponent<HLStoneGridEntry>();
                TestHelpers.SetPrivateField(entry, "_demoSceneOnly", true);
                Assert.Throws<System.ArgumentNullException>(() => entry.Generate(null, null, 1));
                Assert.IsFalse(entry.isGenerating);

                entry.CompleteGeneration();
                Assert.IsFalse(entry.isGenerating);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

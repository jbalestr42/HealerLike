using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneGenerationFenceTests
    {
        [Test]
        public void CompletionDoesNotConsumeRandomOrSpawnObjects()
        {
            GameObject go = new GameObject("HLFence");
            try
            {
                HLStoneGridEntry entry = go.AddComponent<HLStoneGridEntry>();
                TestHelpers.SetPrivateField(entry, "_demoSceneOnly", true);
                HLStoneGenerationFence fence = go.AddComponent<HLStoneGenerationFence>();
                fence.Bind(entry);
                System.Reflection.PropertyInfo property = typeof(HLStoneGridEntry).GetProperty("isGenerating");
                System.Reflection.MethodInfo setter = property.GetSetMethod(true);
                setter.Invoke(entry, new object[] { true });
                Assert.Throws<System.InvalidOperationException>(() => entry.Generate(null, null, 2));

                Assert.IsFalse(fence.Spawn(null).MoveNext());
                Assert.IsFalse(entry.isGenerating);
                Assert.AreEqual(0, fence.count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

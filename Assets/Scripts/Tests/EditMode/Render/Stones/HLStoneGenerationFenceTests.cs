using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneGenerationFenceTests
    {
        [Test] public void CompletionDoesNotConsumeRandomOrSpawnObjects()
        {
            var go=new GameObject("HLFence");
            try {
                var entry=go.AddComponent<HLStoneGridEntry>();TestHelpers.SetPrivateField(entry,"demoSceneOnly",true);var fence=go.AddComponent<HLStoneGenerationFence>();fence.Bind(entry);
                typeof(HLStoneGridEntry).GetProperty("IsGenerating").GetSetMethod(true).Invoke(entry,new object[]{true});
                Assert.Throws<System.InvalidOperationException>(()=>entry.Generate(null,null,2));
                Assert.IsFalse(fence.Spawn(null).MoveNext());Assert.IsFalse(entry.IsGenerating);Assert.AreEqual(0,fence.count);
            }finally{Object.DestroyImmediate(go);}
        }
    }
}

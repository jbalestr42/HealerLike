using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneGridEntryTests
    {
        [Test] public void DefaultRefusesBeforeInspectingGameplayInputs()
        {
            var go=new GameObject("HLDefaultEntry");
            try
            {
                var entry=go.AddComponent<HLStoneGridEntry>();
                var error=Assert.Throws<System.InvalidOperationException>(()=>entry.Generate(null,null,1));
                StringAssert.Contains("demo-scene-only",error.Message);
                Assert.IsFalse(entry.IsGenerating);
                Assert.AreEqual(0,go.transform.childCount);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void RefusesUnwiredGenerationBeforeGameplay()
        {
            var go=new GameObject("HLEntry");
            try {var entry=go.AddComponent<HLStoneGridEntry>();TestHelpers.SetPrivateField(entry,"demoSceneOnly",true);Assert.Throws<System.ArgumentNullException>(()=>entry.Generate(null,null,1));Assert.IsFalse(entry.IsGenerating);entry.CompleteGeneration();Assert.IsFalse(entry.IsGenerating);}
            finally{Object.DestroyImmediate(go);}
        }
    }
}

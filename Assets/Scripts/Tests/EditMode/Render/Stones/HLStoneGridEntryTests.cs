using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneGridEntryTests
    {
        [Test] public void RefusesUnwiredGenerationBeforeGameplay()
        {
            var go=new GameObject("HLEntry");
            try {var entry=go.AddComponent<HLStoneGridEntry>();Assert.Throws<System.ArgumentNullException>(()=>entry.Generate(null,null,1));Assert.IsFalse(entry.IsGenerating);entry.CompleteGeneration();Assert.IsFalse(entry.IsGenerating);}
            finally{Object.DestroyImmediate(go);}
        }
    }
}

using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneFractureTests
    {
        [Test] public void SeededSubsetDarkensProgressivelyAndRestoresWithoutMaterialMutation()
        {
            var root=new GameObject("HLAssembly"); var assembly=new HLStoneAssembly();
            try
            {
                assembly.BuildEnemy(root.transform,17,HLStonePreset.Boulder,null);
                var block=new MaterialPropertyBlock(); int changed=0;
                assembly.ApplyFracture(.7f,17);
                var colors=new Color[assembly.Parts.Count];
                for(int i=0;i<colors.Length;i++) { assembly.Parts[i].Renderer.GetPropertyBlock(block); colors[i]=block.GetColor("_BaseColor"); }
                assembly.ApplyFracture(.2f,17);
                for(int i=0;i<colors.Length;i++)
                {
                    assembly.Parts[i].Renderer.GetPropertyBlock(block); var color=block.GetColor("_BaseColor");
                    if(color!=colors[i]) { changed++; Assert.Less(color.grayscale,colors[i].grayscale); }
                }
                Assert.AreEqual(2,changed);
                assembly.ApplyFracture(1,17);
                foreach(var part in assembly.Parts) { part.Renderer.GetPropertyBlock(block); Assert.Less(((Vector4)part.BaseColor-(Vector4)block.GetColor("_BaseColor")).magnitude,1e-6f); }
            }
            finally { assembly.Dispose(); Object.DestroyImmediate(root); }
        }
    }
}

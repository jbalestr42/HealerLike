using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneFractureTests
    {
        [Test] public void StatusTintComposesWithCurrentFractureAndRemovalRestoresHealthColor()
        {
            var root=new GameObject("HLTintAssembly");var assembly=new HLStoneAssembly();
            try
            {
                assembly.BuildEnemy(root.transform,17,HLStonePreset.Boulder,null);
                var block=new MaterialPropertyBlock();assembly.ApplyFracture(.3f,17);
                assembly.Parts[0].Renderer.GetPropertyBlock(block);var healthColor=(Color)block.GetVector("_BaseColor");
                var tint=new Color(.5f,.12f,.55f,1);assembly.ApplyFracture(.3f,17,tint);
                assembly.Parts[0].Renderer.GetPropertyBlock(block);
                Assert.Less(((Vector4)Color.Lerp(healthColor,tint,.42f)-(Vector4)(Color)block.GetVector("_BaseColor")).magnitude,1e-6f);
                assembly.ApplyFracture(.3f,17,Color.white);assembly.Parts[0].Renderer.GetPropertyBlock(block);
                Assert.Less(((Vector4)healthColor-(Vector4)(Color)block.GetVector("_BaseColor")).magnitude,1e-6f);
            }
            finally { assembly.Dispose();Object.DestroyImmediate(root); }
        }
        [Test] public void SeededSubsetDarkensProgressivelyAndRestoresWithoutMaterialMutation()
        {
            var root=new GameObject("HLAssembly"); var assembly=new HLStoneAssembly();
            try
            {
                assembly.BuildEnemy(root.transform,17,HLStonePreset.Boulder,null);
                var block=new MaterialPropertyBlock(); int changed=0;
                assembly.ApplyFracture(.7f,17);
                var colors=new Color[assembly.Parts.Count];
                for(int i=0;i<colors.Length;i++) { assembly.Parts[i].Renderer.GetPropertyBlock(block); colors[i]=(Color)block.GetVector("_BaseColor"); }
                assembly.ApplyFracture(.2f,17);
                for(int i=0;i<colors.Length;i++)
                {
                    assembly.Parts[i].Renderer.GetPropertyBlock(block); var color=(Color)block.GetVector("_BaseColor");
                    if(color!=colors[i]) { changed++; Assert.Less(color.grayscale,colors[i].grayscale); }
                }
                Assert.AreEqual(2,changed);
                assembly.ApplyFracture(1,17);
                foreach(var part in assembly.Parts) { part.Renderer.GetPropertyBlock(block); Assert.Less(((Vector4)part.BaseColor-(Vector4)(Color)block.GetVector("_BaseColor")).magnitude,1e-6f); }
            }
            finally { assembly.Dispose(); Object.DestroyImmediate(root); }
        }
    }
}

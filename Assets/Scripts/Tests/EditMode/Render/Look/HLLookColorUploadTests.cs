using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
namespace HealerLike.Render.Look
{
    public class HLLookColorUploadTests
    {
        [Test] public void WorkingSpacePropertyVectorsReachPrimitiveShaderUnchanged()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) Assert.Ignore("Requires graphics readback");
            var material=new Material(Shader.Find("HL/Look/Primitive"));
            var mesh=new Mesh { vertices=new[]{new Vector3(-.8f,-.8f,0),new Vector3(-.8f,.8f,0),new Vector3(.8f,.8f,0),new Vector3(.8f,-.8f,0)}, triangles=new[]{0,1,2,0,2,3}, normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back} };
            var target=new RenderTexture(16,16,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear); target.Create();
            var texture=new Texture2D(16,16,TextureFormat.RGBAFloat,false,true);
            var names=new[]{"_HLLookApplied","_HLToonThreshold","_HLFogStart","_HLFogEnd","_HLFogBands","_HLInkStrength"};
            var saved=new float[names.Length];for(int i=0;i<names.Length;i++)saved[i]=Shader.GetGlobalFloat(names[i]);
            var previous=RenderTexture.active;
            try {
                Shader.SetGlobalFloat(names[0],1);Shader.SetGlobalFloat(names[1],0);Shader.SetGlobalFloat(names[2],10000);Shader.SetGlobalFloat(names[3],20000);Shader.SetGlobalFloat(names[4],6);Shader.SetGlobalFloat(names[5],0);
                var artist=new Color(.4f,.6f,.8f,1);var linear=artist.linear;
                Color Read(int mode) {
                    var block=new MaterialPropertyBlock();
                    if(mode==0)block.SetColor("_BaseColor",artist);else if(mode==1)block.SetColor("_BaseColor",linear);else block.SetVector("_BaseColor",linear);
                    Debug.Log("HL COLOR mode="+mode+" GetColor="+block.GetColor("_BaseColor").ToString("F5")+" GetVector="+block.GetVector("_BaseColor").ToString("F5"));
                    using(var command=new CommandBuffer()) {command.SetRenderTarget(target);command.ClearRenderTarget(false,true,Color.magenta);command.SetViewProjectionMatrices(Matrix4x4.identity,Matrix4x4.identity);command.DrawMesh(mesh,Matrix4x4.identity,material,0,material.FindPass("HLForward"),block);Graphics.ExecuteCommandBuffer(command);}
                    RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,16,16),0,0);texture.Apply();var color=texture.GetPixel(8,8);Debug.Log("HL COLOR pixel="+color.ToString("F5"));return color;
                }
                var a=Read(0);var b=Read(1);var c=Read(2);
                Assert.That(((Vector4)a-(Vector4)c).magnitude,Is.LessThan(.004f),"Artist SetColor and working SetVector must agree");
                Assert.That(((Vector4)b-(Vector4)c).magnitude,Is.GreaterThan(.2f),"Double conversion must be detected by GPU readback");
                Assert.That(((Vector4)c-(Vector4)linear).magnitude,Is.LessThan(.004f),"Explicit working vector should reach GPU unchanged");
                Debug.Log("HL COLOR active="+QualitySettings.activeColorSpace+" artist="+artist.ToString("F5")+" linear="+linear.ToString("F5")+" A="+a.ToString("F5")+" B="+b.ToString("F5")+" C="+c.ToString("F5"));
            } finally {for(int i=0;i<names.Length;i++)Shader.SetGlobalFloat(names[i],saved[i]);RenderTexture.active=previous;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);Object.DestroyImmediate(mesh);Object.DestroyImmediate(material);}
        }
    }
}

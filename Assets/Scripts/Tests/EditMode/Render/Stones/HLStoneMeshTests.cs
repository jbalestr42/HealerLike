using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneMeshTests
    {
        [TestCase(0,60)] [TestCase(1,240)] [TestCase(2,960)]
        public void CountsAndFlatNormals(int n,int count)
        {
            var s=HLStonePresets.Boulder; s.Subdivisions=n;
            var d=HLStoneMesh.Generate(0,s); Assert.AreEqual(count,d.Vertices.Length); Assert.AreEqual(count,d.Indices.Length);
            for(int i=0;i<count;i+=3)
            {
                var a=d.Vertices[i]; var b=d.Vertices[i+1]; var c=d.Vertices[i+2];
                Assert.That(d.Normals[i].magnitude,Is.EqualTo(1).Within(1e-5));
                Assert.AreEqual(d.Normals[i],d.Normals[i+1]); Assert.AreEqual(d.Normals[i],d.Normals[i+2]);
                Assert.That(Vector3.Dot(d.Normals[i],(a+b+c)),Is.GreaterThan(0));
                Assert.That(Vector3.Distance(d.Normals[i],Vector3.Cross(b-a,c-a).normalized),Is.LessThan(1e-5));
                for(int j=0;j<3;j++) Assert.That(Vector3.Distance(d.Bounds.ClosestPoint(d.Vertices[i+j]),d.Vertices[i+j]),Is.LessThan(1e-6));
            }
        }
        [TestCase(0u)] [TestCase(1u)] [TestCase(uint.MaxValue)] [TestCase(827361u)]
        public void DeterministicAndIndependentOfGlobalRandom(uint seed)
        {
            var state=Random.state;
            try {
                var a=HLStoneMesh.Generate(seed,HLStonePresets.Boulder);
                Assert.AreEqual(state,Random.state);
                Random.InitState(128); HLStoneMesh.Generate(seed+1,HLStonePresets.Monolith);
                var b=HLStoneMesh.Generate(seed,HLStonePresets.Boulder);
                CollectionAssert.AreEqual(a.Vertices,b.Vertices); CollectionAssert.AreEqual(a.Normals,b.Normals); CollectionAssert.AreEqual(a.Indices,b.Indices);
                CollectionAssert.AreNotEqual(a.Vertices,HLStoneMesh.Generate(seed+1,HLStonePresets.Boulder).Vertices);
            } finally { Random.state=state; }
        }
        [Test]
        public void SweepAllSeedsPresetsAndExtremeScalesWithoutDegeneracy()
        {
            var shapes=new[]{HLStonePresets.Boulder,HLStonePresets.Cairn,HLStonePresets.Monolith};
            for(int n=0;n<=2;n++) foreach(var shape in shapes) foreach(float rough in new[]{0f,.18f})
            for(int axes=0;axes<8;axes++) for(uint seed=0;seed<1024;seed++)
            {
                var s=shape; s.Subdivisions=n; s.Roughness=rough;
                // Exercise every seed with all eight independent min/max scale combinations.
                s.Size=(axes&1)==0?.02f:8f; s.Elongation=(axes&2)==0?.25f:5f; s.DepthRatio=(axes&4)==0?.25f:2f;
                var d=HLStoneMesh.Generate(seed,s);
                var inv=new Vector3(2/s.Size,2/(s.Size*s.Elongation),2/(s.Size*s.DepthRatio));
                for(int i=0;i<d.Vertices.Length;i+=3)
                {
                    var a=d.Vertices[i]; var b=d.Vertices[i+1]; var c=d.Vertices[i+2];
                    var cross=Vector3.Cross(b-a,c-a);
                    if (!float.IsFinite(cross.sqrMagnitude) || cross.sqrMagnitude<=0 || Vector3.Dot(cross,a+b+c)<=0)
                        Assert.Fail($"Bad scaled face seed {seed}, n {n}");
                    if(Vector3.Cross(Vector3.Scale(b-a,inv),Vector3.Scale(c-a,inv)).magnitude<=1e-10f)
                        Assert.Fail("Degenerate normalized face");
                    if(Mathf.Abs(d.Normals[i].magnitude-1)>1e-5f) Assert.Fail("Non-unit normal");
                }
            }
        }
        [Test] public void CanonicalGeometryHashIsPinned()
        {
            uint h=2166136261;
            var d=HLStoneMesh.Generate(827361,HLStonePresets.Boulder);
            foreach(var v in d.Vertices) foreach(float f in new[]{v.x,v.y,v.z}) h=HLStoneSeed.ForPart(h,unchecked((uint)System.BitConverter.SingleToInt32Bits(f)));
            foreach(var v in d.Normals) foreach(float f in new[]{v.x,v.y,v.z}) h=HLStoneSeed.ForPart(h,unchecked((uint)System.BitConverter.SingleToInt32Bits(f)));
            foreach(int i in d.Indices) h=HLStoneSeed.ForPart(h,(uint)i);
            Assert.AreEqual(3051263645u,h,"Version-1 geometry golden on Unity 6000.6");
        }
        [Test]
        public void InvalidSettingsRejectedAndMeshCopiesData()
        {
            var s=HLStonePresets.Boulder; s.Size=float.NaN; Assert.Throws<System.ArgumentOutOfRangeException>(()=>HLStoneMesh.Generate(0,s));
            s=HLStonePresets.Boulder; s.Elongation=0; Assert.Throws<System.ArgumentOutOfRangeException>(()=>HLStoneMesh.Generate(0,s));
            s=HLStonePresets.Boulder; s.DepthRatio=3; Assert.Throws<System.ArgumentOutOfRangeException>(()=>HLStoneMesh.Generate(0,s));
            s=HLStonePresets.Boulder; s.Roughness=.19f; Assert.Throws<System.ArgumentOutOfRangeException>(()=>HLStoneMesh.Generate(0,s));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>HLStoneMesh.VertexCount(3));
            var data=HLStoneMesh.Generate(8,HLStonePresets.Boulder); var mesh=HLStoneMesh.CreateMesh(data);
            try { CollectionAssert.AreEqual(data.Vertices,mesh.vertices); CollectionAssert.AreEqual(data.Normals,mesh.normals); Assert.AreEqual(240,mesh.vertexCount); }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}

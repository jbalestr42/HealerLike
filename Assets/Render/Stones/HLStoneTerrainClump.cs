using System;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneTerrainClump : MonoBehaviour
    {
        [SerializeField] Material stoneMaterial;
        [SerializeField] bool groundShadowEnabled=true;
        [SerializeField] Vector3 directionToKeyLight=new Vector3(-1,2,-1);
        HLStoneGroundShadow groundShadow;
        HLStoneGroundRing groundRing;
        HLStoneLife life;
        Mesh ochreMesh;
        GameObject ochreFace;
        public float BareGroundRadius=>groundRing!=null?groundRing.Radius:0;
        public Vector3 BareGroundCenter=>groundRing!=null?groundRing.Center:transform.position;
        public bool GroundShadowEnabled { get=>groundShadowEnabled; set { groundShadowEnabled=value; if(groundShadow!=null) groundShadow.Visible=value; } }
        readonly HLStoneAssembly assembly=new HLStoneAssembly();
        public HLStoneAssembly Assembly=>assembly;
        public void Initialize(uint seed,float cellSize)
        {
            if(!float.IsFinite(cellSize) || cellSize<=0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            ClearFace(); assembly.Dispose(); var r=new HLStoneRandom(HLStoneSeed.ForPart(seed,401));
            int count=3+(int)(r.Next()%3);
            for(int i=0;i<count;i++)
            {
                float size=i==0?.65f:r.Range(.27f,.42f);
                var part=new HLStonePart {
                    Shape=HLStonePresets.Shape(size,i==0?1.35f:r.Range(.7f,1.15f),.95f,.14f,(int)(r.Next()%2)),
                    SeedSalt=501u+(uint)i,PaletteIndex=(int)(r.Next()%3),
                    LocalEulerAngles=new Vector3(0,r.Range(0,360),0),
                    LocalPosition=i==0?Vector3.zero:new Vector3(Mathf.Cos(i*2.4f)*.28f,0,Mathf.Sin(i*2.4f)*.28f)
                };
                assembly.Add(transform,seed,part,stoneMaterial);
                var p=assembly.Parts[i]; var pos=p.Transform.localPosition; pos.y=-p.Lease.Data.Bounds.min.y; p.Transform.localPosition=pos;
            }
            assembly.Fit(.96f,r.Range(.7f,1.2f));
            foreach(var p in assembly.Parts) { p.Transform.localPosition*=cellSize; p.Transform.localScale*=cellSize; }
            assembly.RecalculateBounds();
            if(groundShadow==null) groundShadow=gameObject.AddComponent<HLStoneGroundShadow>();
            groundShadow.Configure(assembly.LocalBounds,directionToKeyLight,groundShadowEnabled);
            if(groundRing==null) groundRing=gameObject.AddComponent<HLStoneGroundRing>();
            groundRing.Configure(assembly.LocalBounds);
            if(life==null) life=gameObject.AddComponent<HLStoneLife>();
            life.Configure(null,seed,BareGroundRadius,true);
            if(HLStoneLifeState.Ochre(seed)) CreateFace();
        }
        void CreateFace()
        {
            var part=assembly.Parts[0]; var mesh=part.Lease.Mesh;
            var vertices=mesh.vertices; var normals=mesh.normals; int face=0;
            for(int i=3;i<vertices.Length;i+=3) if(normals[i].x+normals[i].y*.5f>normals[face].x+normals[face].y*.5f) face=i;
            ochreMesh=new Mesh{name="HLOchreFacet"};
            ochreMesh.vertices=new[]{vertices[face]+normals[face]*.002f,vertices[face+1]+normals[face]*.002f,vertices[face+2]+normals[face]*.002f};
            ochreMesh.triangles=new[]{0,1,2}; ochreMesh.RecalculateNormals(); ochreMesh.RecalculateBounds();
            ochreFace=new GameObject("HLOchreFace"); ochreFace.transform.SetParent(part.Transform,false);
            ochreFace.AddComponent<MeshFilter>().sharedMesh=ochreMesh;
            var renderer=ochreFace.AddComponent<MeshRenderer>(); renderer.sharedMaterial=stoneMaterial;
            var block=new MaterialPropertyBlock(); block.SetColor("_BaseColor",HLStoneAssembly.Palette[3].linear); renderer.SetPropertyBlock(block);
        }
        void ClearFace() { HLStoneMeshCache.DestroyOwned(ochreFace); HLStoneMeshCache.DestroyOwned(ochreMesh); }
        void OnEnable() { if(life!=null) life.enabled=true; }
        void OnDisable() { if(life!=null) life.enabled=false; }
        void OnDestroy() { ClearFace(); assembly.Dispose(); }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    // Owns only its generated part objects and mesh leases, never gameplay anchors.
    public sealed class HLStoneAssembly : IDisposable
    {
        public sealed class Part
        {
            public Transform Transform; public MeshRenderer Renderer; public HLStoneMeshCache.Lease Lease;
        }
        public readonly List<Part> Parts=new List<Part>();
        public Bounds LocalBounds { get; private set; }
        public static readonly Color[] Palette={ new Color32(201,196,180,255),new Color32(142,147,161,255),new Color32(74,84,104,255),new Color32(199,154,75,255) };
        public void Add(Transform parent,uint seed,HLStonePart recipe,Material material)
        {
            var lease=HLStoneMeshCache.Acquire(HLStoneSeed.ForPart(seed,recipe.SeedSalt),recipe.Shape);
            var go=new GameObject("HLStonePart"); go.layer=parent.gameObject.layer; go.transform.SetParent(parent,false);
            go.transform.localPosition=recipe.LocalPosition; go.transform.localRotation=Quaternion.Euler(recipe.LocalEulerAngles);
            go.AddComponent<MeshFilter>().sharedMesh=lease.Mesh;
            var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            var properties=new MaterialPropertyBlock(); properties.SetColor("_BaseColor",Palette[Mathf.Clamp(recipe.PaletteIndex,0,3)].linear); renderer.SetPropertyBlock(properties);
            Parts.Add(new Part { Transform=go.transform,Renderer=renderer,Lease=lease });
        }
        public void BuildEnemy(Transform parent,uint seed,HLStonePreset preset,Material material,HLStoneAssemblyProfile profile=null)
        {
            Dispose();
            if(profile!=null)
            {
                foreach(var recipe in profile.Parts) Add(parent,seed,recipe,material);
            }
            else if(preset==HLStonePreset.Monolith)
                Add(parent,seed,new HLStonePart{Shape=HLStonePresets.Monolith,SeedSalt=1,PaletteIndex=2},material);
            else
            {
                float top=0;
                for(int i=0;i<3;i++)
                {
                    HLStoneSettings shape;
                    if(preset==HLStonePreset.Cairn) shape=HLStonePresets.Shape(new[]{.55f,.42f,.29f}[i],new[]{.65f,.75f,.95f}[i]);
                    else shape=HLStonePresets.Shape(new[]{.58f,.33f,.23f}[i],i==0?.85f:1.15f,.9f,.14f);
                    Add(parent,seed,new HLStonePart{Shape=shape,SeedSalt=(uint)i+1,PaletteIndex=preset==HLStonePreset.Cairn?(i==2?3:2-i):i,
                        LocalEulerAngles=new Vector3(0,(HLStoneSeed.ForPart(seed,(uint)i+31)%360),0)},material);
                    var part=Parts[i]; Bounds b=PartBounds(part);
                    if(preset==HLStonePreset.Cairn) { part.Transform.localPosition=new Vector3(0,top-b.min.y,0); top+=b.size.y*.92f; }
                    else part.Transform.localPosition=new Vector3(new[]{-.08f,.23f,-.22f}[i],-b.min.y+(i==0?0:.22f),new[]{0f,.06f,-.16f}[i]);
                }
            }
            Fit(.9f,preset==HLStonePreset.Cairn?1.05f:preset==HLStonePreset.Monolith?1.45f:.8f);
        }
        public void Fit(float width,float height)
        {
            RecalculateBounds();
            float xz=Mathf.Min(1,width/Mathf.Max(LocalBounds.size.x,LocalBounds.size.z));
            float y=height/LocalBounds.size.y;
            Vector3 scale=new Vector3(xz,y,xz),offset=new Vector3(LocalBounds.center.x,LocalBounds.min.y,LocalBounds.center.z);
            foreach(var p in Parts) { p.Transform.localPosition=Vector3.Scale(p.Transform.localPosition-offset,scale); p.Transform.localScale=Vector3.Scale(p.Transform.localScale,scale); }
            RecalculateBounds();
        }
        public void RecalculateBounds()
        {
            if(Parts.Count==0) { LocalBounds=default; return; }
            Bounds b=PartBounds(Parts[0]); for(int i=1;i<Parts.Count;i++) b.Encapsulate(PartBounds(Parts[i])); LocalBounds=b;
        }
        static Bounds PartBounds(Part part)
        {
            Matrix4x4 m=Matrix4x4.TRS(part.Transform.localPosition,part.Transform.localRotation,part.Transform.localScale);
            var vertices=part.Lease.Data.Vertices; var b=new Bounds(m.MultiplyPoint3x4(vertices[0]),Vector3.zero);
            foreach(var v in vertices) b.Encapsulate(m.MultiplyPoint3x4(v)); return b;
        }
        public void Dispose()
        {
            foreach(var part in Parts) { if(part.Transform!=null) { part.Transform.gameObject.SetActive(false); HLStoneMeshCache.DestroyOwned(part.Transform.gameObject); } part.Lease.Dispose(); }
            Parts.Clear();
        }
    }
}

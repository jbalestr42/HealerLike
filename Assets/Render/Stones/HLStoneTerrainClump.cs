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
        public bool GroundShadowEnabled { get=>groundShadowEnabled; set { groundShadowEnabled=value; if(groundShadow!=null) groundShadow.Visible=value; } }
        readonly HLStoneAssembly assembly=new HLStoneAssembly();
        public HLStoneAssembly Assembly=>assembly;
        public void Initialize(uint seed,float cellSize)
        {
            if(!float.IsFinite(cellSize) || cellSize<=0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            assembly.Dispose(); var r=new HLStoneRandom(HLStoneSeed.ForPart(seed,401));
            int count=3+(int)(r.Next()%3);
            for(int i=0;i<count;i++)
            {
                float size=i==0?.65f:r.Range(.27f,.42f);
                var part=new HLStonePart {
                    Shape=HLStonePresets.Shape(size,i==0?1.35f:r.Range(.7f,1.15f),.95f,.14f,(int)(r.Next()%2)),
                    SeedSalt=501u+(uint)i,PaletteIndex=i==count-1 && r.Next01()<.2f?3:(int)(r.Next()%3),
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
        }
        void OnDestroy() => assembly.Dispose();
    }
}

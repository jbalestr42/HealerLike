using UnityEngine;
using UnityEngine.Rendering;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneGroundRing : MonoBehaviour
    {
        [SerializeField] Color groundColour=new Color32(70,111,87,255);
        public Color GroundColour
        {
            get=>groundColour;
            set { groundColour=value; if(material!=null) ApplyColour(); }
        }
        void ApplyColour() { Color c=groundColour; c.a=1; material.SetColor("_BaseColor",c.linear); }
        Transform disc;
        Mesh mesh;
        Material material;
        float radius;
        public float Radius=>radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
        public Vector3 Center=>disc!=null?disc.position:transform.position;
        public void Configure(Bounds bounds)
        {
            radius=Mathf.Max(bounds.extents.x,bounds.extents.z)*1.18f;
            if(disc==null)
            {
                disc=new GameObject("HLBareGround").transform; disc.SetParent(transform,false); disc.gameObject.layer=gameObject.layer;
                const int n=32; var v=new Vector3[n+1]; var t=new int[n*3];
                for(int i=0;i<n;i++)
                {
                    float angle=i*Mathf.PI*2/n; float edge=.87f+.08f*Mathf.Sin(angle*5)+.05f*Mathf.Cos(angle*9); v[i+1]=new Vector3(Mathf.Cos(angle)*edge,0,Mathf.Sin(angle)*edge);
                    t[i*3]=0; t[i*3+1]=(i+1)%n+1; t[i*3+2]=i+1;
                }
                mesh=new Mesh{name="HLBareGroundDisc"}; mesh.vertices=v; mesh.triangles=t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                disc.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="HLBareGroundMaterial"};
                var renderer=disc.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            }
            ApplyColour();
            disc.localPosition=new Vector3(bounds.center.x,bounds.min.y+.006f,bounds.center.z);
            disc.localScale=new Vector3(radius,1,radius);
        }
        void OnEnable() { if(disc!=null) disc.gameObject.SetActive(true); }
        void OnDisable() { if(disc!=null) disc.gameObject.SetActive(false); }
        void OnDestroy()
        {
            if(disc!=null) HLStoneMeshCache.DestroyOwned(disc.gameObject);
            HLStoneMeshCache.DestroyOwned(mesh); HLStoneMeshCache.DestroyOwned(material);
        }
    }
}

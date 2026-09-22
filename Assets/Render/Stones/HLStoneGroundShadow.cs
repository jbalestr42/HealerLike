using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stones
{
    // Cosmetic ground projection. Direction points toward the key light, in world space.
    public sealed class HLStoneGroundShadow : MonoBehaviour
    {
        Transform disc;
        Mesh mesh;
        Material material;
        Bounds bounds;
        Vector3 directionToLight;
        bool visible=true;
        public bool Visible { get=>visible; set { visible=value; if(disc!=null) disc.gameObject.SetActive(value); } }
        public Transform Disc=>disc;

        public void Configure(Bounds localBounds,Vector3 keyLightDirection,bool show)
        {
            bounds=localBounds; directionToLight=keyLightDirection;
            if(disc==null)
            {
                disc=new GameObject("HLGroundShadow").transform; disc.SetParent(transform,false);
                disc.gameObject.layer=gameObject.layer;
                const int segments=32;
                var vertices=new Vector3[segments+1]; var triangles=new int[segments*3];
                for(int i=0;i<segments;i++)
                {
                    float angle=i*Mathf.PI*2/segments;
                    vertices[i+1]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    triangles[i*3]=0; triangles[i*3+1]=(i+1)%segments+1; triangles[i*3+2]=i+1;
                }
                mesh=new Mesh{name="HLShadowDisc"}; mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                disc.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="HLUltramarineShadow",renderQueue=2001};
                material.SetColor("_BaseColor",((Color)new Color32(43,75,143,255)).linear);
                var renderer=disc.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            }
            Visible=show; Refresh();
        }
        public void Refresh()
        {
            if(disc==null) return;
            Vector3 away=new Vector3(-directionToLight.x,0,-directionToLight.z);
            if(!float.IsFinite(away.sqrMagnitude) || away.sqrMagnitude<1e-6f) away=Vector3.forward;
            away.Normalize();
            Vector3 scale=transform.lossyScale;
            float width=Mathf.Max(.05f,Mathf.Max(bounds.size.x*Mathf.Abs(scale.x),bounds.size.z*Mathf.Abs(scale.z))*.55f);
            float length=Mathf.Max(width,bounds.size.y*Mathf.Abs(scale.y)*1.25f);
            Vector3 center=transform.TransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
            disc.position=center+away*length*.55f+Vector3.up*.012f;
            disc.rotation=Quaternion.LookRotation(away,Vector3.up);
            // Compensate parent scale: the projection stays flat even when the model is scaled.
            disc.localScale=Vector3.one;
            Vector3 inherited=disc.lossyScale;
            disc.localScale=new Vector3(width/Mathf.Max(.0001f,Mathf.Abs(inherited.x)),.001f/Mathf.Max(.0001f,Mathf.Abs(inherited.y)),length/Mathf.Max(.0001f,Mathf.Abs(inherited.z)));
        }
        void LateUpdate()=>Refresh();
        void OnEnable() { if(disc!=null) disc.gameObject.SetActive(visible); }
        void OnDisable() { if(disc!=null) disc.gameObject.SetActive(false); }
        void OnDestroy()
        {
            if(disc!=null) HLStoneMeshCache.DestroyOwned(disc.gameObject);
            HLStoneMeshCache.DestroyOwned(mesh); HLStoneMeshCache.DestroyOwned(material);
        }
    }
}

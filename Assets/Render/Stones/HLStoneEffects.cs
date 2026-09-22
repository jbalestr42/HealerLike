using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneEffects : MonoBehaviour
    {
        public const int MaxLiveFragments=256;
        sealed class Fragment
        {
            public HLStoneEffects Owner; public GameObject Object; public MeshFilter Filter; public MeshRenderer Renderer;
            public Vector3 Start,Velocity,Spin; public Quaternion Rotation; public float Age,Life,Ground;
            public bool Bounce,Split; public uint Seed; public Mesh OwnedMesh;
            public LinkedListNode<Fragment> GlobalNode;
        }
        static readonly LinkedList<Fragment> global=new LinkedList<Fragment>();
        static readonly Dictionary<Scene,HLStoneEffects> sceneOwners=new Dictionary<Scene,HLStoneEffects>();
        readonly List<Fragment> active=new List<Fragment>();
        readonly Stack<Fragment> pool=new Stack<Fragment>();
        [SerializeField] Material stoneMaterial;
        Material fallback,coral; Mesh cone;
        public int LiveCount => active.Count;
        public static int GlobalLiveCount => global.Count;
        public static HLStoneEffects ForScene(Scene scene,Material material)
        {
            if(sceneOwners.TryGetValue(scene,out var owner) && owner!=null) return owner;
            var go=new GameObject("HLStoneEffects"); SceneManager.MoveGameObjectToScene(go,scene);
            owner=go.AddComponent<HLStoneEffects>(); owner.stoneMaterial=material; sceneOwners[scene]=owner; return owner;
        }
        void EnsureAssets()
        {
            if(cone==null) cone=CreateCone();
            if(stoneMaterial==null)
            {
                fallback=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="HLPlaceholderStone"};
                fallback.SetColor("_BaseColor",HLStoneAssembly.Palette[2]); stoneMaterial=fallback;
            }
            if(coral==null)
            {
                coral=new Material(stoneMaterial){name="HLCoralSpark"}; coral.SetColor("_BaseColor",new Color32(242,96,122,255));
            }
        }
        static Mesh CreateCone()
        {
            var p=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f),Vector3.up};
            var faces=new[]{0,4,1,1,4,2,2,4,3,3,4,0,0,1,2,0,2,3};
            var v=new Vector3[18]; var normals=new Vector3[18]; var indices=new int[18];
            for(int i=0;i<18;i+=3)
            {
                Vector3 n=Vector3.Cross(p[faces[i+1]]-p[faces[i]],p[faces[i+2]]-p[faces[i]]).normalized;
                for(int j=0;j<3;j++){v[i+j]=p[faces[i+j]]; normals[i+j]=n; indices[i+j]=i+j;}
            }
            var mesh=new Mesh{name="HLFlatCone"}; mesh.vertices=v; mesh.normals=normals; mesh.triangles=indices; mesh.RecalculateBounds(); return mesh;
        }
        Fragment Spawn(Mesh mesh,Material material,Vector3 position,Quaternion rotation,Vector3 scale,Vector3 velocity,float life,float ground,bool bounce,uint seed,Mesh owned=null,bool split=false)
        {
            while(global.Count>=MaxLiveFragments) global.First.Value.Owner.Release(global.First.Value);
            Fragment f;
            if(pool.Count>0) f=pool.Pop();
            else
            {
                var go=new GameObject("HLStoneFragment"); go.transform.SetParent(transform,false);
                f=new Fragment{Owner=this,Object=go,Filter=go.AddComponent<MeshFilter>(),Renderer=go.AddComponent<MeshRenderer>()};
            }
            f.Filter.sharedMesh=mesh; f.Renderer.sharedMaterial=material; f.Object.transform.SetPositionAndRotation(position,rotation);
            f.Object.transform.localScale=scale; f.Object.SetActive(true);
            f.Start=position; f.Rotation=rotation; f.Velocity=velocity; f.Spin=new Vector3(70,120,45); f.Age=0; f.Life=life;
            f.Ground=ground; f.Bounce=bounce; f.Seed=seed; f.OwnedMesh=owned; f.Split=split;
            f.GlobalNode=global.AddLast(f); active.Add(f); return f;
        }
        void Release(Fragment f)
        {
            active.Remove(f); global.Remove(f.GlobalNode); f.Object.SetActive(false); f.Filter.sharedMesh=null;
            HLStoneMeshCache.DestroyOwned(f.OwnedMesh); f.OwnedMesh=null; pool.Push(f);
        }
        static Vector3 Direction(ref HLStoneRandom r,Vector3 normal)
        {
            Vector3 v=new Vector3(r.Range(-1,1),r.Range(.2f,1),r.Range(-1,1));
            if(Vector3.Dot(v,normal)<0) v-=2*Vector3.Dot(v,normal)*normal;
            Vector3 upwardTangent=Vector3.up-normal*Vector3.Dot(Vector3.up,normal);
            return (v+normal*.5f+upwardTangent*.3f).normalized;
        }
        public void EmitHit(in HLStoneImpact impact,bool critical,uint seed)
        {
            EnsureAssets(); var r=new HLStoneRandom(seed); int sparks=critical?9:6,shards=critical?5:3;
            Vector3 normal=impact.NormalWS.sqrMagnitude>0?impact.NormalWS.normalized:Vector3.up;
            for(int i=0;i<sparks+shards;i++)
            {
                bool spark=i<sparks; float size=r.Range(.025f,.07f);
                Spawn(cone,spark || i%3==0?coral:stoneMaterial,impact.PointWS+normal*.005f,Quaternion.FromToRotation(Vector3.up,normal),
                    spark?new Vector3(size*.25f,size,size*.25f):Vector3.one*size,
                    Direction(ref r,normal)*r.Range(.6f,1.4f),spark?r.Range(.12f,.22f):r.Range(.35f,.55f),impact.PointWS.y-1,false,r.Next());
            }
        }
        public void EmitDetachedPart(Mesh mesh,Material material,in Matrix4x4 pose,Vector3 velocityWS,float groundY,uint seed)
        {
            EnsureAssets(); var r=new HLStoneRandom(seed);
            // A copy belongs to the effects owner, so releasing the enemy's cache lease cannot invalidate it.
            Mesh copy=Instantiate(mesh); copy.name="HLDetachedStone";
            Vector3 direction=new Vector3(r.Range(-1,1),0,r.Range(-1,1)).normalized;
            Spawn(copy,material!=null?material:stoneMaterial,pose.GetColumn(3),pose.rotation,pose.lossyScale,
                velocityWS+direction*r.Range(.6f,1.2f)+Vector3.up*.2f,.25f,groundY,false,seed,copy,true);
        }
        public void CollapseOnce(HLStoneEnemyVisual visual,uint seed)
        {
            if(visual==null || !visual.TryBeginCollapse()) return;
            EnsureAssets(); var r=new HLStoneRandom(seed); var surviving=new List<HLStoneAssembly.Part>();
            foreach(var p in visual.Parts) if(p.Transform.gameObject.activeSelf) surviving.Add(p);
            int count=surviving.Count==0?0:12;
            for(int i=0;i<count;i++)
            {
                var part=surviving[i%surviving.Count];
                Vector3 position=part.Transform.TransformPoint(part.Lease.Data.Vertices[(int)(r.Next()%(uint)part.Lease.Data.Vertices.Length)]);
                Spawn(cone,i%4==0?coral:stoneMaterial,position,part.Transform.rotation,Vector3.one*r.Range(.06f,.16f),
                    visual.PlanarVelocity+new Vector3(r.Range(-.8f,.8f),r.Range(.3f,1),r.Range(-.8f,.8f)),.8f,visual.GroundY,true,r.Next());
            }
            visual.HideParts();
        }
        public static Vector3 PositionAt(Vector3 start,Vector3 velocity,float age,float ground,bool bounce)
        {
            const float gravity=8;
            Vector3 position=start+velocity*age+Vector3.down*(.5f*gravity*age*age);
            if(!bounce) return position;
            float height=Mathf.Max(0,start.y-ground);
            float hit=(velocity.y+Mathf.Sqrt(velocity.y*velocity.y+2*gravity*height))/gravity;
            if(age>=hit)
            {
                float t=age-hit,up=(gravity*hit-velocity.y)*.3f;
                position.y=Mathf.Max(ground,ground+up*t-.5f*gravity*t*t);
            }
            return position;
        }
        public void Advance(float deltaTime)
        {
            for(int i=active.Count-1;i>=0;i--)
            {
                var f=active[i]; f.Age+=Mathf.Max(0,deltaTime);
                f.Object.transform.position=PositionAt(f.Start,f.Velocity,f.Age,f.Ground,f.Bounce);
                f.Object.transform.rotation=f.Rotation*Quaternion.Euler(f.Spin*f.Age);
                if(f.Age<f.Life) continue;
                bool split=f.Split; Vector3 p=f.Object.transform.position; float ground=f.Ground; uint seed=f.Seed;
                Release(f);
                if(split)
                {
                    var r=new HLStoneRandom(seed);
                    for(int j=0;j<3;j++) Spawn(cone,stoneMaterial,p,Quaternion.identity,Vector3.one*r.Range(.04f,.07f),
                        new Vector3(r.Range(-.3f,.3f),.3f,r.Range(-.3f,.3f)),.25f,ground,true,r.Next());
                }
            }
        }
        void Update() => Advance(Time.deltaTime);
        void OnDestroy()
        {
            while(active.Count>0) Release(active[active.Count-1]);
            foreach(var f in pool) if(f.Object!=null) HLStoneMeshCache.DestroyOwned(f.Object);
            pool.Clear(); HLStoneMeshCache.DestroyOwned(cone); HLStoneMeshCache.DestroyOwned(coral); HLStoneMeshCache.DestroyOwned(fallback);
            if(sceneOwners.TryGetValue(gameObject.scene,out var owner) && owner==this) sceneOwners.Remove(gameObject.scene);
        }
    }
}

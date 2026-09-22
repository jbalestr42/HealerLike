using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    /// <summary>Small primitive assemblies, no textures, colliders, random state or imported geometry.</summary>
    public static class HLSpellPrimitives
    {
        public static HLSpellEffectKind Kind(HLSpellSignature s)
        {
            if (s.operation == HLOperation.Prevention || s.operation == HLOperation.Attribute && (s.attribute == AttributeType.HitArmor || s.attribute == AttributeType.PercentArmor || s.attribute == AttributeType.FlatArmor)) return HLSpellEffectKind.Shield;
            if (s.operation == HLOperation.Resource)
            {
                if (s.tempo == HLTempo.HandlerTick) return HLSpellEffectKind.Drip;
                if (s.attribute == AttributeType.ManaMax) return HLSpellEffectKind.Mana;
                return s.sign == HLSign.Positive ? HLSpellEffectKind.Heal : HLSpellEffectKind.Impact;
            }
            return HLSpellEffectKind.Buff;
        }
        static Mesh _torus, _cone;
        static Material _fallback;
        static int _users;
        public static void Retain() => _users++;
        public static void ReleaseUser()
        {
            if (_users > 0) _users--;
            Release();
        }
        /// <summary>Release owned native resources only after all sinks and effects are gone.</summary>
        public static void Release()
        {
            if (_users != 0) return;
            Dispose(_torus); Dispose(_cone); Dispose(_fallback);
            _torus = null; _cone = null; _fallback = null;
        }
        static void Dispose(Object resource)
        {
            if (!resource) return;
#if UNITY_EDITOR
            // Authoring or external tools may persist a mesh returned by the public cache.
            if (UnityEditor.EditorUtility.IsPersistent(resource)) return;
#endif
            if (Application.isPlaying) Object.Destroy(resource);
            else Object.DestroyImmediate(resource);
        }
        public static void Build(HLSpellEffect effect)
        {
            effect.RetainPrimitives();
            var parts = new List<Transform>();
            Color gold = new Color32(242,194,48,255), lime = new Color32(198,242,74,255), coral = new Color32(242,96,122,255);
            if (effect.kind == HLSpellEffectKind.Buff)
            {
                for (int i = 0; i < 3; i++)
                    parts.Add(Part(effect, Torus, gold, Vector3.up * (i - 1) * .12f, Vector3.one * (.55f + i * .16f), Quaternion.Euler(30 + i * 23, i * 60, 18)));
            }
            else if (effect.kind == HLSpellEffectKind.Area)
                parts.Add(Part(effect, Torus, lime, Vector3.up*.03f, Vector3.one*2, Quaternion.identity));
            else if (effect.kind == HLSpellEffectKind.Drip)
            {
                for (int i = 0; i < 5; i++)
                    parts.Add(Primitive(effect, PrimitiveType.Sphere, coral, new Vector3(Mathf.Cos(i*2.4f)*.25f,.2f,Mathf.Sin(i*2.4f)*.25f), new Vector3(.05f,.11f,.05f), Quaternion.identity));
            }
            else if (effect.kind == HLSpellEffectKind.Shield)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3;
                    parts.Add(Primitive(effect, PrimitiveType.Sphere, gold, new Vector3(Mathf.Cos(a)*.4f,0,Mathf.Sin(a)*.4f), new Vector3(.12f,.85f,.32f), Quaternion.Euler(0,-i*60,20)));
                }
            }
            else if (effect.kind == HLSpellEffectKind.Heal || effect.kind == HLSpellEffectKind.Mana)
            {
                for (int i = 0; i < 7; i++)
                {
                    float a = i * 2.4f; var p = new Vector3(Mathf.Cos(a)*.25f, i*.035f, Mathf.Sin(a)*.25f);
                    parts.Add(effect.kind == HLSpellEffectKind.Mana ? Part(effect, Torus, gold, p, Vector3.one*.12f, Quaternion.Euler(90,0,0)) : Primitive(effect, PrimitiveType.Sphere, lime, p, Vector3.one*(.065f+i*.006f), Quaternion.identity));
                }
            }
            else if (effect.kind == HLSpellEffectKind.Impact)
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4; var p = new Vector3(Mathf.Cos(a),Mathf.Sin(a), .1f) * .2f;
                    parts.Add(Part(effect, Cone, coral, p, new Vector3(.045f,.16f,.045f), Quaternion.FromToRotation(Vector3.up,p)));
                    if (i % 2 == 0)
                    {
                        parts.Add(Primitive(effect, PrimitiveType.Cube, coral, p*.7f, new Vector3(.025f,.17f,.025f), Quaternion.Euler(0,0,45)));
                        parts.Add(Primitive(effect, PrimitiveType.Cube, coral, p*.7f, new Vector3(.025f,.17f,.025f), Quaternion.Euler(0,0,-45)));
                    }
                }
            }
            else for (int i = 0; i < 16; i++)
            {
                parts.Add(Primitive(effect, PrimitiveType.Cylinder, gold, Vector3.right*i*.1f, new Vector3(.025f,.05f,.025f), Quaternion.Euler(0,0,90)));
                parts.Add(Primitive(effect, PrimitiveType.Sphere, gold, Vector3.right*i*.1f, Vector3.one*.055f, Quaternion.identity));
            }
            effect.parts = parts.ToArray();
        }
        public static Renderer SideRim(HLSpellEffect effect) => Part(effect, Torus, new Color32(201,196,180,255), Vector3.down*.18f, new Vector3(.72f,.2f,.72f), Quaternion.identity).GetComponent<Renderer>();
        public static void AddCritical(HLSpellEffect effect)
        {
            var color = effect.kind == HLSpellEffectKind.Impact ? new Color32(242,96,122,255) : new Color32(198,242,74,255);
            Part(effect, Torus, color, Vector3.zero, Vector3.one*.7f, Quaternion.Euler(90,0,0));
            Part(effect, Torus, color, Vector3.zero, Vector3.one*.85f, Quaternion.Euler(90,0,0));
        }
        public static Transform[] StackBeads(HLSpellEffect effect)
        {
            var beads = new Transform[8];
            for (int i = 0; i < beads.Length; i++) beads[i] = Primitive(effect, PrimitiveType.Sphere, new Color32(242,194,48,255), new Vector3((i-3.5f)*.055f,.48f,0), Vector3.one*.035f, Quaternion.identity);
            return beads;
        }
        public static void AddMarker(HLSpellEffect effect, HLSpellSignature signature)
        {
            bool harm = signature.sign == HLSign.Negative;
            Color color = harm ? new Color32(242,96,122,255) : new Color32(242,194,48,255);
            Vector3 p = new Vector3(.36f, 0, 0);
            if (signature.operation == HLOperation.Resource)
            {
                if (harm) Part(effect, Cone, color, p, new Vector3(.07f,.16f,.07f), Quaternion.Euler(0,0,90));
                else Primitive(effect, PrimitiveType.Sphere, new Color32(198,242,74,255), p, Vector3.one*.1f, Quaternion.identity);
                return;
            }
            if (!signature.hasAttribute) return;
            switch(signature.attribute)
            {
                case AttributeType.Damage: case AttributeType.FlatArmor: case AttributeType.Vulnerability:
                    Part(effect, Cone, color, p, new Vector3(.07f,.16f,.07f), Quaternion.Euler(0,0,harm ? 90 : -90)); break;
                case AttributeType.ManaMax: case AttributeType.Range: case AttributeType.HealthMax:
                    Part(effect, Torus, color, p, Vector3.one*.2f, Quaternion.Euler(90,0,0)); break;
                case AttributeType.CriticalChance: case AttributeType.CriticalMultiplier: case AttributeType.CriticalChanceResist:
                    Primitive(effect, PrimitiveType.Sphere, color, p+Vector3.up*.06f, Vector3.one*.065f, Quaternion.identity);
                    Primitive(effect, PrimitiveType.Sphere, color, p-Vector3.up*.06f, Vector3.one*.065f, Quaternion.identity); break;
                default: Primitive(effect, PrimitiveType.Sphere, color, p, Vector3.one*.1f, Quaternion.identity); break;
            }
        }
        static Transform Primitive(HLSpellEffect effect, PrimitiveType type, Color color, Vector3 p, Vector3 scale, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(type); var collider = go.GetComponent<Collider>();
            if (collider) { collider.enabled = false; if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider); }
            return Finish(effect, go, color, p, scale, rotation);
        }
        static Transform Part(HLSpellEffect effect, Mesh mesh, Color color, Vector3 p, Vector3 scale, Quaternion rotation)
        {
            var go = new GameObject("HLPrimitive", typeof(MeshFilter), typeof(MeshRenderer)); go.GetComponent<MeshFilter>().sharedMesh = mesh;
            return Finish(effect, go, color, p, scale, rotation);
        }
        static Transform Finish(HLSpellEffect effect, GameObject go, Color color, Vector3 p, Vector3 scale, Quaternion rotation)
        {
            go.name = "HLPrimitive"; go.transform.SetParent(effect.transform, false); go.transform.localPosition = p; go.transform.localScale = scale; go.transform.localRotation = rotation;
            var renderer = go.GetComponent<Renderer>();
            if (!effect.material && !_fallback) _fallback = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "HLSpellFallback", enableInstancing = true };
            renderer.sharedMaterial = effect.material ? effect.material : _fallback;
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block);
            return go.transform;
        }
        public static Mesh Torus => _torus ? _torus : _torus = CreateTorus();
        public static Mesh Cone => _cone ? _cone : _cone = CreateCone();
        static Mesh CreateTorus()
        {
            const int rings = 32, sides = 6; var v = new Vector3[rings*sides]; var t = new int[rings*sides*6];
            for (int i=0;i<rings;i++) for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/rings,b=j*Mathf.PI*2/sides; int k=i*sides+j;
                v[k]=new Vector3((.5f+.025f*Mathf.Cos(b))*Mathf.Cos(a), .025f*Mathf.Sin(b), (.5f+.025f*Mathf.Cos(b))*Mathf.Sin(a));
                int n=((i+1)%rings)*sides+j, q=i*sides+(j+1)%sides, r=((i+1)%rings)*sides+(j+1)%sides;
                int o=k*6; t[o]=k;t[o+1]=q;t[o+2]=n;t[o+3]=q;t[o+4]=r;t[o+5]=n;
            }
            var mesh=new Mesh { name="HLTorus" };mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh CreateCone()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4; var p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                int k=v.Count;v.Add(p);v.Add(Vector3.up);v.Add(q);v.Add(p);v.Add(q);v.Add(Vector3.zero);for(int j=0;j<6;j++)t.Add(k+j);
            }
            var mesh=new Mesh { name="HLCone" };mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}

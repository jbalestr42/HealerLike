using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public static class HLSpellPrimitives
    {
        static Mesh _torus;
        static Mesh _cone;
        static Mesh _star;
        static Mesh _boulder;
        static Material _fallback;
        static int _users;

        public static Mesh torus
        {
            get
            {
                if (!_torus)
                {
                    _torus = CreateTorus();
                }
                return _torus;
            }
        }

        public static Mesh cone
        {
            get
            {
                if (!_cone)
                {
                    _cone = CreateCone();
                }
                return _cone;
            }
        }

        public static Mesh star
        {
            get
            {
                if (!_star)
                {
                    _star = CreateStar();
                }
                return _star;
            }
        }

        public static Mesh boulder
        {
            get
            {
                if (!_boulder)
                {
                    _boulder = CreateBoulder();
                }
                return _boulder;
            }
        }

        public static HLSpellEffectKind Kind(HLSpellSignature s)
        {
            if (
                s.operation == HLOperation.Prevention
                || s.operation == HLOperation.Attribute
                    && (
                        s.attribute == AttributeType.HitArmor
                        || s.attribute == AttributeType.PercentArmor
                        || s.attribute == AttributeType.FlatArmor
                    )
            )
            {
                return HLSpellEffectKind.Shield;
            }
            if (s.operation == HLOperation.Resource)
            {
                if (s.tempo == HLTempo.HandlerTick && s.sign != HLSign.Positive)
                {
                    return HLSpellEffectKind.Drip;
                }
                if (s.attribute == AttributeType.ManaMax)
                {
                    return HLSpellEffectKind.Mana;
                }
                return s.sign == HLSign.Positive ? HLSpellEffectKind.Heal : HLSpellEffectKind.Impact;
            }
            return HLSpellEffectKind.Buff;
        }

        public static void Retain()
        {
            _users++;
        }

        public static void ReleaseUser()
        {
            if (_users > 0)
            {
                _users--;
            }
            Release();
        }

        public static void Release()
        {
            if (_users != 0)
            {
                return;
            }
            Dispose(_torus);
            Dispose(_cone);
            Dispose(_star);
            Dispose(_boulder);
            Dispose(_fallback);
            _torus = null;
            _cone = null;
            _star = null;
            _boulder = null;
            _fallback = null;
        }

        static void Dispose(Object resource)
        {
            if (!resource)
            {
                return;
            }
#if UNITY_EDITOR
            // Authoring or external tools may persist a mesh returned by the public cache.
            if (UnityEditor.EditorUtility.IsPersistent(resource))
            {
                return;
            }
#endif
            if (Application.isPlaying)
            {
                Object.Destroy(resource);
            }
            else
            {
                Object.DestroyImmediate(resource);
            }
        }

        public static void Build(HLSpellEffect effect)
        {
            effect.RetainPrimitives();
            List<Transform> parts = new List<Transform>();
            Color gold = new Color32(242, 194, 48, 255);
            Color lime = new Color32(198, 242, 74, 255);
            Color coral = new Color32(242, 96, 122, 255);
            if (effect.kind == HLSpellEffectKind.Buff)
            {
                for (int i = 0; i < 3; i++)
                {
                    parts.Add(
                        Part(
                            effect,
                            torus,
                            gold,
                            Vector3.up * (i - 1) * 0.12f,
                            Vector3.one * (0.55f + i * 0.16f),
                            Quaternion.Euler(30f + i * 23f, i * 60f, 18f)
                        )
                    );
                }
            }
            else if (effect.kind == HLSpellEffectKind.Area)
            {
                Vector3 scale = new Vector3(2f, 0.3f, 2f);
                parts.Add(Part(effect, torus, lime, Vector3.up * 0.03f, scale, Quaternion.identity));
            }
            else if (effect.kind == HLSpellEffectKind.Drip)
            {
                for (int i = 0; i < 5; i++)
                {
                    parts.Add(
                        Primitive(
                            effect,
                            PrimitiveType.Sphere,
                            coral,
                            new Vector3(Mathf.Cos(i * 2.4f) * 0.25f, 0.2f, Mathf.Sin(i * 2.4f) * 0.25f),
                            new Vector3(0.05f, 0.11f, 0.05f),
                            Quaternion.identity
                        )
                    );
                }
            }
            else if (effect.kind == HLSpellEffectKind.Shield)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    parts.Add(
                        Primitive(
                            effect,
                            PrimitiveType.Sphere,
                            gold,
                            new Vector3(Mathf.Cos(a) * 0.4f, 0f, Mathf.Sin(a) * 0.4f),
                            new Vector3(0.14f, 0.9f, 0.46f),
                            Quaternion.Euler(0f, -i * 60f, 12f)
                        )
                    );
                }
            }
            else if (effect.kind == HLSpellEffectKind.Litter)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 2.4f;
                    float r = 0.3f + i * 0.11f;
                    parts.Add(
                        Part(
                            effect,
                            boulder,
                            new Color32(58, 66, 87, 255),
                            new Vector3(Mathf.Cos(a) * r, 0.06f, Mathf.Sin(a) * r),
                            new Vector3(0.12f + i * 0.009f, 0.09f, 0.10f),
                            Quaternion.Euler(i * 17f, i * 43f, 12f)
                        )
                    );
                }
            }
            else if (effect.kind == HLSpellEffectKind.Heal || effect.kind == HLSpellEffectKind.Mana)
            {
                if (effect.kind == HLSpellEffectKind.Heal)
                {
                    effect.stalks = new Transform[7];
                }
                for (int i = 0; i < 7; i++)
                {
                    float a = i * 2.4f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * 0.25f, i * 0.035f, Mathf.Sin(a) * 0.25f);
                    if (effect.kind == HLSpellEffectKind.Mana)
                    {
                        parts.Add(Part(effect, torus, gold, p, Vector3.one * 0.12f, Quaternion.Euler(90f, 0f, 0f)));
                    }
                    else
                    {
                        Vector3 scale = Vector3.one * (0.065f + i * 0.006f);
                        parts.Add(Primitive(effect, PrimitiveType.Sphere, lime, p, scale, Quaternion.identity));
                    }
                    if (effect.kind == HLSpellEffectKind.Heal)
                    {
                        effect.stalks[i] = Primitive(
                            effect,
                            PrimitiveType.Cylinder,
                            lime,
                            new Vector3(p.x, (p.y + 0.12f) * 0.5f - 0.12f, p.z),
                            new Vector3(0.009f, (p.y + 0.12f) * 0.5f, 0.009f),
                            Quaternion.identity
                        );
                    }
                }
            }
            else if (effect.kind == HLSpellEffectKind.Impact)
            {
                parts.Add(Part(effect, star, coral, Vector3.zero, Vector3.one * 0.28f, Quaternion.identity));
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 2.4f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * 0.14f, 0.08f + i * 0.025f, Mathf.Sin(a) * 0.14f);
                    parts.Add(
                        Part(
                            effect,
                            cone,
                            coral,
                            p,
                            new Vector3(0.035f, 0.12f, 0.035f),
                            Quaternion.FromToRotation(Vector3.up, p)
                        )
                    );
                }
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    parts.Add(
                        Primitive(
                            effect,
                            PrimitiveType.Cylinder,
                            gold,
                            Vector3.right * i * 0.1f,
                            new Vector3(0.025f, 0.05f, 0.025f),
                            Quaternion.Euler(0f, 0f, 90f)
                        )
                    );
                    parts.Add(
                        Primitive(
                            effect,
                            PrimitiveType.Sphere,
                            gold,
                            Vector3.right * i * 0.1f,
                            Vector3.one * 0.055f,
                            Quaternion.identity
                        )
                    );
                }
            }
            effect.parts = parts.ToArray();
        }

        public static Renderer SideRim(HLSpellEffect effect)
        {
            return Part(
                    effect,
                    torus,
                    new Color32(201, 196, 180, 255),
                    Vector3.down * 0.18f,
                    new Vector3(0.72f, 0.2f, 0.72f),
                    Quaternion.identity
                )
                .GetComponent<Renderer>();
        }

        public static void AddCritical(HLSpellEffect effect)
        {
            Color color = new Color32(198, 242, 74, 255);
            if (effect.kind == HLSpellEffectKind.Impact)
            {
                color = new Color32(242, 96, 122, 255);
            }
            Part(effect, torus, color, Vector3.zero, Vector3.one * 0.7f, Quaternion.Euler(90f, 0f, 0f));
            Part(effect, torus, color, Vector3.zero, Vector3.one * 0.85f, Quaternion.Euler(90f, 0f, 0f));
        }

        public static Transform[] StackBeads(HLSpellEffect effect)
        {
            Transform[] beads = new Transform[8];
            for (int i = 0; i < beads.Length; i++)
            {
                beads[i] = Primitive(
                    effect,
                    PrimitiveType.Sphere,
                    new Color32(242, 194, 48, 255),
                    new Vector3((i - 3.5f) * 0.055f, 0.48f, 0f),
                    Vector3.one * 0.035f,
                    Quaternion.identity
                );
            }
            return beads;
        }

        public static void AddMarker(HLSpellEffect effect, HLSpellSignature signature)
        {
            bool harm = signature.sign == HLSign.Negative;
            Color color = harm ? new Color32(242, 96, 122, 255) : new Color32(242, 194, 48, 255);
            Vector3 p = new Vector3(0.36f, 0f, 0f);
            if (signature.operation == HLOperation.Resource)
            {
                if (harm)
                {
                    Part(effect, cone, color, p, new Vector3(0.07f, 0.16f, 0.07f), Quaternion.Euler(0f, 0f, 90f));
                }
                else
                {
                    Primitive(
                        effect,
                        PrimitiveType.Sphere,
                        new Color32(198, 242, 74, 255),
                        p,
                        Vector3.one * 0.1f,
                        Quaternion.identity
                    );
                }
                return;
            }
            if (!signature.hasAttribute)
            {
                return;
            }
            switch (signature.attribute)
            {
                case AttributeType.Damage:
                case AttributeType.FlatArmor:
                case AttributeType.Vulnerability:
                    Part(
                        effect,
                        cone,
                        color,
                        p,
                        new Vector3(0.07f, 0.16f, 0.07f),
                        Quaternion.Euler(0f, 0f, harm ? 90f : -90f)
                    );
                    break;
                case AttributeType.ManaMax:
                case AttributeType.Range:
                case AttributeType.HealthMax:
                    Part(effect, torus, color, p, Vector3.one * 0.2f, Quaternion.Euler(90f, 0f, 0f));
                    break;
                case AttributeType.CriticalChance:
                case AttributeType.CriticalMultiplier:
                case AttributeType.CriticalChanceResist:
                    Primitive(
                        effect,
                        PrimitiveType.Sphere,
                        color,
                        p + Vector3.up * 0.06f,
                        Vector3.one * 0.065f,
                        Quaternion.identity
                    );
                    Primitive(
                        effect,
                        PrimitiveType.Sphere,
                        color,
                        p - Vector3.up * 0.06f,
                        Vector3.one * 0.065f,
                        Quaternion.identity
                    );
                    break;
                default:
                    Primitive(effect, PrimitiveType.Sphere, color, p, Vector3.one * 0.1f, Quaternion.identity);
                    break;
            }
        }

        static Transform Primitive(
            HLSpellEffect effect,
            PrimitiveType type,
            Color color,
            Vector3 p,
            Vector3 scale,
            Quaternion rotation
        )
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Collider collider = go.GetComponent<Collider>();
            if (collider)
            {
                collider.enabled = false;
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }
            return Finish(effect, go, color, p, scale, rotation);
        }

        static Transform Part(
            HLSpellEffect effect,
            Mesh mesh,
            Color color,
            Vector3 p,
            Vector3 scale,
            Quaternion rotation
        )
        {
            GameObject go = new GameObject("HLPrimitive", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            return Finish(effect, go, color, p, scale, rotation);
        }

        static Transform Finish(
            HLSpellEffect effect,
            GameObject go,
            Color color,
            Vector3 p,
            Vector3 scale,
            Quaternion rotation
        )
        {
            go.name = "HLPrimitive";
            go.transform.SetParent(effect.transform, false);
            go.transform.localPosition = p;
            go.transform.localScale = scale;
            go.transform.localRotation = rotation;
            Renderer renderer = go.GetComponent<Renderer>();
            if (!effect.material && !_fallback)
            {
                _fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "HLSpellFallback",
                    enableInstancing = true
                };
            }
            renderer.sharedMaterial = effect.material ? effect.material : _fallback;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
            return go.transform;
        }

        static Mesh CreateStar()
        {
            // Two-sided planar triangle fan: eight long rays alternating with short notches.
            Vector3[] vertices = new Vector3[34];
            int[] triangles = new int[96];
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                float r = i % 2 == 0 ? 1f : 0.32f;
                vertices[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                vertices[i + 18] = vertices[i + 1];
                int next = (i + 1) % 16 + 1;
                int o = i * 6;
                triangles[o] = 0;
                triangles[o + 1] = i + 1;
                triangles[o + 2] = next;
                triangles[o + 3] = 17;
                triangles[o + 4] = next + 17;
                triangles[o + 5] = i + 18;
            }
            Mesh mesh = new Mesh { name = "HLStar" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CreateBoulder()
        {
            // Flat-shaded octahedron, kept deliberately asymmetric.
            Vector3[] vertices = new Vector3[24];
            int[] triangles = new int[24];
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                float b = (i + 1) * Mathf.PI * 0.5f;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 q = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b));
                int k = i * 6;
                vertices[k] = p;
                vertices[k + 1] = new Vector3(0.15f, 1f, 0f);
                vertices[k + 2] = q;
                vertices[k + 3] = q;
                vertices[k + 4] = new Vector3(-0.1f, -0.7f, 0.1f);
                vertices[k + 5] = p;
                for (int j = 0; j < 6; j++)
                {
                    triangles[k + j] = k + j;
                }
            }
            Mesh mesh = new Mesh { name = "HLBoulder" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CreateTorus()
        {
            int rings = 32;
            int sides = 6;
            Vector3[] v = new Vector3[rings * sides];
            int[] t = new int[rings * sides * 6];
            for (int i = 0; i < rings; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    float a = i * Mathf.PI * 2f / rings;
                    float b = j * Mathf.PI * 2f / sides;
                    int k = i * sides + j;
                    v[k] = new Vector3(
                        (0.5f + 0.025f * Mathf.Cos(b)) * Mathf.Cos(a),
                        0.025f * Mathf.Sin(b),
                        (0.5f + 0.025f * Mathf.Cos(b)) * Mathf.Sin(a)
                    );
                    int n = ((i + 1) % rings) * sides + j;
                    int q = i * sides + (j + 1) % sides;
                    int r = ((i + 1) % rings) * sides + (j + 1) % sides;
                    int o = k * 6;
                    t[o] = k;
                    t[o + 1] = q;
                    t[o + 2] = n;
                    t[o + 3] = q;
                    t[o + 4] = r;
                    t[o + 5] = n;
                }
            }
            Mesh mesh = new Mesh { name = "HLTorus" };
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CreateCone()
        {
            List<Vector3> v = new List<Vector3>();
            List<int> t = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                float b = (i + 1) * Mathf.PI / 4f;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 q = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b));
                int k = v.Count;
                v.Add(p);
                v.Add(Vector3.up);
                v.Add(q);
                v.Add(p);
                v.Add(q);
                v.Add(Vector3.zero);
                for (int j = 0; j < 6; j++)
                {
                    t.Add(k + j);
                }
            }
            Mesh mesh = new Mesh { name = "HLCone" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

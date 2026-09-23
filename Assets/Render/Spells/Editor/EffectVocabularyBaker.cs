using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Reads the ten effect prefabs into the effect vocabulary and authors Press and Crack, which no prefab drew.
    // -executeMethod HealerLike.Render.Spells.EffectVocabularyBaker.Bake
    public static class EffectVocabularyBaker
    {
        public static readonly string VocabularyPath = "Assets/Render/Spells/Data/EffectVocabulary.asset";
        static readonly string prefabFolder = "Assets/Render/Spells/Prefabs/";
        static readonly string palettePath = "Assets/Render/Grammar/Data/LookPalette.asset";
        static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
        // The prefabs were drawn for a 1.35 scale on a body of 0.275 radius, one prefab unit is five body radii
        static readonly float bodyPerPrefab = 5f;
        static readonly float orbitRadius = 1.35f;
        static readonly float plateRadius = 1.1f;
        static readonly float budRadius = 0.9f;
        static readonly float feetRadius = 1.5f;
        static readonly float dripRadius = 0.3f;

        [MenuItem("Tools/Render/Bake Effect Vocabulary")]
        public static void Bake()
        {
            PrimitiveMeshes meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath);
            LookPalette palette = AssetDatabase.LoadAssetAtPath<LookPalette>(palettePath);
            if (meshes == null || palette == null)
            {
                Debug.LogError($"[EffectVocabularyBaker] Needs {meshesPath} and {palettePath}.");
                return;
            }

            EffectVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<EffectVocabulary>(VocabularyPath);
            bool isNew = vocabulary == null;
            if (isNew)
            {
                vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            }

            vocabulary.palette = palette;
            vocabulary.elements = new Dictionary<EffectElement, ElementEntry>();
            Add(vocabulary, EffectElement.Burst, Burst(meshes));
            Add(vocabulary, EffectElement.Rise, Rise(meshes));
            Add(vocabulary, EffectElement.Stalks, Stalks(meshes));
            Add(vocabulary, EffectElement.Drips, Drips(meshes));
            Add(vocabulary, EffectElement.Orbit, Orbit(meshes));
            Add(vocabulary, EffectElement.Plates, Plates(meshes));
            Add(vocabulary, EffectElement.Bud, Bud(meshes));
            Add(vocabulary, EffectElement.Press, Press(meshes));
            Add(vocabulary, EffectElement.Crack, Crack(meshes));
            Add(vocabulary, EffectElement.ManaUp, Mana("Resolved_ManaMaxPositive", EffectMotion.Rise, meshes));
            Add(vocabulary, EffectElement.ManaDown, Mana("Resolved_ManaMaxNegative", EffectMotion.Fall, meshes));
            Add(vocabulary, EffectElement.Beam, World("Fx_ChainBeam", EffectMotion.Grow, EffectSocket.Link, 0.6f, meshes));
            Add(vocabulary, EffectElement.Ring, World("Fx_HealRing", EffectMotion.Grow, EffectSocket.Ground, 0.8f, meshes));
            Add(vocabulary, EffectElement.Litter, Litter(meshes));

            if (isNew)
            {
                AssetDatabase.CreateAsset(vocabulary, VocabularyPath);
            }
            else
            {
                EditorUtility.SetDirty(vocabulary);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EffectVocabularyBaker] Baked {vocabulary.elements.Count} elements into {VocabularyPath}");
        }

        static void Add(EffectVocabulary vocabulary, EffectElement element, ElementEntry entry)
        {
            if (entry == null)
            {
                Debug.LogError($"[EffectVocabularyBaker] {element} was not baked.");
                return;
            }

            vocabulary.elements[element] = entry;
        }

        static ElementEntry Burst(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Fx_Impact", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            Set(entry, EffectMotion.Burst, EffectSocket.Body, EffectCount.Fixed, 1, 0.3f);
            return entry;
        }

        static ElementEntry Rise(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Fx_HealSpheres", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            List<LookPart> spheres = Shapes(entry, PartRole.Body);
            // The prefab has seven spheres, the count runs to eight
            LookPart extra = spheres[0];
            extra.position = new Vector3(-extra.position.x, extra.position.y, -extra.position.z);
            spheres.Add(extra);
            for (int i = 0; i < spheres.Count; i++)
            {
                LookPart sphere = Radial(spheres[i], feetRadius);
                sphere.size *= 1.3f;
                spheres[i] = sphere;
            }

            entry.parts = spheres.ToArray();
            Set(entry, EffectMotion.Rise, EffectSocket.Feet, EffectCount.Amount, 3, 0.6f);
            return entry;
        }

        static ElementEntry Stalks(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Fx_HealSpheres", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            List<LookPart> spheres = ByAngle(Shapes(entry, PartRole.Body));
            List<LookPart> stalks = ByAngle(Shapes(entry, PartRole.Stem));
            List<LookPart> parts = new List<LookPart>();
            for (int i = 0; i < spheres.Count; i++)
            {
                LookPart sphere = Radial(spheres[i], feetRadius);
                // Heights from 0.8 to 1.6 body radii, the stalk runs from the ground to its sphere
                sphere.position.y = 0.8f + 0.8f * i / Mathf.Max(1, spheres.Count - 1);
                sphere.size *= 1.2f;
                parts.Add(sphere);
            }

            foreach (LookPart stalk in stalks)
            {
                LookPart thin = Radial(stalk, feetRadius);
                thin.size = new Vector3(0.06f, 1f, 0.06f);
                parts.Add(thin);
            }

            entry.parts = parts.ToArray();
            Set(entry, EffectMotion.Grow, EffectSocket.Feet, EffectCount.Stacks, 4, 1f);
            return entry;
        }

        static ElementEntry Drips(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Fx_PoisonDrips", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            for (int i = 0; i < entry.parts.Length; i++)
            {
                LookPart drop = Radial(entry.parts[i], dripRadius);
                drop.position.y = 0f;
                drop.size *= 0.8f;
                entry.parts[i] = drop;
            }

            Set(entry, EffectMotion.Fall, EffectSocket.UnderHead, EffectCount.Stacks, 2, 1f);
            return entry;
        }

        // Three tori around the body, each a little wider and tilted 10, 15 and 20 degrees
        static ElementEntry Orbit(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Status_Buff", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            float diameter = meshes.GetMesh(Primitive.Torus).bounds.size.x;
            for (int i = 0; i < entry.parts.Length; i++)
            {
                LookPart torus = entry.parts[i];
                float radius = orbitRadius + 0.1f * i;
                float scale = 2f * radius / diameter;
                torus.position = Vector3.zero;
                torus.size = new Vector3(scale, scale * 0.6f, scale);
                torus.euler = (Quaternion.Euler(0f, 120f * i, 0f) * Quaternion.Euler(10f + 5f * i, 0f, 0f)).eulerAngles;
                entry.parts[i] = torus;
            }

            Set(entry, EffectMotion.Orbit, EffectSocket.Body, EffectCount.Stacks, 2, 4f);
            return entry;
        }

        static ElementEntry Plates(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Status_Shield", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            for (int i = 0; i < entry.parts.Length; i++)
            {
                LookPart plate = Fit(entry.parts[i], plateRadius);
                plate.size.y *= 0.65f;
                plate.position.y = 0f;
                entry.parts[i] = plate;
            }

            Set(entry, EffectMotion.Close, EffectSocket.Body, EffectCount.Charges, 1, 0.25f);
            return entry;
        }

        // The shield plates closed toward the neck
        static ElementEntry Bud(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Status_Shield", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            for (int i = 0; i < entry.parts.Length; i++)
            {
                LookPart plate = Fit(entry.parts[i], budRadius);
                plate.size.y *= 0.85f;
                plate.position.y = -0.1f;
                Vector3 radial = new Vector3(plate.position.x, 0f, plate.position.z).normalized;
                Quaternion lean = Quaternion.AngleAxis(-18f, Vector3.Cross(Vector3.up, radial));
                plate.euler = (lean * Quaternion.Euler(plate.euler)).eulerAngles;
                entry.parts[i] = plate;
            }

            Set(entry, EffectMotion.Close, EffectSocket.Body, EffectCount.Fixed, 1, 0.4f);
            return entry;
        }

        // Four or five cones pointing down at the head, drawn from above it
        static ElementEntry Press(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Status_Buff", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            List<LookPart> cones = new List<LookPart>();
            for (int i = 0; i < 4; i++)
            {
                float angle = (90f * i + 45f) * Mathf.Deg2Rad;
                cones.Add(Part("Press", Primitive.Cone, new Vector3(Mathf.Cos(angle) * 0.42f, 0.3f, Mathf.Sin(angle) * 0.42f),
                               new Vector3(180f, 0f, 0f), new Vector3(0.26f, 0.6f, 0.26f)));
            }

            cones.Add(Part("Press", Primitive.Cone, new Vector3(0f, 0.38f, 0f), new Vector3(180f, 0f, 0f), new Vector3(0.32f, 0.76f, 0.32f)));
            entry.parts = cones.ToArray();
            Set(entry, EffectMotion.Press, EffectSocket.AboveHead, EffectCount.Stacks, 4, 1.2f);
            return entry;
        }

        // Five plates around the body, each split in two shards
        static ElementEntry Crack(PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read("Status_Shield", bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            List<LookPart> shards = new List<LookPart>();
            for (int i = 0; i < 5; i++)
            {
                float angle = (72f * i + 18f) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 position = radial * plateRadius + tangent * (0.14f * side) + Vector3.up * (0.08f * side - 0.45f);
                    Quaternion rotation = Quaternion.LookRotation(radial) * Quaternion.Euler(0f, 0f, 12f * side);
                    shards.Add(Part("Crack", Primitive.Pyramid, position, rotation.eulerAngles, new Vector3(0.26f, 0.9f, 0.1f)));
                }
            }

            entry.parts = shards.ToArray();
            Set(entry, EffectMotion.Shed, EffectSocket.Body, EffectCount.Fixed, 1, 1.6f);
            return entry;
        }

        static ElementEntry Mana(string prefab, EffectMotion motion, PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read(prefab, bodyPerPrefab, meshes);
            if (entry == null)
            {
                return null;
            }

            Set(entry, motion, EffectSocket.Body, EffectCount.Fixed, 1, 0.6f);
            return entry;
        }

        static ElementEntry Litter(PrimitiveMeshes meshes)
        {
            ElementEntry entry = World("Fx_HostileLitter", EffectMotion.Grow, EffectSocket.Ground, 0.8f, meshes);
            if (entry == null)
            {
                return null;
            }

            for (int i = 0; i < entry.parts.Length; i++)
            {
                entry.parts[i].colour = ColourRole.BaneAccent;
            }
            return entry;
        }

        static ElementEntry World(string prefab, EffectMotion motion, EffectSocket socket, float cycleSeconds, PrimitiveMeshes meshes)
        {
            ElementEntry entry = Read(prefab, 1f, meshes);
            if (entry == null)
            {
                return null;
            }

            Set(entry, motion, socket, EffectCount.Fixed, 1, cycleSeconds);
            return entry;
        }

        static void Set(ElementEntry entry, EffectMotion motion, EffectSocket socket, EffectCount count, int minCount,
                        float cycleSeconds)
        {
            entry.motion = motion;
            entry.socket = socket;
            entry.count = count;
            entry.minCount = minCount;
            entry.cycleSeconds = cycleSeconds;
            Dress(entry);
        }

        // Beads and rim move under the element, clear of the head, for every socket on a unit
        static void Dress(ElementEntry entry)
        {
            if (entry.socket == EffectSocket.Link || entry.socket == EffectSocket.Ground)
            {
                return;
            }

            float beadHeight = -0.9f;
            float beadDepth = -1.3f;
            float rimHeight = -0.95f;
            float rimRadius = 1.3f;
            if (entry.socket == EffectSocket.Feet)
            {
                beadHeight = 0.15f;
                beadDepth = -1.8f;
                rimHeight = 0.05f;
                rimRadius = 1.8f;
            }
            else if (entry.socket == EffectSocket.AboveHead)
            {
                beadHeight = 1.2f;
                beadDepth = 0f;
                rimHeight = 0.9f;
                rimRadius = 0.6f;
            }
            else if (entry.socket == EffectSocket.UnderHead)
            {
                rimHeight = 0.1f;
                rimRadius = 0.45f;
            }

            for (int i = 0; i < entry.stackBeads.Length; i++)
            {
                entry.stackBeads[i].position = new Vector3(entry.stackBeads[i].position.x, beadHeight, beadDepth);
            }

            for (int i = 0; i < entry.sideRim.Length; i++)
            {
                entry.sideRim[i].position = new Vector3(0f, rimHeight, 0f);
                entry.sideRim[i].euler = Vector3.zero;
                entry.sideRim[i].size = new Vector3(2f * rimRadius, 0.5f, 2f * rimRadius);
            }
        }

        static ElementEntry Read(string prefab, float scale, PrimitiveMeshes meshes)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFolder + prefab + ".prefab");
            if (root == null)
            {
                Debug.LogError($"[EffectVocabularyBaker] No prefab at {prefabFolder}{prefab}.prefab");
                return null;
            }

            List<LookPart> parts = new List<LookPart>();
            List<LookPart> beads = new List<LookPart>();
            List<LookPart> rings = new List<LookPart>();
            List<LookPart> rims = new List<LookPart>();
            foreach (Transform child in root.transform)
            {
                MeshFilter filter = child.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                if (child.name == "StackBead")
                {
                    beads.AddRange(ToParts(child, filter.sharedMesh, scale, PartRole.Body, ColourRole.BoonAccent, meshes));
                }
                else if (child.name == "CriticalRing")
                {
                    rings.AddRange(ToParts(child, filter.sharedMesh, scale, PartRole.Body, ColourRole.Accent, meshes));
                }
                else if (child.name == "SideRim")
                {
                    rims.AddRange(ToParts(child, filter.sharedMesh, scale, PartRole.Body, ColourRole.Body, meshes));
                }
                else
                {
                    // A cylinder under a shape is its stalk or, on a beam, a segment between two beads
                    bool isStalk = Nearest(filter.sharedMesh, meshes) == Primitive.CylinderSegment;
                    parts.AddRange(ToParts(child, filter.sharedMesh, scale, isStalk ? PartRole.Stem : PartRole.Body,
                                           isStalk ? ColourRole.Stem : ColourRole.Accent, meshes));
                }
            }

            ElementEntry entry = new ElementEntry();
            entry.parts = parts.ToArray();
            entry.stackBeads = ByX(beads).ToArray();
            entry.criticalRings = rings.ToArray();
            entry.sideRim = rims.ToArray();
            return entry;
        }

        // Keeps the part's extent when its mesh becomes a baked primitive, the star becomes two crossed flat pyramids
        static List<LookPart> ToParts(Transform child, Mesh source, float scale, PartRole role, ColourRole colour,
                                      PrimitiveMeshes meshes)
        {
            List<LookPart> parts = new List<LookPart>();
            if (source == meshes.star)
            {
                Vector3 extent = Vector3.Scale(source.bounds.size, child.localScale);
                Vector3 size = new Vector3(extent.x * 0.8f, extent.z, extent.y * 0.8f);
                parts.Add(Mapped(child, source, Primitive.Pyramid, Quaternion.Euler(90f, 0f, 0f), size, scale, role, colour, meshes));
                parts.Add(Mapped(child, source, Primitive.Pyramid, Quaternion.Euler(0f, 0f, 45f) * Quaternion.Euler(90f, 0f, 0f), size,
                                 scale, role, colour, meshes));
                return parts;
            }

            Primitive primitive = Nearest(source, meshes);
            Vector3 targetSize = meshes.GetMesh(primitive).bounds.size;
            Vector3 ratio = new Vector3(Ratio(source.bounds.size.x, targetSize.x), Ratio(source.bounds.size.y, targetSize.y),
                                        Ratio(source.bounds.size.z, targetSize.z));
            parts.Add(Mapped(child, source, primitive, Quaternion.identity, Vector3.Scale(child.localScale, ratio), scale, role,
                             colour, meshes));
            return parts;
        }

        static LookPart Mapped(Transform child, Mesh source, Primitive primitive, Quaternion turn, Vector3 size, float scale,
                               PartRole role, ColourRole colour, PrimitiveMeshes meshes)
        {
            Mesh target = meshes.GetMesh(primitive);
            Quaternion rotation = child.localRotation * turn;
            Vector3 centre = child.localRotation * Vector3.Scale(source.bounds.center, child.localScale)
                             - rotation * Vector3.Scale(target.bounds.center, size);
            LookPart part = Part(child.name, primitive, (child.localPosition + centre) * scale, rotation.eulerAngles, size * scale);
            part.role = role;
            part.colour = colour;
            return part;
        }

        static LookPart Part(string id, Primitive primitive, Vector3 position, Vector3 euler, Vector3 size)
        {
            LookPart part = new LookPart();
            part.id = id;
            part.primitive = primitive;
            part.role = PartRole.Body;
            part.colour = ColourRole.Accent;
            part.position = position;
            part.euler = euler;
            part.size = size;
            return part;
        }

        // Unity's built in sphere and cylinder, and the thin torus, have no primitive of their own
        static Primitive Nearest(Mesh mesh, PrimitiveMeshes meshes)
        {
            if (mesh == meshes.sphere || mesh.name == "Sphere")
            {
                return Primitive.Sphere;
            }

            if (mesh == meshes.cylinder || mesh.name == "Cylinder")
            {
                return Primitive.CylinderSegment;
            }

            if (mesh == meshes.cone)
            {
                return Primitive.Cone;
            }

            if (mesh == meshes.torus || mesh == meshes.thinTorus)
            {
                return Primitive.Torus;
            }

            if (mesh == meshes.boulder)
            {
                return Primitive.Boulder;
            }

            if (mesh == meshes.pyramid || mesh == meshes.star)
            {
                return Primitive.Pyramid;
            }

            if (mesh == meshes.leaf)
            {
                return Primitive.Leaf;
            }

            if (mesh == meshes.capsule || mesh.name == "Capsule")
            {
                return Primitive.Capsule;
            }

            Debug.LogError($"[EffectVocabularyBaker] No primitive near {mesh.name}, drawn as a sphere.");
            return Primitive.Sphere;
        }

        static float Ratio(float source, float target)
        {
            if (target < 0.0001f)
            {
                return 1f;
            }
            return source / target;
        }

        static List<LookPart> Shapes(ElementEntry entry, PartRole role)
        {
            List<LookPart> shapes = new List<LookPart>();
            foreach (LookPart part in entry.parts)
            {
                if (part.role == role)
                {
                    shapes.Add(part);
                }
            }
            return shapes;
        }

        // Moves a part onto a circle around the socket, keeping its height and direction
        static LookPart Radial(LookPart part, float radius)
        {
            Vector3 flat = new Vector3(part.position.x, 0f, part.position.z);
            if (flat.sqrMagnitude > 0.000001f)
            {
                flat = flat.normalized * radius;
            }

            part.position = new Vector3(flat.x, part.position.y, flat.z);
            return part;
        }

        // Scales a part so its distance from the socket axis becomes the radius
        static LookPart Fit(LookPart part, float radius)
        {
            float distance = new Vector2(part.position.x, part.position.z).magnitude;
            float scale = distance > 0.0001f ? radius / distance : 1f;
            part.position *= scale;
            part.size *= scale;
            return part;
        }

        static List<LookPart> ByAngle(List<LookPart> parts)
        {
            parts.Sort((a, b) => Mathf.Atan2(a.position.z, a.position.x).CompareTo(Mathf.Atan2(b.position.z, b.position.x)));
            return parts;
        }

        static List<LookPart> ByX(List<LookPart> parts)
        {
            parts.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            return parts;
        }
    }
}

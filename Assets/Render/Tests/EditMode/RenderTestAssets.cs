using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render
{

// The shipped assets and the fixtures more than one test class builds on
public static class RenderTestAssets
{
    public static readonly string LookVocabularyPath = "Assets/Render/Creatures/Data/LookVocabulary.asset";
    public static readonly string DeliveryVocabularyPath = "Assets/Render/Deliveries/Data/DeliveryVocabulary.asset";
    public static readonly string EffectVocabularyPath = "Assets/Render/Spells/Data/EffectVocabulary.asset";
    public static readonly string PalettePath = "Assets/Render/Grammar/Data/LookPalette.asset";
    public static readonly string MeshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
    public static readonly string LookMaterialPath = "Assets/Render/Look/Look_Default.mat";
    public static readonly string StoneEffectsPath = "Assets/Render/Stones/Prefabs/StoneEffects.prefab";

    static readonly string entities = "Assets/Data/Entities/";
    static readonly string projectiles = "Assets/Prefabs/Projectiles/";
    static readonly string items = "Assets/Data/EntityItems/";

    public static LookVocabulary LoadLookVocabulary()
    {
        return AssetDatabase.LoadAssetAtPath<LookVocabulary>(LookVocabularyPath);
    }

    public static DeliveryVocabulary LoadDeliveryVocabulary()
    {
        return AssetDatabase.LoadAssetAtPath<DeliveryVocabulary>(DeliveryVocabularyPath);
    }

    public static EffectVocabulary LoadEffectVocabulary()
    {
        return AssetDatabase.LoadAssetAtPath<EffectVocabulary>(EffectVocabularyPath);
    }

    public static LookPalette LoadPalette()
    {
        return AssetDatabase.LoadAssetAtPath<LookPalette>(PalettePath);
    }

    public static PrimitiveMeshes LoadMeshes()
    {
        return AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(MeshesPath);
    }

    // The look material every rig fixture draws with
    public static Material LoadLookMaterial()
    {
        return AssetDatabase.LoadAssetAtPath<Material>(LookMaterialPath);
    }

    // The game's entity data, by its folder under Assets/Data/Entities
    public static EntityData LoadEntity(string folder)
    {
        string file = folder == "HitArmorBufferEntityEntity" ? "HitArmorBufferEntity" : folder;
        EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(entities + folder + "/" + file + ".asset");
        Assert.NotNull(data, folder);
        return data;
    }

    public static GameObject LoadProjectile(string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectiles + name + ".prefab");
        Assert.NotNull(prefab, name);
        return prefab;
    }

    // A buff handler of the game's items, by its path under Assets/Data/EntityItems
    public static ABuffHandlerFactory LoadHandler(string path)
    {
        ABuffHandlerFactory handler = AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(items + path + ".asset");
        Assert.NotNull(handler, path);
        return handler;
    }

    // One body part and one arm, the smallest recipe that validates
    public static CreatureRecipe CreateRecipe()
    {
        CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
        recipe.parts = new CreaturePart[]
        {
            new CreaturePart { id = "Body", parent = -1, dimensions = Vector3.one, colour = Color.green }
        };
        recipe.sourceLocal = new Vector3[] { Vector3.up };
        recipe.arms = new ArmDefinition[]
        {
            new ArmDefinition
            {
                bodyPart = 0,
                segmentCount = 24,
                segmentLength = 0.2f,
                radius = 0.018f,
                restJoints = CreateRestPose(),
                bendPole = Vector3.up,
                colour = Color.green
            }
        };
        return recipe;
    }

    // A curling chain of n links 0.2 long
    public static Vector3[] CreateRestPose(int n = 24)
    {
        Vector3[] joints = new Vector3[n + 1];
        for (int i = 0; i < n; i++)
        {
            joints[i + 1] = joints[i] + new Vector3(Mathf.Cos(i * 0.4f), Mathf.Sin(i * 0.4f), 0f) * 0.2f;
        }

        return joints;
    }

    public static UnitChannels CreateChannels(LookSide side, HeadKind head, CountBand count = CountBand.One,
        StemBand stem = StemBand.Steady, MassBand mass = MassBand.Light, AccessoryKind accessory = AccessoryKind.None)
    {
        return new UnitChannels
        {
            side = side,
            head = head,
            count = count,
            stem = stem,
            mass = mass,
            reach = ReachBand.Long,
            accessory = accessory,
            accessoryHead = HeadKind.Arch,
            accent = EffectFamily.Damage
        };
    }

    public static CreatureRig CreateRig(CreatureRecipe recipe, Transform parent, Material material)
    {
        CreatureRig rig = new CreatureRig();
        rig.Init(recipe, parent, material, LoadMeshes());
        return rig;
    }

    // A stone the way the composer lays one out: a body on two limbs, a head on top
    public static CreatureRecipe CreateStoneRecipe()
    {
        CreatureRecipe recipe = ScriptableObject.CreateInstance<CreatureRecipe>();
        recipe.parts = new CreaturePart[]
        {
            StonePart("Body", -1, Vector3.up, Vector3.one, PartRole.Body),
            StonePart("LimbLeft", 0, new Vector3(-0.4f, -0.7f, 0f), Vector3.one * 0.4f, PartRole.Limb),
            StonePart("LimbRight", 0, new Vector3(0.4f, -0.7f, 0f), Vector3.one * 0.4f, PartRole.Limb),
            StonePart("Head", 0, Vector3.up * 0.8f, Vector3.one * 0.5f, PartRole.Head)
        };
        recipe.roots.count = 0;
        recipe.sourceLocal = new Vector3[] { Vector3.up * 2f };
        return recipe;
    }

    static CreaturePart StonePart(string id, int parent, Vector3 position, Vector3 dimensions, PartRole role)
    {
        return new CreaturePart
        {
            id = id,
            parent = parent,
            primitive = Primitive.Stone,
            localPosition = position,
            dimensions = dimensions,
            colour = Color.grey,
            role = role
        };
    }

    // A derived stone the way the prefab lays it out: the builder builds the rig, then the body joins it
    public static StoneBody CreateStoneBody(GameObject owner, Entity entity, CreatureRecipe recipe, Material material)
    {
        GameObject viewGo = new GameObject("DerivedStone");
        viewGo.transform.SetParent(owner.transform, false);
        CreatureBuilder builder = viewGo.AddComponent<CreatureBuilder>();
        builder.SetRecipe(recipe, material, LoadMeshes());
        builder.Init(entity);
        StoneBody body = viewGo.AddComponent<StoneBody>();
        TestHelpers.SetPrivateField(body, "_palette", LoadPalette());
        return body;
    }

    public static Entity CreateStoneEntity(GameObject owner, ResourceAttribute health)
    {
        Entity entity = null;
        TestHelpers.WithLoggingDisabled(() => entity = owner.AddComponent<Entity>());
        TestHelpers.SetPrivateField(entity, "_health", health);
        return entity;
    }

    public static StoneEffects CreateStoneEffects()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoneEffectsPath);
        return Object.Instantiate(prefab).GetComponent<StoneEffects>();
    }

    public static StoneGroundDisc CreateGroundDisc(Transform parent, bool isShadow)
    {
        GameObject discGo = new GameObject("Disc", typeof(MeshFilter), typeof(MeshRenderer));
        discGo.transform.SetParent(parent, false);
        StoneGroundDisc disc = discGo.AddComponent<StoneGroundDisc>();
        TestHelpers.SetPrivateField(disc, "_renderer", discGo.GetComponent<MeshRenderer>());
        TestHelpers.SetPrivateField(disc, "_isShadow", isShadow);
        return disc;
    }

    // Welds corners by position, skips zero-area triangles, then needs every edge used once in each direction
    // and a positive enclosed volume: a closed solid with its faces turned outward.
    public static void AssertClosed(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Dictionary<Vector3Int, int> welded = new Dictionary<Vector3Int, int>();
        int[] ids = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3Int key = Vector3Int.RoundToInt(vertices[i] * 100000f);
            if (!welded.TryGetValue(key, out ids[i]))
            {
                ids[i] = welded.Count;
                welded.Add(key, ids[i]);
            }
        }

        Dictionary<long, int> edges = new Dictionary<long, int>();
        float volume = 0f;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            int[] corners = { ids[triangles[i]], ids[triangles[i + 1]], ids[triangles[i + 2]] };
            bool isDegenerate = corners[0] == corners[1] || corners[1] == corners[2] || corners[2] == corners[0];
            if (isDegenerate || Vector3.Cross(b - a, c - a).sqrMagnitude < 0.00000000000001f)
            {
                continue;
            }

            volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
            for (int j = 0; j < 3; j++)
            {
                long edge = (long)corners[j] * welded.Count + corners[(j + 1) % 3];
                edges.TryGetValue(edge, out int count);
                edges[edge] = count + 1;
            }
        }

        foreach (KeyValuePair<long, int> edge in edges)
        {
            long from = edge.Key / welded.Count;
            long to = edge.Key % welded.Count;
            edges.TryGetValue(to * welded.Count + from, out int reverse);
            Assert.AreEqual(1, edge.Value, mesh.name + " has an edge used twice in one direction");
            Assert.AreEqual(1, reverse, mesh.name + " has an open or one-sided edge");
        }

        Assert.Greater(volume, 0f, mesh.name + " encloses no volume or faces inward");
    }
}

}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

// A sink wired like the shipped prefab and initialised with what the RenderManager would hand it
public static class SpellSinkFixture
{
    public static readonly string SinkPath = "Assets/Render/Spells/Prefabs/SpellVisualSink.prefab";
    public static readonly string MeshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
    public static readonly string LooksPath = "Assets/Render/Spells/Data/SpellLooks.asset";

    public static SpellVisualSink Add(GameObject host)
    {
        SpellVisualSink sink = host.AddComponent<SpellVisualSink>();
        TestHelpers.SetPrivateField(sink, "_vocabulary", RenderTestAssets.LoadEffectVocabulary());
        TestHelpers.SetPrivateField(sink, "_material", RenderTestAssets.LoadLookMaterial());
        Init(sink);
        return sink;
    }

    public static void Init(SpellVisualSink sink)
    {
        sink.Init(AssetDatabase.LoadAssetAtPath<SpellLooks>(LooksPath), RenderTestAssets.LoadMeshes(), null, null);
    }

    public static BuffHandlerFactory Modifier(AttributeType type, float value, List<Object> created)
    {
        FlatModifierFactory modifier = ScriptableObject.CreateInstance<FlatModifierFactory>();
        modifier.data = new FlatModifierData { type = type, modifierType = AttributeModifierType.Add, value = value };
        return Handler(modifier, false, 0f, created);
    }

    public static BuffHandlerFactory Consumer(float value, float period, List<Object> created)
    {
        ConsumerFactory consumer = ScriptableObject.CreateInstance<ConsumerFactory>();
        FlatValue flat = new FlatValue();
        flat.data = new FlatValueData { value = value };
        consumer.data = new ConsumerData { value = flat };
        created.Add(consumer);
        ApplyConsumerBuffFactory buff = ScriptableObject.CreateInstance<ApplyConsumerBuffFactory>();
        buff.data = new ApplyConsumerBuffData { consumerFactory = consumer };
        return Handler(buff, period > 0f, period, created);
    }

    public static BuffHandlerFactory Invincible(List<Object> created)
    {
        return Handler(ScriptableObject.CreateInstance<InvincibilityBuffFactory>(), false, 0f, created);
    }

    static BuffHandlerFactory Handler(ABuffFactory buff, bool isPeriodic, float period, List<Object> created)
    {
        created.Add(buff);
        BuffHandlerFactory handler = ScriptableObject.CreateInstance<BuffHandlerFactory>();
        handler.data = new BuffHandlerData
        {
            durationType = DurationType.Duration,
            duration = 6f,
            isPeriodic = isPeriodic,
            periodDuration = period,
            buffFactoryList = new List<ABuffFactory> { buff }
        };
        created.Add(handler);
        return handler;
    }
}

}

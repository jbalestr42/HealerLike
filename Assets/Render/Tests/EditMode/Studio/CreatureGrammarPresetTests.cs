using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Studio
{

public class CreatureGrammarPresetTests
{
    static readonly string soldierPath = "Assets/Data/Entities/SoldierEntity/SoldierEntity.asset";

    readonly List<Object> _objects = new List<Object>();
    readonly List<string> _paths = new List<string>();
    CreatureGrammarPreset _preset;

    [SetUp]
    public void SetUp()
    {
        _preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        _preset.vocabulary = RenderTestAssets.LoadLookVocabulary();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (string path in _paths)
        {
            AssetDatabase.DeleteAsset(path);
        }

        foreach (Object trackedObject in _objects)
        {
            if (trackedObject != null && !EditorUtility.IsPersistent(trackedObject))
            {
                Object.DestroyImmediate(trackedObject);
            }
        }
        _paths.Clear();
        _objects.Clear();
    }

    T Track<T>(T instance) where T : Object
    {
        _objects.Add(instance);
        return instance;
    }

    // A soldier's primary skill under attributes the test owns
    EntityData CreateEntity(float health)
    {
        EntityData entity = Track(ScriptableObject.CreateInstance<EntityData>());
        EntityData soldier = AssetDatabase.LoadAssetAtPath<EntityData>(soldierPath);
        entity.skillFactories = new List<ASkillFactory>(soldier.skillFactories);
        entity.attributes[AttributeType.HealthMax] = health;
        entity.attributes[AttributeType.Range] = 2f;
        return entity;
    }

    [TestCase(LookSide.Plant, HeadKind.Bud, CountBand.One, MassBand.Light)]
    [TestCase(LookSide.Plant, HeadKind.Arch, CountBand.Many, MassBand.Sturdy)]
    [TestCase(LookSide.Stone, HeadKind.Ward, CountBand.Few, MassBand.Heavy)]
    public void Compose_ManualChannels_MatchesTheLookComposer(LookSide side, HeadKind head, CountBand count,
        MassBand mass)
    {
        _preset.side = side;
        _preset.head = head;
        _preset.count = count;
        _preset.mass = mass;

        CreatureRecipe actual = Track(_preset.Compose());
        CreatureRecipe expected = Track(LookComposer.Compose(_preset.Channels(), _preset.vocabulary));

        CollectionAssert.AreEqual(expected.parts, actual.parts);
        CollectionAssert.AreEqual(expected.sourceLocal, actual.sourceLocal);
        Assert.AreEqual(expected.roots, actual.roots);
        Assert.AreEqual(expected.idle, actual.idle);
        Assert.AreEqual(expected.neckLocal, actual.neckLocal);
        Assert.AreEqual(expected.wiltColour, actual.wiltColour);
        Assert.AreEqual(expected.arms.Length, actual.arms.Length);
        for (int i = 0; i < expected.arms.Length; i++)
        {
            CollectionAssert.AreEqual(expected.arms[i].restJoints, actual.arms[i].restJoints);
        }
    }

    [Test]
    public void Compose_StoneWilt_ComesFromThePalette()
    {
        LookVocabulary custom = Track(Object.Instantiate(_preset.vocabulary));
        custom.palette = Track(Object.Instantiate(_preset.vocabulary.palette));
        custom.palette.stoneWilt = new Color(0.2f, 0.4f, 0.6f, 1f);
        _preset.vocabulary = custom;
        _preset.side = LookSide.Stone;

        CreatureRecipe result = Track(_preset.Compose());

        Assert.AreEqual(custom.palette.Colour(ColourRole.Wilt, _preset.accent, LookSide.Stone), result.wiltColour);
    }

    [Test]
    public void Compose_TwoBakes_OwnTheirPartsAndJoints()
    {
        LookPart original = _preset.vocabulary.heads[_preset.head].plant[0];

        CreatureRecipe first = Track(_preset.Compose());
        CreatureRecipe second = Track(_preset.Compose());
        Vector3 before = second.parts[0].dimensions;
        first.parts[0].dimensions = Vector3.one * 99f;

        Assert.AreEqual(before, second.parts[0].dimensions);
        Assert.AreEqual(original, _preset.vocabulary.heads[_preset.head].plant[0]);
        Assert.AreNotSame(first.arms, second.arms);
    }

    [Test]
    public void Compose_PinnedReach_IgnoresTheReachBand()
    {
        Assume.That(_preset.vocabulary.isReachPinned, "The shipped vocabulary pins the plant reach");
        _preset.reach = ReachBand.Short;
        CreatureRecipe shortReach = Track(_preset.Compose());

        _preset.reach = ReachBand.Long;
        CreatureRecipe longReach = Track(_preset.Compose());

        Assert.AreEqual(shortReach.roots.footRadius, longReach.roots.footRadius);
    }

    [Test]
    public void Channels_DerivingFromEntity_ReadsTheEntity()
    {
        EntityData entity = CreateEntity(300f);
        _preset.sourceEntity = entity;
        _preset.sourceSide = Entity.EntityType.Computer;
        _preset.deriveFromEntity = true;

        UnitChannels channels = _preset.Channels();

        Assert.AreEqual(LookDerivation.Channels(entity, Entity.EntityType.Computer), channels);
        Assert.AreEqual(MassBand.Heavy, channels.mass); // 300 health is past the heavy threshold
    }

    [Test]
    public void ReadFromEntity_DerivedChannels_DetachesFromTheEntity()
    {
        EntityData entity = CreateEntity(300f);
        _preset.sourceEntity = entity;
        _preset.sourceSide = Entity.EntityType.Computer;
        _preset.deriveFromEntity = true;
        UnitChannels expected = _preset.Channels();

        Assert.IsTrue(_preset.ReadFromEntity());
        entity.attributes[AttributeType.HealthMax] = 50f;

        Assert.IsFalse(_preset.deriveFromEntity);
        Assert.AreEqual(expected, _preset.Channels());
        Assert.AreSame(entity, _preset.sourceEntity);
    }

    [Test]
    public void ReadFromEntity_EntityWithoutPrimarySkill_ReturnsFalse()
    {
        _preset.sourceEntity = Track(ScriptableObject.CreateInstance<EntityData>());
        _preset.deriveFromEntity = true;

        Assert.IsFalse(_preset.ReadFromEntity());
        Assert.IsNull(_preset.Compose());
        Assert.IsTrue(_preset.deriveFromEntity);
    }

    [Test]
    public void TryChannels_DerivingWithoutEntity_GivesTheManualChannelsAndAnError()
    {
        _preset.deriveFromEntity = true;
        _preset.head = HeadKind.Spear;

        UnitChannels channels;
        string error;
        bool isDerived = _preset.TryChannels(out channels, out error);

        Assert.IsFalse(isDerived);
        Assert.AreEqual(HeadKind.Spear, channels.head);
        StringAssert.Contains("EntityData", error);
    }

    [Test]
    public void SavedAsset_Reloaded_KeepsEveryChannelAndReference()
    {
        string path = "Assets/__CreatureGrammarPreset_" + Guid.NewGuid().ToString("N") + ".asset";
        _paths.Add(path);
        CreatureGrammarPreset saved = Object.Instantiate(_preset);
        saved.displayName = "Persistent ward";
        saved.side = LookSide.Stone;
        saved.head = HeadKind.Ward;
        saved.count = CountBand.Many;
        saved.stem = StemBand.Slow;
        saved.accessory = AccessoryKind.MiniHead;
        saved.accessoryHead = HeadKind.Spear;
        saved.sourceSide = Entity.EntityType.Computer;
        saved.sourceEntity = AssetDatabase.LoadAssetAtPath<EntityData>(soldierPath);
        saved.deriveFromEntity = true;
        AssetDatabase.CreateAsset(saved, path);
        AssetDatabase.SaveAssetIfDirty(saved);
        Resources.UnloadAsset(saved);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        CreatureGrammarPreset loaded = AssetDatabase.LoadAssetAtPath<CreatureGrammarPreset>(path);

        Assert.AreEqual("Persistent ward", loaded.displayName);
        Assert.AreSame(_preset.vocabulary, loaded.vocabulary);
        Assert.AreEqual(LookDerivation.Channels(loaded.sourceEntity, Entity.EntityType.Computer), loaded.Channels());
        loaded.deriveFromEntity = false;
        Assert.AreEqual(HeadKind.Ward, loaded.Channels().head);
        Assert.AreEqual(AccessoryKind.MiniHead, loaded.Channels().accessory);
        Assert.AreEqual(HeadKind.Spear, loaded.Channels().accessoryHead);
    }
}

}

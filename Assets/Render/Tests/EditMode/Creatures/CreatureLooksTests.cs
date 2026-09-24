using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class CreatureLooksTests
{
    readonly List<Object> _objects = new List<Object>();
    readonly List<string> _bakedPaths = new List<string>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            Object.DestroyImmediate(trackedObject);
        }
        _objects.Clear();
        foreach (string path in _bakedPaths)
        {
            AssetDatabase.DeleteAsset(path);
        }
        _bakedPaths.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    GameObject CreateView(string name)
    {
        GameObject view = new GameObject(name);
        _objects.Add(view);
        return view;
    }

    CreatureLooks CreateLooks()
    {
        CreatureLooks looks = CreateTracked<CreatureLooks>();
        looks.plant = CreateView("Plant");
        looks.stone = CreateView("Stone");
        looks.vocabulary = RenderTestAssets.LoadLookVocabulary();
        return looks;
    }

    [Test]
    public void GetView_MappedEntity_ReturnsMappedView()
    {
        CreatureLooks looks = CreateLooks();
        EntityData data = CreateTracked<EntityData>();
        GameObject view = CreateView("Mapped");
        looks.entities[data] = view;

        GameObject result = looks.GetView(data, Entity.EntityType.Computer);

        Assert.AreSame(view, result);
    }

    [Test]
    public void GetView_UnmappedPlayerEntity_ReturnsThePlantHost()
    {
        CreatureLooks looks = CreateLooks();

        GameObject result = looks.GetView(CreateTracked<EntityData>(), Entity.EntityType.Player);

        Assert.AreSame(looks.plant, result);
    }

    [Test]
    public void GetView_UnmappedComputerEntity_ReturnsTheStoneHost()
    {
        CreatureLooks looks = CreateLooks();

        GameObject result = looks.GetView(CreateTracked<EntityData>(), Entity.EntityType.Computer);

        Assert.AreSame(looks.stone, result);
    }

    [Test]
    public void GetView_NullData_ReturnsSideDefault()
    {
        CreatureLooks looks = CreateLooks();

        GameObject result = looks.GetView(null, Entity.EntityType.Player);

        Assert.AreSame(looks.plant, result);
    }

    [Test]
    public void GetRecipe_MappedEntity_ReturnsNullSoTheAuthoredViewKeepsItsOwn()
    {
        CreatureLooks looks = CreateLooks();
        EntityData data = RenderTestAssets.LoadEntity("NormalEntity");
        looks.entities[data] = CreateView("Mapped");

        Assert.IsNull(looks.GetRecipe(data, Entity.EntityType.Player));
    }

    [Test]
    public void GetRecipe_UnmappedEntity_DerivesOncePerSide()
    {
        CreatureLooks looks = CreateLooks();
        EntityData data = RenderTestAssets.LoadEntity("SoldierEntity");

        CreatureRecipe plant = looks.GetRecipe(data, Entity.EntityType.Player);
        CreatureRecipe stone = looks.GetRecipe(data, Entity.EntityType.Computer);
        _objects.Add(plant);
        _objects.Add(stone);

        Assert.NotNull(plant);
        Assert.NotNull(stone);
        Assert.AreNotSame(plant, stone);
        Assert.AreSame(plant, looks.GetRecipe(data, Entity.EntityType.Player));
        Assert.AreEqual(Primitive.Stone, stone.parts[0].primitive);
        Assert.AreEqual(Primitive.Sphere, plant.parts[0].primitive);
    }

    [Test]
    public void BakeToOverride_DerivedEntity_SavesItsRecipeAndAddsItsRow()
    {
        CreatureLooks looks = CreateTracked<CreatureLooks>();
        looks.plant = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Creatures/Prefabs/DerivedPlant.prefab");
        looks.vocabulary = RenderTestAssets.LoadLookVocabulary();
        EntityData data = RenderTestAssets.LoadEntity("NormalEntity");

        GameObject view = looks.BakeToOverride(data, Entity.EntityType.Player);

        string prefabPath = AssetDatabase.GetAssetPath(view);
        CreatureRecipe recipe = view.GetComponent<CreatureBuilder>().recipe;
        string recipePath = AssetDatabase.GetAssetPath(recipe);
        _bakedPaths.Add(prefabPath);
        _bakedPaths.Add(recipePath);
        Assert.AreSame(view, looks.GetView(data, Entity.EntityType.Player));
        Assert.IsNull(looks.GetRecipe(data, Entity.EntityType.Player));
        StringAssert.StartsWith("Assets/Render/Creatures/Data/", recipePath);
        StringAssert.StartsWith("Assets/Render/Creatures/Prefabs/", prefabPath);
        Assert.IsTrue(CreatureValidator.TryValidate(recipe, out string error), error);
    }

    [Test]
    public void GetView_ShippedAsset_DerivesEveryEntityAndKeepsTheHealerAuthored()
    {
        CreatureLooks looks = AssetDatabase.LoadAssetAtPath<CreatureLooks>("Assets/Render/Creatures/Data/CreatureLooks.asset");
        CharacterData healer = AssetDatabase.LoadAssetAtPath<CharacterData>(
            "Assets/Data/Characters/BasicHealerCharacter/BasicHealerCharacter.asset");

        Assert.IsEmpty(looks.entities);
        Assert.IsEmpty(looks.characters);
        Assert.AreSame(RenderTestAssets.LoadLookVocabulary(), looks.vocabulary);
        Assert.AreEqual("DerivedPlant", looks.plant.name);
        Assert.AreEqual("DerivedStone", looks.stone.name);
        Assert.AreEqual("HealerCharacter", looks.GetView(healer).name);
        Assert.IsNull(looks.plant.GetComponent<CreatureBuilder>().recipe);
        Assert.IsNull(looks.stone.GetComponent<CreatureBuilder>().recipe);
    }

    // The prefab folder holds the views the asset names and nothing else, every other unit is derived
    [Test]
    public void Character_PrefabsFolder_HoldsOnlyThePlantHostAndTheCharacterView()
    {
        CreatureLooks looks = AssetDatabase.LoadAssetAtPath<CreatureLooks>("Assets/Render/Creatures/Data/CreatureLooks.asset");

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Render/Creatures/Prefabs" });

        string[] names = System.Array.ConvertAll(guids,
            guid => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
        CollectionAssert.AreEquivalent(new[] { looks.plant.name, looks.character.name }, names);
    }
}

}

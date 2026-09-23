using System;
using System.Collections;
using System.Reflection;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class CharacterViewTests
{
    GameObject _characterGo;
    GameObject _anchorGo;
    GameObject _targetGo;
    GameObject _managerGo;
    Material _material;
    CreatureRecipe _ownedRecipe;

    [SetUp]
    public void SetUp()
    {
        _characterGo = new GameObject("Character");
        _anchorGo = new GameObject("Anchor");
        _targetGo = new GameObject("HealTarget");
        _managerGo = new GameObject("RenderManager");
        _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader"));
    }

    [TearDown]
    public void TearDown()
    {
        ReleaseViews(_characterGo);
        ReleaseViews(_anchorGo);
        Object.DestroyImmediate(_anchorGo);
        Object.DestroyImmediate(_characterGo);
        Object.DestroyImmediate(_targetGo);
        Object.DestroyImmediate(_managerGo);
        Object.DestroyImmediate(_material);
        if (_ownedRecipe != null)
        {
            Object.DestroyImmediate(_ownedRecipe);
            _ownedRecipe = null;
        }
    }

    static void ReleaseViews(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (CharacterView view in root.GetComponentsInChildren<CharacterView>(true))
        {
            TestHelpers.InvokePrivate(view, "OnDestroy");
        }
    }

    // What Init takes from the view prefab and the manager, without a manager
    static void Bind(CharacterView view, Character character, CreatureRecipe recipe, Transform anchor,
        Material material, RenderRegistry registry)
    {
        TestHelpers.SetPrivateField(view, "_character", character);
        TestHelpers.SetPrivateField(view, "_recipe", recipe);
        TestHelpers.SetPrivateField(view, "_visualAnchor", anchor);
        TestHelpers.SetPrivateField(view, "_material", material);
        TestHelpers.SetPrivateField(view, "_registry", registry);
        TestHelpers.InvokePrivate(view, "BuildAndRegister");
        TestHelpers.InvokePrivate(view, "ObserveResources");
    }

    [Test]
    public void Bind_AnchorAndRegistryWithoutEntityInit_FollowsAnchorAndReactsToHeals()
    {
        _ownedRecipe = CreatureValidatorTests.Recipe();
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = _characterGo.AddComponent<Character>());
        _anchorGo.transform.position = new Vector3(5f, 1f, 2f);
        _targetGo.transform.position = _anchorGo.transform.position + Vector3.one;
        CharacterView view = _anchorGo.AddComponent<CharacterView>();
        TestHelpers.SetPrivateField(view, "_meshes", PrimitiveMeshesTests.Meshes());
        RenderRegistry registry = new RenderRegistry();

        Bind(view, character, _ownedRecipe, _anchorGo.transform, _material, registry);
        CreatureRig rig = view.rig;
        Bind(view, character, _ownedRecipe, _anchorGo.transform, _material, registry);
        Assert.AreSame(rig, view.rig);

        TestHelpers.InvokePrivate(view, "LateUpdate");
        Assert.AreEqual(_anchorGo.transform.position, rig.root.position);
        Assert.IsNull(_characterGo.GetComponent<Entity>());
        Assert.IsNull(character.mana);

        registry.NotifyHeal(_characterGo, _targetGo, 0f, false);
        Assert.AreEqual(0, rig.activeArmCount);
        registry.NotifyHeal(_characterGo, _targetGo, 4f, true);
        Assert.AreEqual(1, rig.activeArmCount);

        view.enabled = false;
        TestHelpers.InvokePrivate(view, "OnDisable");
        rig.Tick(1f, 0.3f, new FootFrame(_anchorGo.transform.position, Vector3.up, 1f));
        rig.Tick(1.3f, 0.3f, new FootFrame(_anchorGo.transform.position, Vector3.up, 1f));
        registry.NotifyHeal(_characterGo, _targetGo, 4f, true);
        Assert.AreEqual(0, rig.activeArmCount);

        view.enabled = true;
        TestHelpers.InvokePrivate(view, "OnEnable");
        registry.NotifyHeal(_characterGo, _targetGo, 4f, false);
        Assert.AreEqual(1, rig.activeArmCount);

        Object.DestroyImmediate(_characterGo);
        view.enabled = false;
        TestHelpers.InvokePrivate(view, "OnDisable");
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo field = typeof(RenderRegistry).GetField("_healSinks", flags);
        Assert.AreEqual(0, ((IDictionary)field.GetValue(registry)).Count);
    }

    [Test]
    public void Bind_RegisteredCharacter_CountsHealGesturesAndTintsBudsByManaWithoutAllocating()
    {
        CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/Healer.asset");
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = _characterGo.AddComponent<Character>());
        ResourceAttribute mana = TestHelpers.CreateResourceAttribute(_characterGo, AttributeType.ManaMax, 100);
        TestHelpers.SetPrivateField(character, "_mana", mana);
        ResourceAttribute health = TestHelpers.CreateResourceAttribute(_targetGo, AttributeType.HealthMax, 100);
        CharacterView view = _characterGo.AddComponent<CharacterView>();
        TestHelpers.SetPrivateField(view, "_meshes", PrimitiveMeshesTests.Meshes());
        RenderRegistry registry = new RenderRegistry();
        Bind(view, character, recipe, _characterGo.transform, _material, registry);
        ResourceOutcomeObserver observer = _targetGo.AddComponent<ResourceOutcomeObserver>();
        observer.Bind(health, null, null, registry);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo updateMethod = typeof(CharacterView).GetMethod("Update", flags);
        Action update = (Action)Delegate.CreateDelegate(typeof(Action), view, updateMethod);
        for (int i = 0; i < 10; i++)
        {
            update();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++)
        {
            update();
        }

        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.AreEqual(3, view.budAnchors.Count);
        Assert.NotNull(view.bud0);
        Assert.NotNull(view.bud1);
        Assert.NotNull(view.bud2);

        ResourceModifier modifier = new ResourceModifier { source = _characterGo };
        health.OnAllConsumerProcessed.Invoke(_targetGo, modifier, -5f, false);
        Assert.AreEqual(1, view.castGestureCount);
        for (int i = 0; i < 2; i++)
        {
            health.OnAllConsumerProcessed.Invoke(_targetGo, modifier, 5f, false);
        }

        Assert.AreEqual(3, view.castGestureCount);
        health.OnAllConsumerProcessed.Invoke(_targetGo, modifier, 7f, false);
        Assert.AreEqual(4, view.castGestureCount);
        mana.OnAllConsumerProcessed.Invoke(_characterGo, modifier, 10f, false);
        Assert.AreEqual(4, view.castGestureCount, "Mana restoration must not count as a heal gesture.");

        TestHelpers.SetPrivateField(mana, "_value", 0f);
        TestHelpers.InvokePrivate(view, "LateUpdate");
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Renderer renderer = view.bud0.GetComponentInChildren<Renderer>();
        renderer.GetPropertyBlock(block);
        Color empty = block.GetColor("_BaseColor");
        TestHelpers.SetPrivateField(mana, "_value", 100f);
        TestHelpers.InvokePrivate(view, "LateUpdate");
        renderer.GetPropertyBlock(block);
        Assert.Greater(block.GetColor("_BaseColor").g, empty.g);

        view.enabled = false;
        health.OnAllConsumerProcessed.Invoke(_targetGo, modifier, -5f, false);
        Assert.AreEqual(4, view.castGestureCount);
    }

    [Test]
    public void Init_ViewPrefab_AnchorsBodyOnItself()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Creatures/Prefabs/HealerCharacter.prefab");
        GameObject viewGo = Object.Instantiate(prefab, _characterGo.transform);
        Character character = null;
        TestHelpers.WithLoggingDisabled(() => character = _characterGo.AddComponent<Character>());
        RenderManager manager = _managerGo.AddComponent<RenderManager>();
        TestHelpers.SetPrivateField(manager, "_meshes", PrimitiveMeshesTests.Meshes());
        CharacterView view = viewGo.GetComponent<CharacterView>();

        view.Init(character, manager);

        Assert.NotNull(view.rig);
        Assert.AreSame(viewGo.transform, view.rig.root.parent);
    }
}

}

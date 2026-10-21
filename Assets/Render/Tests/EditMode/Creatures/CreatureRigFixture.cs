using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public abstract class CreatureRigFixture
{
    protected static readonly FootFrame ground = new FootFrame(Vector3.zero, Vector3.up, 1f);
    protected static readonly string healerPath = "Assets/Render/Creatures/Data/Healer.asset";
    protected GameObject _parent;
    protected Material _material;
    protected CreatureRecipe _recipe;
    protected CreatureRig _rig;

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("TestRig");
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.idle = default;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
    }

    [TearDown]
    public void TearDown()
    {
        _rig.Dispose();
        Object.DestroyImmediate(_parent);
        Object.DestroyImmediate(_material);
        Object.DestroyImmediate(_recipe);
    }

    protected void CreateDerivedRig(LookSide side)
    {
        _rig.Dispose();
        Object.DestroyImmediate(_recipe);
        _recipe = LookComposer.Compose(
            RenderTestAssets.CreateChannels(side, HeadKind.Bud),
            RenderTestAssets.LoadLookVocabulary()
        );
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
    }

    protected static Color BaseColour(Renderer renderer)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        return block.GetColor("_BaseColor");
    }

    protected Color TipColour(int tip)
    {
        return BaseColour(_rig.partTransforms[tip].GetComponent<Renderer>());
    }
}
}

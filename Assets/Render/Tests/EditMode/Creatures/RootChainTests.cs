using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{

public class RootChainTests
{
    CreatureRecipe _recipe;
    GameObject _root;
    GameObject _sway;
    Material _material;
    RootChain _roots;
    RootDefinition _definition;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Root");
        _sway = new GameObject("Sway");
        _sway.transform.SetParent(_root.transform, false);
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        _recipe = RenderTestAssets.CreateRecipe();
        _definition = _recipe.roots;
        _roots = new RootChain();
        _roots.Init(_definition, _root.transform, RenderTestAssets.LoadMeshes(), _material, Color.green);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_material);
        Object.DestroyImmediate(_recipe);
    }

    [Test]
    public void Place_Roots_AreJointedCylinderChainsDownToTheFoot()
    {
        _roots.Place(_sway.transform, _root.transform, 1f);

        int segments = 0;
        int joints = 0;
        float lowest = float.MaxValue;
        foreach (Transform child in _root.transform)
        {
            if (child.name == "Root")
            {
                segments++;
                Assert.AreSame(RenderTestAssets.LoadMeshes().cylinder, child.GetComponent<MeshFilter>().sharedMesh);
                lowest = Mathf.Min(lowest, child.GetComponent<Renderer>().bounds.min.y);
            }
            else if (child.name == "RootJoint")
            {
                joints++;
            }
        }

        Assert.AreEqual(_definition.count * _definition.segments, segments);
        Assert.AreEqual(_definition.count * (_definition.segments - 1), joints);
        Assert.That(lowest, Is.LessThan(_definition.thickness)); // the last segment lies on the ground
    }

    [Test]
    public void Place_LeaningSway_KeepsTheFeetWhereTheyStand()
    {
        _roots.Place(_sway.transform, _root.transform, 1f);
        Transform last = LastSegment();
        float footBefore = last.GetComponent<Renderer>().bounds.min.y;

        _sway.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        _roots.Place(_sway.transform, _root.transform, 1f);

        Assert.AreEqual(footBefore, last.GetComponent<Renderer>().bounds.min.y, 0.05f);
    }

    Transform LastSegment()
    {
        Transform last = null;
        foreach (Transform child in _root.transform)
        {
            if (child.name == "Root")
            {
                last = child;
            }
        }
        return last;
    }
}

}

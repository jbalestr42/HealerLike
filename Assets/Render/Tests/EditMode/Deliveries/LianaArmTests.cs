using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{

public class LianaArmTests
{
    CreatureRecipe _recipe;
    LianaArm _arm;
    GameObject _parent;
    Material _material;
    LianaArm _rendered;

    public static LianaArm CreateArm(ArmDefinition definition, Transform parent = null,
        Material material = null)
    {
        LianaArm arm = new LianaArm();
        arm.Init(definition, parent, material, RenderTestAssets.LoadMeshes(),
            RenderTestAssets.LoadDeliveryVocabulary());
        return arm;
    }

    [SetUp]
    public void SetUp()
    {
        _recipe = RenderTestAssets.CreateRecipe();
        _arm = CreateArm(_recipe.arms[0]);
        _arm.Tick(0f, Vector3.zero, Quaternion.identity);
        _parent = new GameObject("ArmFixture");
        _material = new Material(RenderTestAssets.LoadLookMaterial());
    }

    [TearDown]
    public void TearDown()
    {
        if (_rendered != null)
        {
            _rendered.Dispose();
            _rendered = null;
        }

        _arm.Dispose();
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_parent);
        Object.DestroyImmediate(_material);
    }

    // The shipped Healer arm, rendered under the fixture parent
    LianaArm CreateRenderedArm()
    {
        string healerPath = "Assets/Render/Creatures/Data/Healer.asset";
        CreatureRecipe authored = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(healerPath);
        _rendered = CreateArm(authored.arms[0], _parent.transform, _material);
        return _rendered;
    }

    // The tip hangs from the chain, its parts appear on the first drawn frame
    Transform TipRoot()
    {
        return _parent.transform.Find("LianaArm/DeliveryTip");
    }

    int TipParts()
    {
        Transform root = TipRoot();
        if (!root)
        {
            return 0;
        }
        return root.GetComponentsInChildren<Renderer>().Length;
    }

    [Test]
    public void Tick_Gesture_DrawsAClosedChain()
    {
        LianaArm rendered = CreateRenderedArm();
        rendered.Tick(0f, Vector3.zero, Quaternion.identity);

        rendered.Begin(1, GestureKind.Attack, Vector3.one);
        rendered.SetTipGoal(1, Vector3.one);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);

        RenderTestAssets.AssertClosed(_parent.GetComponentInChildren<MeshFilter>().sharedMesh);
    }

    [Test]
    public void Tick_Gesture_ShowsTheMeshAndItsLeaves()
    {
        LianaArm rendered = CreateRenderedArm();
        Renderer[] renderers = _parent.GetComponentsInChildren<Renderer>(true);
        Assert.AreEqual(1, renderers.Length);
        Assert.IsFalse(renderers[0].enabled);
        rendered.Tick(0f, Vector3.zero, Quaternion.identity);
        Assert.IsNull(TipRoot());

        rendered.Begin(1, GestureKind.Attack, Vector3.one);
        rendered.SetTipGoal(1, Vector3.one);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);

        Assert.IsTrue(renderers[0].enabled);
        Assert.That(Vector3.Distance(TipRoot().position, rendered.tip), Is.LessThan(0.00001f));
        for (int i = 0; i < LianaArm.LeafCount; i++)
        {
            Vector4 leaf = rendered.LeafMatrix(i).GetColumn(3);
            Assert.IsTrue(float.IsFinite(leaf.x) && float.IsFinite(leaf.y) && float.IsFinite(leaf.z));
        }
    }

    [Test]
    public void Tick_WarmGesture_AllocatesNothingAndReachesTheGoal()
    {
        LianaArm rendered = CreateRenderedArm();
        rendered.Begin(1, GestureKind.Attack, Vector3.one);
        rendered.SetTipGoal(1, Vector3.one);
        for (int i = 0; i < 11; i++)
        {
            rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++)
        {
            rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
        }

        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
        Mesh mesh = _parent.GetComponentInChildren<MeshFilter>().sharedMesh;
        Assert.Greater(mesh.vertexCount, 0);
        foreach (Vector3 vertex in mesh.vertices)
        {
            Assert.IsTrue(float.IsFinite(vertex.x) && float.IsFinite(vertex.y) && float.IsFinite(vertex.z));
        }
        Assert.That(Vector3.Distance(rendered.tip, Vector3.one), Is.LessThan(0.001f));
    }

    [Test]
    public void Tick_BackAtRest_HidesTheMeshAndStopsRewritingIt()
    {
        LianaArm rendered = CreateRenderedArm();
        Renderer renderer = _parent.GetComponentInChildren<Renderer>(true);
        Mesh mesh = _parent.GetComponentInChildren<MeshFilter>().sharedMesh;
        rendered.Begin(1, GestureKind.Attack, Vector3.one);
        rendered.SetTipGoal(1, Vector3.one);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
        rendered.End(1);
        rendered.Tick(1f, Vector3.zero, Quaternion.identity);
        Vector3[] vertices = mesh.vertices;

        rendered.SetVisible(true);
        rendered.Tick(1f, Vector3.one, Quaternion.identity);

        Assert.IsFalse(renderer.enabled);
        Assert.IsFalse(TipRoot().gameObject.activeSelf);
        CollectionAssert.AreEqual(vertices, mesh.vertices);
    }

    [Test]
    public void Dispose_RenderedArm_DestroysItsMesh()
    {
        LianaArm rendered = CreateRenderedArm();
        Mesh mesh = _parent.GetComponentInChildren<MeshFilter>().sharedMesh;

        rendered.Dispose();

        Assert.IsFalse(mesh);
    }

    [Test]
    public void Init_AuthoredArm_RestMeshHasOneNormalPerVertex()
    {
        CreateRenderedArm();

        Mesh mesh = _parent.GetComponentInChildren<MeshFilter>().sharedMesh;

        Assert.Greater(mesh.vertexCount, 0);
        Assert.AreEqual(mesh.vertexCount, mesh.normals.Length,
            "QuickOutline indexes normals per vertex on Awake");
    }

    [Test]
    public void Init_MismatchedRestJoints_LogsAndReturnsFalse()
    {
        ArmDefinition definition = _recipe.arms[0];
        definition.restJoints = new Vector3[3];
        LianaArm arm = new LianaArm();
        LogAssert.Expect(LogType.Error, "[LianaArm] Invalid arm definition.");

        bool isInitialized = arm.Init(definition, null, null, RenderTestAssets.LoadMeshes(), null);

        Assert.IsFalse(isInitialized);
    }

    [Test]
    public void Tick_NonfiniteGoal_HidesChainAndLogsOnce()
    {
        LianaArm rendered = CreateRenderedArm();
        Renderer renderer = _parent.GetComponentInChildren<Renderer>(true);
        rendered.Begin(1, GestureKind.Attack, Vector3.one);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
        LogAssert.Expect(LogType.Error, "[LianaArm] The chain has no finite solution, it stays hidden this frame.");

        rendered.SetTipGoal(1, Vector3.one * float.NaN);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);

        Assert.IsFalse(renderer.enabled);
    }

    [Test]
    public void Init_TipColourSet_TipRestsInIt()
    {
        ArmDefinition definition = _recipe.arms[0];
        definition.tipColour = Color.magenta;

        LianaArm arm = CreateArm(definition);

        Assert.AreEqual(Color.magenta, arm.tipColour);
        Assert.AreEqual(Color.magenta, arm.restTipColour);
    }

    [Test]
    public void Init_TipColourAlphaZero_TipRestsInArmColour()
    {
        ArmDefinition definition = _recipe.arms[0];
        definition.tipColour = new Color(1f, 0f, 1f, 0f);

        LianaArm arm = CreateArm(definition);

        Assert.AreEqual(definition.colour, arm.tipColour);
    }

    [Test]
    public void SetTipAccent_LiveGesture_ColoursTipUntilRest()
    {
        _arm.Begin(1, GestureKind.Attack, Vector3.one);

        _arm.SetTipAccent(2, Color.cyan); // another lease
        Color stale = _arm.tipColour;
        _arm.SetTipAccent(1, Color.red);
        Color live = _arm.tipColour;
        _arm.End(1);
        _arm.Tick(1f, Vector3.zero, Quaternion.identity);

        Assert.AreEqual(_arm.restTipColour, stale);
        Assert.AreEqual(Color.red, live);
        Assert.AreEqual(GesturePhase.Rest, _arm.phase);
        Assert.AreEqual(_arm.restTipColour, _arm.tipColour);
    }

    [Test]
    public void SetTipAccent_AtRest_KeepsRestColour()
    {
        _arm.SetTipAccent(0, Color.red);

        Assert.AreEqual(_arm.restTipColour, _arm.tipColour);
    }

    [TestCase(DeliveryStyle.Direct)]
    [TestCase(DeliveryStyle.Arc)]
    [TestCase(DeliveryStyle.Swarm)]
    [TestCase(DeliveryStyle.Thrown)]
    public void Tick_Style_TipDrawsItsFragment(DeliveryStyle style)
    {
        LianaArm rendered = CreateRenderedArm();
        rendered.style = style;
        rendered.isDeliveryProfile = true;

        rendered.Begin(1, GestureKind.Attack, Vector3.right);
        rendered.SetTipGoal(1, Vector3.right);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);

        int expectedParts = RenderTestAssets.LoadDeliveryVocabulary().GetTip(style).Length;
        Assert.AreEqual(expectedParts, TipParts());
    }

    [Test]
    public void Tick_TipFrame_LooksAlongTheLastLink()
    {
        LianaArm rendered = CreateRenderedArm();
        rendered.style = DeliveryStyle.Rigid;
        rendered.isDeliveryProfile = true;

        rendered.Begin(1, GestureKind.Attack, Vector3.right * 2f);
        rendered.SetTipGoal(1, Vector3.right * 2f);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);

        Vector3 forward = TipRoot().forward;
        Assert.That(Vector3.Dot(forward, Vector3.right), Is.GreaterThan(0.99f));
    }


}

}

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
        arm.Init(definition, parent, material, RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadDeliveryVocabulary());
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

    void Lengths()
    {
        for (int i = 0; i < _arm.segmentCount; i++)
        {
            float link = Vector3.Distance(_arm.Joint(i), _arm.Joint(i + 1));
            Assert.That(link, Is.EqualTo(_recipe.arms[0].segmentLength).Within(0.00001));
        }
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
        Assert.AreEqual(0, rendered.meshRevision);
        Assert.AreEqual(0, rendered.activeLeafCount);

        rendered.Begin(1, GestureKind.Attack, Vector3.one);
        rendered.SetTipGoal(1, Vector3.one);
        rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);

        Assert.IsTrue(renderers[0].enabled);
        Assert.AreEqual(LianaArm.LeafCount, rendered.activeLeafCount);
        Assert.That(Vector3.Distance(rendered.tipMatrix.GetColumn(3), rendered.tip), Is.LessThan(0.00001f));
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
        int revision = rendered.meshRevision;
        Vector3[] vertices = mesh.vertices;

        rendered.SetVisible(true);
        rendered.Tick(1f, Vector3.one, Quaternion.identity);

        Assert.IsFalse(renderer.enabled);
        Assert.AreEqual(revision, rendered.meshRevision);
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
    public void Contact_Heal_SkipsAnticipationAndReturnsToExactRestPose()
    {
        _arm.Begin(1, GestureKind.Heal, Vector3.one);
        _arm.Contact(1, Vector3.one);
        _arm.Tick(0.001f, Vector3.zero, Quaternion.identity);
        Assert.Less(Vector3.Distance(_arm.tip, Vector3.one), 0.001f);
        Lengths();
        for (int i = 0; i < 30; i++)
        {
            _arm.Tick(0.01f, Vector3.zero, Quaternion.identity);
            Lengths();
        }

        Assert.AreEqual(GesturePhase.Rest, _arm.phase);
        for (int i = 0; i <= _arm.segmentCount; i++)
        {
            Assert.AreEqual(_recipe.arms[0].restJoints[i], _arm.Joint(i));
        }
    }

    [Test]
    public void End_StaleOrRepeatedToken_DoesNotRetractNewGesture()
    {
        _arm.Begin(1, GestureKind.Attack, Vector3.one);
        _arm.Begin(2, GestureKind.Attack, Vector3.up);
        _arm.End(1);
        _arm.End(1);
        Assert.AreEqual(GesturePhase.Extend, _arm.phase);
        _arm.Tick(0.02f, Vector3.zero, Quaternion.identity);
        _arm.End(2);
        _arm.End(2);
        Assert.AreEqual(GesturePhase.Retract, _arm.phase);
        _arm.Tick(0.1f, Vector3.zero, Quaternion.identity);
        Lengths();
        _arm.End(2);
        _arm.Tick(0.11f, Vector3.zero, Quaternion.identity);
        Assert.AreEqual(GesturePhase.Rest, _arm.phase);
    }

    [Test]
    public void SetTipGoal_ProjectileGoal_TipFollowsIt()
    {
        _arm.Begin(1, GestureKind.Attack, Vector3.one);
        _arm.SetTipGoal(1, Vector3.up * 2f);

        _arm.Tick(0.001f, Vector3.zero, Quaternion.identity);

        Assert.Less(Vector3.Distance(_arm.tip, Vector3.up * 2f), 0.001f);
        Lengths();
    }

    [Test]
    public void End_RightAfterContact_StillShowsContact()
    {
        _arm.Begin(1, GestureKind.Attack, Vector3.one);
        _arm.Contact(1, Vector3.one);
        _arm.End(1);

        _arm.Tick(0.016f, Vector3.zero, Quaternion.identity);

        Assert.AreEqual(GesturePhase.Contact, _arm.phase);
        Assert.Less(Vector3.Distance(_arm.tip, Vector3.one), 0.001f);
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

    [TestCase(DeliveryStyle.Arc)]
    [TestCase(DeliveryStyle.Rigid)]
    [TestCase(DeliveryStyle.Bounce)]
    public void Tick_DeliveryProfile_FollowsLiveEndpoint(DeliveryStyle style)
    {
        // Which styles draw as a rod is the vocabulary's arm entry
        _arm.style = style;
        _arm.isDeliveryProfile = true;
        _arm.Begin(1, GestureKind.Attack, Vector3.right * 2f);
        _arm.SetTipGoal(1, Vector3.right * 2f);
        _arm.Tick(0.016f, Vector3.zero, Quaternion.identity);
        Assert.That(Vector3.Distance(_arm.tip, Vector3.right * 2f), Is.LessThan(0.001f));
        if (style == DeliveryStyle.Arc)
        {
            Assert.Greater(_arm.Joint(_arm.segmentCount / 2).y, 0.1f);
        }
        else
        {
            Assert.AreEqual(0, _arm.Joint(_arm.segmentCount / 2).y);
        }

        _arm.Contact(1, Vector3.right * 2f);
        _arm.SetTipGoal(1, Vector3.forward);
        _arm.Tick(0.016f, Vector3.zero, Quaternion.identity);
        Assert.That(Vector3.Distance(_arm.tip, Vector3.forward), Is.LessThan(0.001f));
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

        Assert.AreEqual(style, rendered.tipFragment.style);
        int expectedParts = RenderTestAssets.LoadDeliveryVocabulary().GetTip(style).Length;
        Assert.AreEqual(expectedParts, rendered.tipFragment.partCount);
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

        Vector3 forward = rendered.tipMatrix.MultiplyVector(Vector3.forward).normalized;
        Assert.That(Vector3.Dot(forward, Vector3.right), Is.GreaterThan(0.99f));
    }

    [Test]
    public void Contact_HealerArm_ReachesAcrossBoardAndClampsOutsideIt()
    {
        LianaArm arm = CreateRenderedArm();
        CreatureRecipe healer = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/Healer.asset");
        arm.Tick(0f, Vector3.zero, Quaternion.identity);
        Vector3 target = new Vector3(15f, 4f, 15f);
        arm.Begin(1, GestureKind.Attack, target);
        arm.Contact(1, target);
        arm.Tick(0.016f, Vector3.zero, Quaternion.identity);
        Assert.IsTrue(arm.lastResult.reached, arm.lastResult.error.ToString());
        Assert.Less(Vector3.Distance(arm.tip, target), 0.001f);

        arm.Contact(1, Vector3.right * 100f);
        arm.Tick(0.016f, Vector3.zero, Quaternion.identity);

        Assert.IsTrue(arm.lastResult.clamped);
        for (int i = 0; i < arm.segmentCount; i++)
        {
            float link = Vector3.Distance(arm.Joint(i), arm.Joint(i + 1));
            Assert.That(link, Is.EqualTo(healer.arms[0].segmentLength).Within(0.00001));
        }
    }
}

}

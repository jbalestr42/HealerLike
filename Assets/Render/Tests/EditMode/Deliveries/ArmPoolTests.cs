using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{

public class ArmPoolTests
{
    GameObject _parent;
    Material _material;
    CreatureRecipe _recipe;
    CreatureRig _rig;
    ArmPool _pool;
    GameObject _projectile;
    float _time;

    [SetUp]
    public void SetUp()
    {
        _parent = new GameObject("TestRig");
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        _recipe = RenderTestAssets.CreateRecipe();
        _recipe.idle = default;
        CreatePool(_recipe);
        _projectile = new GameObject("Projectile");
    }

    [TearDown]
    public void TearDown()
    {
        _pool.Dispose();
        _rig.Dispose();
        Object.DestroyImmediate(_projectile);
        Object.DestroyImmediate(_parent);
        Object.DestroyImmediate(_material);
        Object.DestroyImmediate(_recipe);
    }

    void CreatePool(CreatureRecipe recipe)
    {
        _rig = RenderTestAssets.CreateRig(recipe, _parent.transform, _material);
        _pool = new ArmPool();
        _pool.Init(_rig, _material, RenderTestAssets.LoadMeshes(), RenderTestAssets.LoadDeliveryVocabulary());
    }

    // The rig places the root and the sway first, the arms hang from them
    void Tick(float deltaTime)
    {
        _time += deltaTime;
        _rig.Tick(_time, deltaTime, new FootFrame(Vector3.zero, Vector3.up, 1f));
        _pool.Tick(deltaTime);
    }

    int ArmObjects()
    {
        int count = 0;
        foreach (MeshRenderer renderer in _rig.root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.name == "LianaArm")
            {
                count++;
            }
        }
        return count;
    }

    // An arm draws its chain only while it is out
    int DrawnArms()
    {
        int count = 0;
        foreach (MeshRenderer renderer in _rig.root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.name == "LianaArm" && renderer.enabled)
            {
                count++;
            }
        }
        return count;
    }

    [Test]
    public void Init_Recipe_OneRestingArmPerRecipeArm()
    {
        Tick(0f);

        Assert.AreEqual(_recipe.arms.Length, ArmObjects());
        Assert.AreEqual(0, DrawnArms());
    }

    [Test]
    public void ContactDelivery_ShortDirectReach_DoesNotCoilUnusedLength()
    {
        _pool.Dispose();
        _rig.Dispose();
        CreatureRecipe healer = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/Render/Creatures/Data/Healer.asset");
        CreatePool(healer);
        Tick(0f);
        Vector3 target = new Vector3(2f, 1.3f, 0f);

        Assert.IsTrue(_pool.BeginDelivery(500, DeliveryStyle.Direct, null, target));
        _pool.ContactDelivery(500, target, null);
        Tick(0.02f);

        MeshFilter arm = _rig.root.Find("LianaArm").GetComponent<MeshFilter>();
        Assert.Less(arm.sharedMesh.bounds.size.magnitude, 4f,
            "A two-cell real delivery must not loop the unused 24-cell reach around the actor.");
    }

    LianaArm[] Arms()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        ArmLeases leases = (ArmLeases)typeof(ArmPool).GetField("_leases", flags).GetValue(_pool);
        return (LianaArm[])typeof(ArmLeases).GetField("_arms", flags).GetValue(leases);
    }

    [Test]
    public void SetAccent_ChainDelivery_TintsEveryBranchOfItsLeaseOnly()
    {
        _pool.BeginDelivery(1, DeliveryStyle.ChainSync, null, Vector3.one);
        _pool.ContactDelivery(1, Vector3.one, null);
        _pool.ContactDelivery(1, Vector3.right * 2f, null);
        _pool.BeginDelivery(2, DeliveryStyle.Direct, null, Vector3.one);

        _pool.SetAccent(1, Color.magenta);

        int tinted = 0;
        foreach (LianaArm arm in Arms())
        {
            if (arm != null && arm.tipColour == Color.magenta)
            {
                tinted++;
            }
        }
        Assert.AreEqual(2, tinted);
    }

    [Test]
    public void Tick_LiveProjectile_ArmFollowsWithoutObserverPush()
    {
        _projectile.transform.position = new Vector3(1f, 1f, 0f);
        Assert.IsTrue(_pool.BeginDelivery(7, DeliveryStyle.Direct, _projectile.transform, Vector3.one));
        Tick(0.2f);

        _projectile.transform.position = new Vector3(2f, 1f, 0f);
        Tick(0.016f);

        LianaArm[] arms = Arms();
        Assert.That(Vector3.Distance(arms[0].goal, _projectile.transform.position), Is.LessThan(0.00001f));
    }

    [Test]
    public void BeginDelivery_PoolSaturated_RefusesWithoutStealing()
    {
        for (int i = 1; i <= ArmPool.MaxArms; i++)
        {
            Assert.IsTrue(_pool.BeginDelivery(i, DeliveryStyle.Direct, null, Vector3.one));
        }

        bool isClaimed = _pool.BeginDelivery(ArmPool.MaxArms + 1, DeliveryStyle.Direct, null, Vector3.one);
        Tick(0.016f);

        Assert.IsFalse(isClaimed);
        Assert.AreEqual(ArmPool.MaxArms, DrawnArms());
    }

    [Test]
    public void CancelAll_OutArms_RetractToRest()
    {
        for (int i = 1; i <= ArmPool.MaxArms; i++)
        {
            _pool.BeginDelivery(i, DeliveryStyle.Direct, null, Vector3.one);
        }

        _pool.CancelAll();
        Tick(0.05f);
        Tick(0.21f);

        Assert.AreEqual(0, DrawnArms());
    }

    [Test]
    public void HealContact_CancelledSaturatedPool_StaysCancelled()
    {
        for (int i = 0; i < ArmPool.MaxArms; i++)
        {
            _pool.HealContact(Vector3.one);
        }

        _pool.CancelAll();
        Tick(0.05f);
        _pool.HealContact(Vector3.one);
        Tick(0.21f);

        Assert.AreEqual(0, DrawnArms());
    }

    [Test]
    public void BeginDelivery_SwarmCapAndStaleOrUnsupportedTokens_AreRefused()
    {
        Assert.IsFalse(_pool.BeginDelivery(1, DeliveryStyle.Thrown, null, Vector3.one));
        for (int i = 1; i <= ArmPool.MaxSwarm; i++)
        {
            Assert.IsTrue(_pool.BeginDelivery(i, DeliveryStyle.Swarm, null, Vector3.one));
        }

        Assert.IsFalse(_pool.BeginDelivery(ArmPool.MaxSwarm + 1, DeliveryStyle.Swarm, null, Vector3.one));
        Assert.IsFalse(_pool.BeginDelivery(1, DeliveryStyle.Direct, null, Vector3.one));
        _pool.EndDelivery(1);
        _pool.EndDelivery(1);
        Assert.IsTrue(_pool.BeginDelivery(ArmPool.MaxSwarm + 1, DeliveryStyle.Swarm, null, Vector3.one));
    }

    [Test]
    public void ContactDelivery_ChainSync_BranchesFromEachContactAndRetractsTogether()
    {
        Assert.IsTrue(_pool.BeginDelivery(123, DeliveryStyle.ChainSync, null, Vector3.one));
        _pool.ContactDelivery(123, Vector3.one, null);
        _pool.ContactDelivery(123, Vector3.right * 2f, null);
        Tick(0.1f);
        Assert.AreEqual(2, DrawnArms());

        _pool.EndDelivery(123);
        Tick(0.3f);

        Assert.AreEqual(0, DrawnArms());
    }

    [Test]
    public void Tick_RootPlacedFarAway_EndsItsGestures()
    {
        _pool.BeginDelivery(1, DeliveryStyle.Direct, null, Vector3.one);
        Tick(0.016f);

        _rig.Tick(_time, 0.016f, new FootFrame(Vector3.right * 10f, Vector3.up, 1f));
        _pool.Tick(0.016f);
        Tick(0.3f);

        Assert.AreEqual(0, DrawnArms());
    }

    [Test]
    public void Tick_WarmPool_AllocatesNothing()
    {
        for (int i = 0; i < 20; i++)
        {
            Tick(0.016f);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            Tick(0.016f);
        }

        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Test]
    public void Dispose_Twice_ReleasesTheArmsOnce()
    {
        _pool.Dispose();
        _pool.Dispose();

        Assert.AreEqual(0, ArmObjects());
    }
}

}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class HLSpellEffectTests
{
    readonly List<GameObject> _objects = new List<GameObject>();

    HLSpellEffect CreateEffect(string prefab)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Spells/Prefabs/" + prefab + ".prefab");
        GameObject go = Object.Instantiate(asset);
        _objects.Add(go);
        HLSpellEffect effect = go.GetComponent<HLSpellEffect>();
        effect.Init();
        return effect;
    }

    GameObject CreateObject(string name)
    {
        GameObject go = new GameObject(name);
        _objects.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            Object.DestroyImmediate(go);
        }
        _objects.Clear();
    }

    [TestCase("HLFx_HealSpheres")]
    [TestCase("HLFx_Impact")]
    [TestCase("HLStatus_Buff")]
    [TestCase("HLStatus_Shield")]
    [TestCase("HLFx_ChainBeam")]
    [TestCase("HLFx_PoisonDrips")]
    [TestCase("HLFx_HostileLitter")]
    public void Advance_AfterWarmup_AllocatesNothing(string prefab)
    {
        HLSpellEffect effect = CreateEffect(prefab);
        effect.SetEndpoints(Vector3.zero, Vector3.right * 4f);
        for (int i = 0; i < 16; i++)
        {
            effect.Advance(0.001f);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 64; i++)
        {
            effect.Advance(0.001f);
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
    }

    [Test]
    public void Advance_Heal_BudsGrowThenPopAndStalksStayConnected()
    {
        HLSpellEffect effect = CreateEffect("HLFx_HealSpheres");
        effect.Advance(0f);
        float seed = effect.parts[0].localScale.x;

        effect.Advance(effect.lifetime * 0.6f);

        Assert.Greater(effect.parts[0].localScale.x, seed);
        for (int i = 0; i < effect.parts.Length; i++)
        {
            Transform stalk = effect.stalks[i];
            Assert.AreEqual(effect.parts[i].localPosition.y, stalk.localPosition.y + stalk.localScale.y, 0.00001f);
            Assert.AreEqual(-0.12f, stalk.localPosition.y - stalk.localScale.y, 0.00001f);
        }

        effect.Advance(effect.lifetime * 0.35f);

        Assert.AreEqual(0f, effect.parts[0].localScale.x, 0.00001f);
        Assert.AreEqual(0f, effect.stalks[0].localScale.x, 0.00001f);
    }

    [Test]
    public void SetStatus_PeriodicHeal_BudsReadOnlyTickTime()
    {
        HLSpellEffect effect = CreateEffect("HLFx_HealSpheres");
        effect.SetPeriod(true, 2f);

        effect.SetStatus(1, 1f, 10f, HLClockKind.Simulation);
        Assert.AreEqual(Vector3.zero, effect.parts[0].localScale); // before the first tick

        effect.SetStatus(1, 2.6f, 10f, HLClockKind.Simulation);
        Vector3 position = effect.parts[0].localPosition;
        Vector3 scale = effect.parts[0].localScale;
        Assert.Greater(scale.x, 0f);

        effect.Advance(1f);
        Assert.AreEqual(position, effect.parts[0].localPosition);
        Assert.AreEqual(scale, effect.parts[0].localScale);

        effect.SetStatus(1, 3.9f, 10f, HLClockKind.Simulation);
        Assert.AreEqual(Vector3.zero, effect.parts[0].localScale); // popped at the end of the period
    }

    [Test]
    public void FaceCamera_Impact_StarFacesCameraAndShardsFall()
    {
        HLSpellEffect effect = CreateEffect("HLFx_Impact");
        GameObject camera = CreateObject("Camera");
        camera.transform.rotation = Quaternion.Euler(35f, 20f, 0f);

        effect.FaceCamera(camera.transform);
        float start = effect.parts[1].localPosition.y;
        effect.Advance(0.2f);
        float peak = effect.parts[1].localPosition.y;
        effect.Advance(0.4f);

        Assert.Less(Quaternion.Angle(camera.transform.rotation, effect.parts[0].rotation), 0.001f);
        Assert.Greater(peak, start);
        Assert.Less(effect.parts[1].localPosition.y, start);
        Assert.AreEqual(5, effect.parts.Length);
    }

    [Test]
    public void SetStatus_Buff_OrbitAndGlowFollowObservedTime()
    {
        HLSpellEffect effect = CreateEffect("HLStatus_Buff");
        effect.SetStatus(1, 0f, 10f, HLClockKind.Simulation);
        Vector3 normal = effect.parts[0].up;
        Vector3 scale = effect.parts[0].localScale;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        effect.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Color color = block.GetColor("_BaseColor");

        effect.SetStatus(1, 1f, 10f, HLClockKind.Simulation);

        effect.parts[0].GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.AreNotEqual(normal, effect.parts[0].up);
        Assert.AreNotEqual(scale, effect.parts[0].localScale);
        Assert.AreNotEqual(color, block.GetColor("_BaseColor"));
    }

    [Test]
    public void Advance_Litter_EmergesThenSinks()
    {
        HLSpellEffect effect = CreateEffect("HLFx_HostileLitter");
        effect.Advance(0f);
        float buried = effect.parts[0].localPosition.y;

        effect.Advance(0.2f);
        float emerged = effect.parts[0].localPosition.y;
        effect.Advance(effect.lifetime);

        Assert.Greater(emerged, buried);
        Assert.Less(effect.parts[0].localPosition.y, emerged);
    }

    [Test]
    public void SetStatus_Drip_WaitsForTheFirstTick()
    {
        HLSpellEffect effect = CreateEffect("HLFx_PoisonDrips");
        effect.SetPeriod(true, 2f);

        effect.SetStatus(1, 1.9f, 10f, HLClockKind.Simulation);
        Assert.AreEqual(Vector3.zero, effect.parts[0].localScale);

        effect.SetStatus(1, 2f, 10f, HLClockKind.Simulation);
        Vector3 position = effect.parts[0].localPosition;
        effect.Advance(1f);

        Assert.Greater(effect.parts[0].localScale.y, 0f);
        Assert.AreEqual(position, effect.parts[0].localPosition);
    }

    [Test]
    public void SetStatus_Drip_FollowsPeriodAndBodyTintResetsOnRemoval()
    {
        HLSpellEffect effect = CreateEffect("HLFx_PoisonDrips");
        effect.SetPeriod(true, 2f);
        Color tint = Color.clear;
        effect.OnBodyTint.AddListener(color => tint = color);

        effect.SetStatus(1, 2f, 6f, HLClockKind.Simulation);
        Vector3 position = effect.parts[0].localPosition;
        Assert.AreEqual((Color)new Color32(242, 96, 122, 255), tint);

        effect.SetStatus(1, 3f, 6f, HLClockKind.Simulation);
        Assert.Less(effect.parts[0].localPosition.y, position.y);

        effect.SetStatus(1, 4f, 6f, HLClockKind.Simulation);
        Assert.AreEqual(position, effect.parts[0].localPosition);

        effect.BeginRemoval();
        Assert.AreEqual(Color.white, tint);
    }

    [Test]
    public void SetEndpoints_ContactThread_HidesBeadsAndEndsOnTheContact()
    {
        HLSpellEffect effect = CreateEffect("HLFx_ChainBeam");
        effect.contactThread = true;

        effect.SetEndpoints(Vector3.zero, Vector3.right * 4f);

        Transform last = effect.parts[30];
        Vector3 tip = last.position + last.up * last.localScale.y;
        Assert.IsFalse(effect.parts[1].gameObject.activeSelf);
        Assert.AreEqual(0f, effect.parts[0].position.y, 0.00001f);
        Assert.Less(Vector3.Distance(Vector3.right * 4f, tip), 0.00001f);

        effect.SetEndpoints(Vector3.one, Vector3.one);

        foreach (Transform part in effect.parts)
        {
            Assert.IsFalse(float.IsNaN(part.position.x));
        }
    }

    [Test]
    public void SetShieldState_Repeated_AllocatesNothing()
    {
        HLSpellEffect effect = CreateEffect("HLStatus_Shield");
        for (int i = 0; i < 32; i++)
        {
            effect.SetStatus(2, 1f, 4f, HLClockKind.Simulation);
            effect.SetSide(Entity.EntityType.Player);
            effect.SetShieldState(2f);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 32; i++)
        {
            effect.SetStatus(2, 1f, 4f, HLClockKind.Simulation);
            effect.SetSide(Entity.EntityType.Player);
            effect.SetShieldState(2f);
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
    }

    [Test]
    public void SetShieldState_Charges_ShowsOnePlatePerCharge()
    {
        HLSpellEffect effect = CreateEffect("HLStatus_Shield");
        effect.SetStatus(9, 0f, 4f, HLClockKind.Simulation);

        effect.SetShieldState(2f);

        int count = 0;
        foreach (Transform part in effect.parts)
        {
            if (part.gameObject.activeSelf)
            {
                count++;
            }
        }
        Assert.AreEqual(2, count);
        Assert.AreEqual(9, effect.stacks);

        effect.SetShieldState(3f);

        Assert.IsTrue(effect.parts[2].gameObject.activeSelf);
    }

    [Test]
    public void SetStatus_Buff_PoseOnlyMovesWithObservedTime()
    {
        HLSpellEffect effect = CreateEffect("HLStatus_Buff");
        effect.SetStatus(1, 1f, 4f, HLClockKind.Simulation);
        Quaternion pose = effect.parts[0].localRotation;

        effect.Advance(7f);
        Assert.AreEqual(pose, effect.parts[0].localRotation);

        effect.SetStatus(2, 2f, 4f, HLClockKind.Simulation);
        Assert.AreNotEqual(pose, effect.parts[0].localRotation);
    }

    [Test]
    public void BeginRemoval_Shield_OpensThePlates()
    {
        HLSpellEffect effect = CreateEffect("HLStatus_Shield");
        effect.SetStatus(1, 0.25f, 4f, HLClockKind.Simulation);
        Quaternion closed = effect.parts[0].localRotation;
        float closedRadius = effect.parts[0].localPosition.magnitude;

        effect.BeginRemoval();
        effect.Advance(0.25f);

        Assert.IsTrue(effect.removalComplete);
        Assert.AreNotEqual(closed, effect.parts[0].localRotation);
        Assert.Greater(effect.parts[0].localPosition.magnitude, closedRadius);
    }

    [Test]
    public void Advance_Chain_BeadsTravelAboveTheChord()
    {
        HLSpellEffect effect = CreateEffect("HLFx_ChainBeam");
        effect.SetEndpoints(Vector3.zero, Vector3.right * 4f);

        Assert.AreEqual(Vector3.zero, effect.parts[1].position);
        Assert.Greater(effect.parts[17].position.y, 0f);
        Assert.AreEqual(32, effect.parts.Length);

        effect.Advance(0.15f);

        Assert.Greater(effect.parts[1].position.x, 0f);
        Assert.Greater(effect.parts[1].position.y, 0f);
    }

    [Test]
    public void SetSide_Twice_ReusesTheAuthoredRim()
    {
        HLSpellEffect effect = CreateEffect("HLFx_HealSpheres");
        int count = effect.transform.childCount;

        effect.SetSide(Entity.EntityType.Player);
        effect.SetSide(Entity.EntityType.Computer);

        Assert.AreEqual(count, effect.transform.childCount);
        Assert.IsTrue(effect.transform.Find("SideRim").gameObject.activeSelf);
    }

    [Test]
    public void ShowCritical_Heal_ShowsTwoRings()
    {
        HLSpellEffect effect = CreateEffect("HLFx_HealSpheres");

        effect.ShowCritical();

        int rings = 0;
        foreach (Transform child in effect.transform)
        {
            if (child.name == "CriticalRing" && child.gameObject.activeSelf)
            {
                rings++;
            }
        }
        Assert.AreEqual(2, rings);
    }

    [Test]
    public void SetStatus_Stacks_ShowsOneBeadPerStack()
    {
        HLSpellEffect effect = CreateEffect("HLStatus_Buff");

        effect.SetStatus(3, 0f, 4f, HLClockKind.Simulation);

        int beads = 0;
        foreach (Transform child in effect.transform)
        {
            if (child.name == "StackBead" && child.gameObject.activeSelf)
            {
                beads++;
            }
        }
        Assert.AreEqual(3, beads);
    }

    [Test]
    public void Advance_Status_DoesNotExpire()
    {
        HLSpellEffect heal = CreateEffect("HLFx_HealSpheres");
        HLSpellEffect status = CreateEffect("HLStatus_Buff");
        float before = heal.parts[0].localPosition.y;

        heal.Advance(0.2f);
        status.SetStatus(3, 2f, 4f, HLClockKind.Realtime);
        status.Advance(10f);

        Assert.Greater(heal.parts[0].localPosition.y, before);
        Assert.AreEqual(3, status.stacks);
        Assert.AreEqual(2f, status.elapsedSeconds);
    }

    [Test]
    public void SetColor_Tint_ColoursEveryPart()
    {
        HLSpellEffect effect = CreateEffect("HLStatus_Buff");
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        effect.SetColor(Color.red);

        foreach (Transform part in effect.parts)
        {
            part.GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.AreEqual(Color.red, block.GetColor("_BaseColor"));
        }
    }
}
}

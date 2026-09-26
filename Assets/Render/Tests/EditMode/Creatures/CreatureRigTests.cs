using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEditor;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{

public class CreatureRigTests : CreatureRigFixture
{
    [Test]
    public void Init_GlowingPart_BrightensBaseColourAndKeepsAlpha()
    {
        _rig.Dispose();
        _recipe.parts[0].glow = 2f;
        _recipe.parts[0].colour = new Color(0.2f, 0.4f, 0.1f, 0.7f);
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
        // 0.4 lit by glow 2 is 1.2, give or take the 8% colour jitter
        Assert.That(BaseColour(renderer).g, Is.InRange(1.104f, 1.296f));
        Assert.That(BaseColour(renderer).a, Is.EqualTo(0.7f).Within(0.0001f));
        _rig.SetReadout(null, 1f, 0f, 1f);
        _rig.Tick(0f, 0f, ground);
        Assert.Greater(BaseColour(renderer).g, 1);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Assert.Less(BaseColour(renderer).g, 1);
    }

    [Test]
    public void SetReadout_ChargeAndWilt_SwellBudAndSuppressGlow()
    {
        _rig.Dispose();
        _recipe.parts[0].glow = 1f;
        _recipe.parts[0].role = PartRole.Tip;
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Vector3 scale = renderer.transform.localScale;
        Color colour = BaseColour(renderer);
        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, ground);
        Assert.Greater(renderer.transform.localScale.x, scale.x * 1.2f);
        Assert.Greater(BaseColour(renderer).g, colour.g);
        _rig.SetReadout(null, 0f, 1f, 1f);
        _rig.Tick(0f, 0f, ground);
        Assert.Less(BaseColour(renderer).g, 0.5f);
    }

    [Test]
    public void Tick_FullCharge_PlantBodySphereDoesNotSwell()
    {
        CreateDerivedRig(LookSide.Plant);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Vector3 rest = _rig.partTransforms[0].localScale;
        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, ground);
        Assert.AreEqual(Primitive.Sphere, _recipe.parts[0].primitive);
        Assert.AreEqual(rest.x, _rig.partTransforms[0].localScale.x, 0.0001f);
    }

    [Test]
    public void Tick_ChargeThenWilt_TipKeepsItsHueAndBrightensThenDarkens()
    {
        CreateDerivedRig(LookSide.Plant);
        int tip = System.Array.FindIndex(_recipe.parts, part => part.role == PartRole.Tip);
        _rig.SetReadout(null, 1f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Color rest = TipColour(tip);
        _rig.SetReadout(null, 1f, 1f, 0f);
        _rig.Tick(0f, 0f, ground);
        Color charged = TipColour(tip);
        Color.RGBToHSV(rest, out float restHue, out _, out float restValue);
        Color.RGBToHSV(charged, out float chargedHue, out _, out float chargedValue);
        Assert.AreEqual(restHue, chargedHue, 0.01f);
        Assert.Greater(chargedValue, restValue);
        // Palette accent after the colour-space round trip.
        Assert.That(Vector4.Distance(_recipe.parts[tip].colour, rest), Is.LessThan(0.000001f));
        _rig.SetReadout(null, 0f, 0f, 0f);
        _rig.Tick(0f, 0f, ground);
        Color.RGBToHSV(TipColour(tip), out float wiltedHue, out _, out float wiltedValue);
        Assert.AreEqual(restHue, wiltedHue, 0.01f);
        Assert.Less(wiltedValue, restValue);
    }

    [Test]
    public void Tick_Crown_SpinsByRoleNotById()
    {
        _rig.Dispose();
        _recipe.parts[0].role = PartRole.Crown;
        _recipe.parts[0].id = "Ring";
        _rig = RenderTestAssets.CreateRig(_recipe, _parent.transform, _material);
        _rig.Tick(0f, 0f, ground);
        Quaternion before = _rig.partTransforms[0].parent.localRotation;
        _rig.Tick(5f, 0f, ground);
        float spin = CreatureRig.CrownSpinDegrees * 5f; // five seconds between the two ticks
        Quaternion after = _rig.partTransforms[0].parent.localRotation;
        Assert.That(Quaternion.Angle(before, after), Is.EqualTo(spin).Within(0.01f));
    }

    [Test]
    public void Tick_WarmRig_AllocatesNothing()
    {
        for (int i = 0; i < 20; i++)
        {
            _rig.Tick(i * 0.016f, 0.016f, ground);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _rig.Tick(i * 0.016f, 0.016f, ground);
        }

        Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Test]
    public void Tick_TranslatedFootFrame_ReplantsRootsWithoutMovingParent()
    {
        _parent.transform.position = new Vector3(3f, 2f, 4f);
        _rig.Tick(2f, 0.016f, new FootFrame(new Vector3(3f, 0.5f, 4f), Vector3.up, 1f));
        Assert.AreEqual(new Vector3(3f, 2f, 4f), _parent.transform.position);
        Assert.AreEqual(new Vector3(3f, 0.5f, 4f), _rig.root.position);
        Assert.NotNull(_rig.root.Find("Root"));
        Assert.IsEmpty(_parent.GetComponentsInChildren<Collider>());
    }

    // Measure actual generated vertices, excluding roots and body; transformed axis-aligned bounds would
    // overstate how wide an edge-on articulated head really appears.
    [Test]
    public void Hit_ThenHealthRestored_ShakeDecaysAndTintRecovers()
    {
        _rig.SetReadout(Vector3.forward, 0.1f, 0f, 0f);
        _rig.Hit();
        _rig.Tick(0f, 0.01f, ground);
        Transform sway = _rig.root.Find("Sway");
        Assert.Greater(Mathf.Abs(sway.localRotation.z), 0.001f);
        Renderer renderer = sway.GetComponentInChildren<Renderer>();
        Color hurt = BaseColour(renderer);
        _rig.SetReadout(Vector3.forward, 1f, 0f, 0f);
        _rig.Tick(1f, 1f, ground);
        Assert.AreNotEqual(hurt, BaseColour(renderer));
        Assert.That(Quaternion.Angle(sway.localRotation, Quaternion.identity), Is.LessThan(0.001f));
    }
}
}

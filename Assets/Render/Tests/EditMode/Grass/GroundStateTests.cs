using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundStateTests
{
    // aura: x ash, y vitality, z light, w blight, as the auras over the texel add up to
    static Vector4 Run(Vector4 state, Vector4 aura, float seconds)
    {
        GroundStateSettings settings = GroundStateSettings.Default;
        for (float t = 0f; t < seconds; t += 0.05f)
        {
            state = GroundState.Step(state, aura, 0.05f, settings);
        }

        return state;
    }

    [Test]
    public void Step_AshAura_BurnsInAboutASecondAndRegrowsSlowly()
    {
        Vector4 burnt = Run(Vector4.zero, new Vector4(1f, 0f, 0f, 0f), 2f);
        Vector4 later = Run(burnt, Vector4.zero, 1f);
        Vector4 regrown = Run(burnt, Vector4.zero, 10f);

        Assert.Greater(burnt.x, 0.9f);
        Assert.Greater(later.x, 0.6f, "A second later the ash still shows where the enemy stood.");
        Assert.Less(regrown.x, 0.05f, "Ten seconds later the grass is back.");
    }

    [Test]
    public void Step_HalfCoveredAura_AsksForHalf()
    {
        Vector4 edge = Run(Vector4.zero, new Vector4(0.5f, 0f, 0f, 0f), 20f);

        Assert.AreEqual(0.5f, edge.x, 0.01f);
    }

    [Test]
    public void Step_OverlappingAuras_AddUpToTheLimit()
    {
        Vector4 mixed = Run(Vector4.zero, new Vector4(1.6f, 0.8f, -1.2f, 0f), 20f);

        Assert.AreEqual(1f, mixed.x, 0.01f);
        Assert.AreEqual(0.8f, mixed.y, 0.01f);
        Assert.AreEqual(-1f, mixed.z, 0.01f);
    }

    [Test]
    public void Step_WiltAura_KillsTheGrassAndItRecovers()
    {
        Vector4 dead = Run(Vector4.zero, new Vector4(0f, -1f, 0f, 0f), 4f);
        Vector4 recovered = Run(dead, Vector4.zero, 20f);

        Assert.Less(dead.y, -0.95f);
        Assert.Greater(recovered.y, -0.05f);
    }

    [Test]
    public void Step_HealAura_GlowsAtOnceAndLeavesLushGrassAfterTheGlowFades()
    {
        Vector4 healed = Run(Vector4.zero, new Vector4(0f, 1f, 1f, 0f), 0.5f);
        Vector4 after = Run(healed, Vector4.zero, 0.5f);
        Vector4 gone = Run(healed, Vector4.zero, 3f);

        Assert.Greater(healed.z, 0.95f);
        Assert.Greater(healed.y, 0.3f);
        Assert.Less(after.z, 0.4f, "The glow fades quickly.");
        Assert.Greater(after.y, 0.15f, "The lush green lingers a moment longer.");
        Assert.Less(gone.y, 0.05f, "Gone before a moving creature can paint a trail.");
    }

    [Test]
    public void Step_FrostAndBlight_SettleInAndFadeOut()
    {
        Vector4 afflicted = Run(Vector4.zero, new Vector4(0f, 0f, -1f, 1f), 3f);
        Vector4 recovered = Run(afflicted, Vector4.zero, 10f);

        Assert.Less(afflicted.z, -0.95f);
        Assert.Greater(afflicted.w, 0.95f);
        Assert.Greater(recovered.z, -0.05f);
        Assert.Less(recovered.w, 0.05f);
    }

    [Test]
    public void Step_Values_StayInRange()
    {
        Vector4 state = GroundState.Step(new Vector4(2f, -3f, 5f, 4f), Vector4.zero, 0f, GroundStateSettings.Default);

        Assert.AreEqual(new Vector4(1f, -1f, 1f, 1f), state);
    }
}

}

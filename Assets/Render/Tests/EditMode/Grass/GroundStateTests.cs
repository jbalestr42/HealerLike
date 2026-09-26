using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundStateTests
{
    static Vector3 Run(Vector3 state, Vector4 aura, float seconds)
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
        Vector3 burnt = Run(Vector3.zero, new Vector4(1f, 0f, 0f, 1f), 2f);
        Vector3 later = Run(burnt, Vector4.zero, 1f);
        Vector3 regrown = Run(burnt, Vector4.zero, 10f);

        Assert.Greater(burnt.x, 0.9f);
        Assert.Greater(later.x, 0.6f, "A second later the ash still shows where the enemy stood.");
        Assert.Less(regrown.x, 0.05f, "Ten seconds later the grass is back.");
    }

    [Test]
    public void Step_HalfCoveredAura_AsksForHalf()
    {
        Vector3 edge = Run(Vector3.zero, new Vector4(0.5f, 0f, 0f, 0.5f), 20f);

        Assert.AreEqual(0.5f, edge.x, 0.01f);
    }

    [Test]
    public void Step_OverlappingAuras_Average()
    {
        // A full ash aura and a lush one over the same texel
        Vector3 mixed = Run(Vector3.zero, new Vector4(1f, 1f, 0f, 2f), 20f);

        Assert.AreEqual(0.5f, mixed.x, 0.01f);
        Assert.AreEqual(0.5f, mixed.y, 0.01f);
    }

    [Test]
    public void Step_WiltAura_KillsTheGrassAndItRecovers()
    {
        Vector3 dead = Run(Vector3.zero, new Vector4(0f, -1f, 0f, 1f), 4f);
        Vector3 recovered = Run(dead, Vector4.zero, 20f);

        Assert.Less(dead.y, -0.95f);
        Assert.Greater(recovered.y, -0.05f);
    }

    [Test]
    public void Step_HealAura_GlowsAtOnceAndLeavesLushGrassAfterTheGlowFades()
    {
        Vector3 healed = Run(Vector3.zero, new Vector4(0f, 1f, 1f, 1f), 0.5f);
        Vector3 after = Run(healed, Vector4.zero, 2f);

        Assert.Greater(healed.z, 0.95f);
        Assert.Greater(healed.y, 0.3f);
        Assert.Less(after.z, 0.25f);
        Assert.Greater(after.y, 0.15f, "The lush green lingers.");
    }

    [Test]
    public void Step_Values_StayInRange()
    {
        Vector3 state = GroundState.Step(new Vector3(2f, -3f, 5f), Vector4.zero, 0f, GroundStateSettings.Default);

        Assert.AreEqual(new Vector3(1f, -1f, 1f), state);
    }
}

}

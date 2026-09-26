using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundSpringTests
{
    static Vector2 Run(Vector2 target, float seconds, GroundSpringSettings settings, out Vector2 velocity,
                       out float peak)
    {
        Vector2 lean = Vector2.zero;
        velocity = Vector2.zero;
        peak = 0f;
        Vector4 spring = settings.ShaderSpring(0.1f);
        int steps = Mathf.RoundToInt(seconds / GroundSpring.MaxStep);
        for (int i = 0; i < steps; i++)
        {
            // A uniform field: every neighbour moves with the texel
            GroundSpring.Step(ref lean, ref velocity, target, lean, Vector2.zero, GroundSpring.MaxStep, spring);
            peak = Mathf.Max(peak, lean.magnitude);
        }

        return lean;
    }

    [Test]
    public void Step_ConstantTarget_SettlesOnIt()
    {
        Vector2 lean = Run(new Vector2(0.3f, -0.2f), 6f, GroundSpringSettings.Default, out Vector2 velocity, out _);

        Assert.That(Vector2.Distance(lean, new Vector2(0.3f, -0.2f)), Is.LessThan(0.001f));
        Assert.That(velocity.magnitude, Is.LessThan(0.01f));
    }

    [Test]
    public void Step_Underdamped_OvershootsBeforeSettling()
    {
        Run(new Vector2(0.4f, 0f), 2f, GroundSpringSettings.Default, out _, out float peak);

        Assert.Greater(peak, 0.45f, "A bouncy spring swings past the push, which is what reads as physical.");
    }

    [Test]
    public void Step_CriticalDamping_NeverOvershoots()
    {
        GroundSpringSettings settings = GroundSpringSettings.Default;
        settings.dampingRatio = 1f;

        Run(new Vector2(0.4f, 0f), 3f, settings, out _, out float peak);

        Assert.LessOrEqual(peak, 0.402f);
    }

    [Test]
    public void Step_PushReleased_SwingsBackThroughRest()
    {
        GroundSpringSettings settings = GroundSpringSettings.Default;
        Vector2 lean = new Vector2(0.5f, 0f);
        Vector2 velocity = Vector2.zero;
        float lowest = 1f;
        for (int i = 0; i < 90; i++)
        {
            GroundSpring.Step(ref lean, ref velocity, Vector2.zero, lean, Vector2.zero, GroundSpring.MaxStep,
                              settings.ShaderSpring(0.1f));
            lowest = Mathf.Min(lowest, lean.x);
        }

        Assert.Less(lowest, 0f, "Released grass whips past upright before it settles.");
    }

    [Test]
    public void Step_Coupling_PullsTowardTheNeighbours()
    {
        Vector4 spring = GroundSpringSettings.Default.ShaderSpring(0.1f);
        Vector2 alone = Vector2.zero;
        Vector2 aloneVelocity = Vector2.zero;
        Vector2 coupled = Vector2.zero;
        Vector2 coupledVelocity = Vector2.zero;

        GroundSpring.Step(ref alone, ref aloneVelocity, Vector2.zero, Vector2.zero, Vector2.zero,
                          GroundSpring.MaxStep, spring);
        GroundSpring.Step(ref coupled, ref coupledVelocity, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero,
                          GroundSpring.MaxStep, spring);

        Assert.AreEqual(Vector2.zero, alone);
        Assert.Greater(coupled.x, 0f, "A push next door starts moving this texel: the ripple.");
    }

    [Test]
    public void Step_HugeTarget_StaysWithinTheMaxLean()
    {
        GroundSpringSettings settings = GroundSpringSettings.Default;
        Vector2 lean = Run(new Vector2(10f, 0f), 1f, settings, out _, out float peak);

        Assert.LessOrEqual(peak, settings.maxLean + 1e-5f);
        Assert.LessOrEqual(lean.magnitude, settings.maxLean + 1e-5f);
    }

    [Test]
    public void Step_BriefKick_ThrowsTheGrassWhichSwingsBackToRest()
    {
        Vector4 spring = GroundSpringSettings.Default.ShaderSpring(0.1f);
        Vector2 lean = Vector2.zero;
        Vector2 velocity = Vector2.zero;
        float peak = 0f;
        float lowest = 0f;
        for (int i = 0; i < 240; i++)
        {
            Vector2 force = i < 6 ? new Vector2(100f, 0f) : Vector2.zero;
            GroundSpring.Step(ref lean, ref velocity, Vector2.zero, lean, force, GroundSpring.MaxStep, spring);
            peak = Mathf.Max(peak, lean.x);
            lowest = Mathf.Min(lowest, lean.x);
        }

        Assert.Greater(peak, 0.3f, "A tenth of a second's kick throws the grass well over.");
        Assert.Less(lowest, -0.05f, "It whips back past upright.");
        Assert.Less(lean.magnitude, 0.02f, "Then it settles, nothing holds it.");
    }

    [Test]
    public void Crush_FallsFastAndRisesSlowly()
    {
        GroundSpringSettings settings = GroundSpringSettings.Default;
        float crush = 0f;
        for (int i = 0; i < 12; i++)
        {
            crush = GroundSpring.Crush(crush, 1f, GroundSpring.MaxStep, settings.crushFall, settings.crushRise);
        }

        Assert.Greater(crush, 0.9f, "An obstacle flattens the grass within a fifth of a second.");
        for (int i = 0; i < 30; i++)
        {
            crush = GroundSpring.Crush(crush, 0f, GroundSpring.MaxStep, settings.crushFall, settings.crushRise);
        }

        Assert.Greater(crush, 0.4f, "Half a second later the wake still shows.");
        for (int i = 0; i < 300; i++)
        {
            crush = GroundSpring.Crush(crush, 0f, GroundSpring.MaxStep, settings.crushFall, settings.crushRise);
        }

        Assert.Less(crush, 0.01f);
    }

    [Test]
    public void StepCount_Frame_SplitsIntoEqualStepsNoLongerThanTheMax()
    {
        Assert.AreEqual(1, GroundSpring.StepCount(1f / 120f, out float fast));
        Assert.AreEqual(1f / 120f, fast, 1e-7f);
        Assert.AreEqual(1, GroundSpring.StepCount(1f / 60f, out _));
        Assert.AreEqual(2, GroundSpring.StepCount(1f / 30f, out float slow));
        Assert.AreEqual(1f / 60f, slow, 1e-6f);
        Assert.AreEqual(GroundSpring.MaxSteps, GroundSpring.StepCount(5f, out float hitch));
        Assert.AreEqual(GroundSpring.MaxFrame / GroundSpring.MaxSteps, hitch, 1e-6f);
    }

    [Test]
    public void StepCount_PausedOrInvalid_TakesNoStep()
    {
        Assert.AreEqual(0, GroundSpring.StepCount(0f, out _));
        Assert.AreEqual(0, GroundSpring.StepCount(-1f, out _));
        Assert.AreEqual(0, GroundSpring.StepCount(float.NaN, out _));
    }

    [Test]
    public void Target_SumsStampAndWindWithinTheMaxLean()
    {
        Assert.AreEqual(new Vector2(0.3f, 0.1f), GroundSpring.Target(new Vector2(0.2f, 0f), new Vector2(0.1f, 0.1f), 1f));
        Assert.AreEqual(1f, GroundSpring.Target(new Vector2(3f, 0f), Vector2.zero, 1f).magnitude, 1e-6f);
    }

    [Test]
    public void ShaderSpring_Frequency_GivesTheMatchingStiffnessDampingAndPull()
    {
        GroundSpringSettings settings = new GroundSpringSettings
        {
            frequency = 1f, dampingRatio = 0.5f, spread = 0.1f, maxLean = 1f
        };

        Vector4 spring = settings.ShaderSpring(0.1f);

        float omega = 2f * Mathf.PI;
        Assert.AreEqual(omega * omega, spring.x, 1e-4f);
        Assert.AreEqual(omega, spring.y, 1e-4f);
        Assert.AreEqual(4f * omega * omega, spring.z, 1e-3f);
        Assert.AreEqual(1f, spring.w);
    }

    [Test]
    public void Coupling_FinerTexels_PullHarderForTheSameWorldSpread()
    {
        GroundSpringSettings settings = GroundSpringSettings.Default;

        Assert.AreEqual(4f * settings.Coupling(0.2f), settings.Coupling(0.1f), 1e-2f);
        Assert.AreEqual(0f, settings.Coupling(0f));
    }

    [Test]
    public void Coupling_TinyTexels_StayWithinTheStableBound()
    {
        GroundSpringSettings settings = GroundSpringSettings.Default;
        float longest = Mathf.Max(GroundSpring.MaxStep, GroundSpring.MaxFrame / GroundSpring.MaxSteps);

        float pull = settings.Coupling(0.0001f);

        Assert.LessOrEqual(longest * longest * (settings.stiffness + 2f * pull), 3.0001f);
    }

    [Test]
    public void Step_HeldPush_FadesIntoTheGrassOverTheSpread()
    {
        GroundSpringSettings settings = GroundSpringSettings.Default;
        float texel = 0.1f;
        Vector4 spring = settings.ShaderSpring(texel);
        int count = 80;
        Vector2[] lean = new Vector2[count];
        Vector2[] velocity = new Vector2[count];
        Vector2[] next = new Vector2[count];
        for (int step = 0; step < 900; step++)
        {
            for (int i = 0; i < count; i++)
            {
                // A row of texels: the first ten held pushed, the rest free
                Vector2 target = i < 10 ? new Vector2(0.5f, 0f) : Vector2.zero;
                Vector2 left = lean[Mathf.Max(0, i - 1)];
                Vector2 right = lean[Mathf.Min(count - 1, i + 1)];
                // The row stands for a wide front, so the two other neighbours move with the texel
                Vector2 mean = 0.25f * (left + right + 2f * lean[i]);
                next[i] = lean[i];
                GroundSpring.Step(ref next[i], ref velocity[i], target, mean, Vector2.zero, GroundSpring.MaxStep,
                                  spring);
            }

            System.Array.Copy(next, lean, count);
        }

        float edge = lean[10].x;
        float oneSpread = lean[10 + Mathf.RoundToInt(settings.spread / texel)].x;
        Assert.Greater(edge, 0.05f);
        Assert.That(oneSpread / edge, Is.InRange(0.2f, 0.55f), "About exp(-1) one spread past the push.");
    }
}

}

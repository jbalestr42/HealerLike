using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{

// GroundSimulation runs its shader on the graphics device; these read the results back
public class GroundSimulationTests
{
    static readonly string shaderPath = "Assets/Render/Shaders/GroundSimulation.shader";
    static readonly Rect area = new Rect(-4f, -2f, 8f, 4f);

    GroundSimulation _ground;
    GroundVolume _volume;

    [SetUp]
    public void SetUp()
    {
        if (!GroundSimulation.IsSupported())
        {
            Assert.Ignore("Requires a graphics device; run the grass suite with -force-metal.");
        }

        _volume = GroundVolume.Create(area, 0.125f);
        _ground = new GroundSimulation(AssetDatabase.LoadAssetAtPath<Shader>(shaderPath), _volume,
                                   GroundSpringSettings.Default);
        Assert.IsTrue(_ground.isValid);
    }

    [TearDown]
    public void TearDown()
    {
        _ground?.Dispose();
        GroundSimulation.Unpublish();
    }

    static Color[] Read(RenderTexture texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        Texture2D copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false, true);
        copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        copy.Apply();
        RenderTexture.active = previous;
        Color[] pixels = copy.GetPixels();
        Object.DestroyImmediate(copy);
        return pixels;
    }

    Color At(Color[] pixels, Vector2 world)
    {
        Vector2 uv = _volume.ToUV(world);
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * _volume.width), 0, _volume.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * _volume.height), 0, _volume.height - 1);
        return pixels[y * _volume.width + x];
    }

    void StepStill(GroundStamp[] stamps, float seconds)
    {
        _ground.SetStamps(stamps);
        for (float t = 0f; t < seconds - 1e-4f; t += GroundSpring.MaxStep)
        {
            _ground.Step(GroundSpring.MaxStep, GroundWind.Shader(0f, t));
        }
    }

    [Test]
    public void Step_DiscStamp_FlattensOnlyWhereTheVolumeMapsIt()
    {
        GroundStamp stamp = GroundStamp.Disc(new Vector2(2f, 1f), 0.6f, 0f, 1f, 0.2f, 0f);

        StepStill(new[] { stamp }, 0.25f);

        Color[] crush = Read(_ground.crush);
        Assert.Greater(At(crush, new Vector2(2f, 1f)).r, 0.9f);
        Assert.Less(At(crush, new Vector2(2f, -1f)).r, 0.01f, "Not mirrored across the rows.");
        Assert.Less(At(crush, new Vector2(-2f, 1f)).r, 0.01f, "Not mirrored across the columns.");
        Assert.Less(At(crush, new Vector2(2f, 1.8f)).r, 0.01f, "Nothing past the radius.");
    }

    [Test]
    public void Step_UniformPush_MatchesTheCpuSpring()
    {
        // A disc so wide and far that its outward push is the same over the whole test area
        Vector2 push = new Vector2(0.3f, 0f);
        GroundStamp far = GroundStamp.Disc(new Vector2(-1000f, 0f), 2000f, push.x, 0f, 0.01f, 0f);
        Vector2 lean = Vector2.zero;
        Vector2 velocity = Vector2.zero;
        Vector4 spring = GroundSpringSettings.Default.ShaderSpring(_volume.texelSize.x);
        for (int i = 0; i < 9; i++)
        {
            GroundSpring.Step(ref lean, ref velocity, push, lean, Vector2.zero, GroundSpring.MaxStep, spring);
        }

        StepStill(new[] { far }, 9f * GroundSpring.MaxStep);

        Color texel = At(Read(_ground.motion), new Vector2(0.3f, 0.2f));
        Assert.That(texel.r, Is.EqualTo(lean.x).Within(0.003f));
        Assert.That(texel.g, Is.EqualTo(lean.y).Within(0.003f));
        Assert.That(texel.b, Is.EqualTo(velocity.x).Within(0.01f));
        Assert.That(texel.a, Is.EqualTo(velocity.y).Within(0.01f));
    }

    [Test]
    public void Step_OutwardPush_LeansAwayFromTheCentreAndFadesJustPastTheStamp()
    {
        GroundStamp stamp = GroundStamp.Disc(Vector2.zero, 0.8f, 0.6f, 0f, 0.3f, 0f);

        StepStill(new[] { stamp }, 0.4f);

        Color[] motion = Read(_ground.motion);
        Assert.Greater(At(motion, new Vector2(0.5f, 0f)).r, 0.2f);
        Assert.Less(At(motion, new Vector2(-0.5f, 0f)).r, -0.2f);
        Assert.Greater(At(motion, new Vector2(0f, 0.5f)).g, 0.2f);
        Assert.Greater(At(motion, new Vector2(0.9f, 0f)).r, 0.002f, "Neighbours soften the push's edge.");
        Assert.Less(Mathf.Abs(At(motion, new Vector2(1.6f, 0f)).r), 0.01f, "Grass away from the push stays still.");
    }

    [Test]
    public void Step_StampRemoved_TheGrassSwingsBackAndStandsUpSlowly()
    {
        GroundStamp stamp = GroundStamp.Disc(Vector2.zero, 1f, 0.5f, 1f, 0.2f, 0f);
        StepStill(new[] { stamp }, 0.5f);
        float pushed = At(Read(_ground.motion), new Vector2(0.5f, 0f)).r;

        StepStill(new GroundStamp[0], 0.3f);

        Assert.Less(At(Read(_ground.motion), new Vector2(0.5f, 0f)).r, pushed * 0.5f);
        Assert.Greater(At(Read(_ground.crush), new Vector2(0.5f, 0f)).r, 0.5f, "The wake lingers.");
    }

    [Test]
    public void Step_ShockRing_ThrowsTheGrassOutwardWithoutHoldingIt()
    {
        GroundStamp shock = GroundStamp.Shock(Vector2.zero, 1f, 0.3f, 1f, 120f);

        StepStill(new[] { shock }, 0.1f);

        Color thrown = At(Read(_ground.motion), new Vector2(1f, 0f));
        Assert.Greater(thrown.r, 0.1f, "Thrown outward.");
        Assert.Greater(thrown.b, 1f, "Still moving outward.");

        StepStill(new GroundStamp[0], 2.5f);

        Assert.Less(Mathf.Abs(At(Read(_ground.motion), new Vector2(1f, 0f)).r), 0.02f, "Nothing holds it there.");
    }

    [Test]
    public void Step_AshAura_BurnsTheStateUnderItOnly()
    {
        GroundStamp ash = GroundStamp.Aura(new Vector2(2f, 1f), 0.8f, 0.2f, 0f, 1f, 0f, 0f);

        StepStill(new[] { ash }, 2f);

        Color[] state = Read(_ground.state);
        Assert.Greater(At(state, new Vector2(2f, 1f)).r, 0.9f);
        Assert.Less(At(state, new Vector2(2f, -1f)).r, 0.01f, "Not mirrored across the rows.");
        Assert.Less(At(state, new Vector2(-2f, 1f)).r, 0.01f);
        Assert.AreEqual(0f, At(Read(_ground.motion), new Vector2(2f, 1f)).r, 1e-3f, "An aura moves nothing.");
    }

    [Test]
    public void Step_Paused_ChangesNothing()
    {
        StepStill(new[] { GroundStamp.Disc(Vector2.zero, 1f, 0.5f, 1f, 0.2f, 0f) }, 0.1f);
        Color before = At(Read(_ground.motion), new Vector2(0.5f, 0f));

        _ground.Step(0f, GroundWind.Shader(1f, 3f));

        Assert.AreEqual(before, At(Read(_ground.motion), new Vector2(0.5f, 0f)));
    }

    [Test]
    public void Reset_AfterAPush_StandsEveryTexelUpright()
    {
        StepStill(new[] { GroundStamp.Disc(Vector2.zero, 1f, 0.5f, 1f, 0.2f, 0f) }, 0.2f);

        _ground.Reset();

        Assert.AreEqual(Color.clear, At(Read(_ground.motion), new Vector2(0.5f, 0f)));
        Assert.AreEqual(0f, At(Read(_ground.crush), new Vector2(0.5f, 0f)).r);
        Assert.AreEqual(Color.clear, At(Read(_ground.state), new Vector2(0.5f, 0f)));
    }

    [Test]
    public void SetStamps_PastTheCapacity_KeepsTheFirstOnes()
    {
        _ground.SetStamps(new GroundStamp[GroundSimulation.StampCapacity + 5]);

        Assert.AreEqual(GroundSimulation.StampCapacity, _ground.stampCount);
    }

    [Test]
    public void Publish_ThenUnpublish_TogglesTheGlobalGround()
    {
        _ground.Publish();

        Assert.AreEqual(1f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
        Assert.AreSame(_ground.motion, Shader.GetGlobalTexture(GroundSimulation.MotionId));
        Assert.AreEqual(_volume.ShaderRect(), Shader.GetGlobalVector(GroundSimulation.RectId));

        GroundSimulation.Unpublish();

        Assert.AreEqual(0f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
    }

    [Test]
    public void Constructor_NoShader_LogsAndStaysInvalid()
    {
        TestHelpers.WithLoggingDisabled(() =>
        {
            GroundSimulation ground = new GroundSimulation(null, _volume, GroundSpringSettings.Default);
            Assert.IsFalse(ground.isValid);
            ground.Step(0.1f, Vector4.zero);
            ground.Dispose();
        });
    }
}

}

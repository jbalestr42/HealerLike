using HealerLike.Render.Look;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GrassGroundColourTests : AGrassDrawFixture
    {
        [Test]
        public void Show_GroundState_TurnsTheGrassToAshStrawAndGlow()
        {
            _scene.BuildKeyLight(20f, 4f);
            _scene.camera.orthographic = true;
            _scene.camera.orthographicSize = 0.65f;
            _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, -4f), Quaternion.identity);
            _material.enableInstancing = true;
            _material.EnableKeyword(instancedKeyword);
            _material.SetColor("_BaseColor", new Color(0.3f, 0.7f, 0.25f));
            _draw.Release();
            _draw = new GrassDraw(_mesh, _material, 1, new Bounds(Vector3.up * 0.5f, Vector3.one * 2f),
                LookTestScene.Layer);
            using (GraphicsBuffer seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftSeed.Stride))
            using (GraphicsBuffer states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftState.Stride))
            using (GraphicsBuffer visible = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4))
            {
                seeds.SetData(new[] { new TuftSeed { heightWidthLean = new Vector4(1f, 0.8f, 0f, 0f) } });
                states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 0f) } });
                visible.SetData(new uint[] { 0 });
                _draw.BindTufts(seeds, states, visible, 1f);
                _draw.Show(_scene.camera, () => true);
                Texture2D state = null;
                try
                {
                    GroundSimulation.Unpublish();
                    _scene.Render();
                    Color32 green = LookTestScene.MedianColour(_scene.texture, 128, 128, 2);

                    state = PublishState(new Vector4(1f, 0f, 0f, 0f));
                    _scene.Render();
                    Color32 ash = LookTestScene.MedianColour(_scene.texture, 128, 128, 2);
                    Object.DestroyImmediate(state);

                    state = PublishState(new Vector4(0f, -1f, 0f, 0f));
                    _scene.Render();
                    Color32 dead = LookTestScene.MedianColour(_scene.texture, 128, 128, 2);
                    Object.DestroyImmediate(state);

                    state = PublishState(new Vector4(0f, 0f, 1f, 0f));
                    _scene.Render();
                    Color32 glowing = LookTestScene.MedianColour(_scene.texture, 128, 128, 2);
                    Object.DestroyImmediate(state);

                    state = PublishState(new Vector4(0f, 0f, 0f, 1f));
                    _scene.Render();
                    Color32 blighted = LookTestScene.MedianColour(_scene.texture, 128, 128, 2);
                    Object.DestroyImmediate(state);

                    state = PublishState(new Vector4(0f, 0f, -1f, 0f));
                    _scene.Render();
                    Color32 frozen = LookTestScene.MedianColour(_scene.texture, 128, 160, 2);

                    Assert.Greater(green.g - green.r, 40, $"Green grass {green}");
                    Assert.Less(Mathf.Abs(ash.g - ash.r), 12, $"Ash is grey, not green: {ash}");
                    Assert.Greater(dead.r, dead.b + 30, $"Dead grass turns straw: {dead}");
                    Assert.Greater(dead.r, green.r + 30, $"Dead grass turns straw: {dead}");
                    Assert.Greater(glowing.r + glowing.g + glowing.b, green.r + green.g + green.b + 40,
                        $"A heal's glow lights the grass: {glowing}");
                    Assert.Greater(blighted.b, blighted.g, $"Blight turns it sickly violet: {blighted}");
                    Assert.Greater(frozen.b, green.b + 60, $"Frost whitens it: {frozen}");
                    Assert.Greater(frozen.r, green.r + 60, $"Frost whitens it: {frozen}");
                }
                finally
                {
                    _draw.Hide();
                    GroundSimulation.Unpublish();
                    if (state != null)
                    {
                        Object.DestroyImmediate(state);
                    }
                }
            }
        }

        [Test]
        public void Show_TipLight_GradesOrdinaryBladesButLeavesHostileSpikesUnchanged()
        {
            _scene.BuildKeyLight(20f, 4f);
            _scene.camera.orthographic = true;
            _scene.camera.orthographicSize = 0.65f;
            _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, -4f), Quaternion.identity);
            LookSettings settings = _scene.look.settings;
            settings.toonThreshold = 0f;
            settings.toonSoftness = 0.001f;
            settings.inkStrength = 0f;
            settings.contrast = 1f;
            _scene.look.settings = settings;
            _material.enableInstancing = true;
            _material.EnableKeyword(instancedKeyword);
            _material.SetColor("_BaseColor", new Color(0.4f, 0.6f, 0.3f));
            _draw.Release();
            _draw = new GrassDraw(_mesh, _material, 1, new Bounds(Vector3.up * 0.5f, Vector3.one * 2f),
                LookTestScene.Layer);
            using (GraphicsBuffer seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftSeed.Stride))
            using (GraphicsBuffer states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftState.Stride))
            using (GraphicsBuffer visible = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4))
            {
                seeds.SetData(new[] { new TuftSeed { heightWidthLean = new Vector4(1f, 0.8f, 0f, 0f) } });
                states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 0f) } });
                visible.SetData(new uint[] { 0 });
                _draw.BindTufts(seeds, states, visible, 1f);
                _draw.Show(_scene.camera, () => true);
                try
                {
                    _scene.Render();
                    Color32 lowBefore = LookTestScene.MedianColour(_scene.texture, 128, 79, 2);
                    Color32 highBefore = LookTestScene.MedianColour(_scene.texture, 128, 197, 2);
                    _material.SetFloat("_HLGrassTipLight", 0.4f);
                    _scene.Render();
                    Color32 lowAfter = LookTestScene.MedianColour(_scene.texture, 128, 79, 2);
                    Color32 highAfter = LookTestScene.MedianColour(_scene.texture, 128, 197, 2);
                    Assert.That(lowBefore.g - lowAfter.g, Is.GreaterThan(8), "The blade base should be darker");
                    Assert.That(highAfter.g - highBefore.g, Is.GreaterThan(4), "The blade tip should catch light");

                    states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 1f) } });
                    _scene.Render();
                    Color32[] spikeWithTipLight = _scene.texture.GetPixels32();
                    _material.SetFloat("_HLGrassTipLight", 0f);
                    _scene.Render();
                    CollectionAssert.AreEqual(spikeWithTipLight, _scene.texture.GetPixels32(),
                        "The hostile spike must not inherit the plant root-to-tip grade");
                }
                finally
                {
                    _draw.Hide();
                }
            }
        }
    }
}

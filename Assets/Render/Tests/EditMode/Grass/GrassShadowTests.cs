using HealerLike.Render.Look;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    public class GrassShadowTests : AGrassDrawFixture
    {
        [Test]
        public void Show_SpikeShadowPolicy_OrdinaryGrassReceivesLightWithoutCastingButSpikesStillCast()
        {
            _scene.BuildKeyLight(20f, 6f);
            _scene.camera.orthographic = true;
            _scene.camera.orthographicSize = 1.8f;
            _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 6f, 0f), Quaternion.Euler(90f, 0f, 0f));
            LookSettings settings = _scene.look.settings;
            settings.inkStrength = 0f;
            _scene.look.settings = settings;
            GameObject ground = _scene.Track(GameObject.CreatePrimitive(PrimitiveType.Plane));
            ground.layer = LookTestScene.Layer;
            ground.GetComponent<Renderer>().sharedMaterial = _scene.Track(new Material(_material));
            ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            _material.enableInstancing = true;
            _material.EnableKeyword(instancedKeyword);
            _draw.Release();
            _draw = new GrassDraw(_mesh, _material, 1, new Bounds(Vector3.up * 0.5f, Vector3.one * 3f),
                LookTestScene.Layer);
            _draw.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            using (GraphicsBuffer seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftSeed.Stride))
            using (GraphicsBuffer states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftState.Stride))
            using (GraphicsBuffer visible = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4))
            {
                seeds.SetData(new[] { new TuftSeed { positionYaw = new Vector4(0f, 0.01f, 0f, 0f),
                    heightWidthLean = new Vector4(1f, 1f, 0f, 0f) } });
                states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 0f) } });
                visible.SetData(new uint[] { 0 });
                _draw.BindTufts(seeds, states, visible, 1f);
                _draw.Show(_scene.camera, () => true);
                try
                {
                    _draw.properties.SetFloat("_HLGrassSpikeShadowsOnly", 0f);
                    _scene.Render();
                    int ordinaryShadow = ShadowPixels(_scene.texture);
                    _draw.properties.SetFloat("_HLGrassSpikeShadowsOnly", 1f);
                    _scene.Render();
                    int quietGround = ShadowPixels(_scene.texture);
                    states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 1f) } });
                    _scene.Render();
                    int spikeShadow = ShadowPixels(_scene.texture);
                    Color32[] spikeOnly = _scene.texture.GetPixels32();
                    _draw.properties.SetFloat("_HLGrassSpikeShadowsOnly", 0f);
                    _scene.Render();

                    Debug.Log("[GrassDrawTests] Ground shadow pixels: ordinary=" + ordinaryShadow
                        + " quiet=" + quietGround + " spike=" + spikeShadow);
                    Assert.That(ordinaryShadow, Is.GreaterThan(40), "The control blade must actually cast a shadow");
                    Assert.That(quietGround, Is.LessThan(ordinaryShadow / 10 + 5));
                    Assert.That(spikeShadow, Is.GreaterThan(6), "The hostile spike must keep a real cast shadow");
                    CollectionAssert.AreEqual(spikeOnly, _scene.texture.GetPixels32(),
                        "Filtering ordinary grass must not alter the spike's shadow");
                }
                finally
                {
                    _draw.Hide();
                }
            }
        }

        [Test]
        public void Show_FlatPatchFromShadedSideUnderKeyLight_ShowsItsOwnShade()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsComputeShaders
                || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                Assert.Ignore("Requires compute, indirect draws and graphics readback");
            }

            // The grass teal is too green to tell from the lit blades, so a copy paints its shade magenta
            Material marked = _scene.Track(new Material(AssetDatabase.LoadAssetAtPath<Material>(grassMaterialPath)));
            marked.SetColor("_HLShadeTint", new Color(1f, 0f, 1f, 1f));
            marked.SetColor("_HLShadeTurnTint", Color.clear);
            marked.SetColor("_HLHighlightTint", Color.clear);
            _scene.BuildKeyLight(20f, 20f);
            // Keep the production sun, but observe the shaded side of the patch. A camera beside the sun sees
            // mostly lit faces; that old fixture depended on ordinary blades casting onto one another.
            Vector3 toLight = StageKeyLight.KeyDirection;
            float yaw = Mathf.Atan2(toLight.x, toLight.z) * Mathf.Rad2Deg;
            _scene.camera.transform.rotation = Quaternion.Euler(25f, yaw, 0f);
            _scene.camera.transform.position = -_scene.camera.transform.forward * 20f;
            CreateGrassPatch(marked);

            _scene.Render();

            int shadePixels = 0;
            int litPixels = 0;
            foreach (Color32 pixel in _scene.texture.GetPixels32())
            {
                if (pixel.r > pixel.g + 30)
                {
                    shadePixels++;
                }
                if (pixel.g > pixel.r + 25 && pixel.g > pixel.b + 25)
                {
                    litPixels++;
                }
            }
            // This back-side view must expose both actual shade and untouched green. Their area ratio is an
            // artistic material choice: the plant threshold naturally shades more of these upright facets.
            // Require visible regions, rather than a fixed percentage or a single antialiased edge pixel.
            Assert.That(shadePixels, Is.GreaterThan(64), "The marked self-shade must cover a visible region");
            Assert.That(litPixels, Is.GreaterThan(64), "Lit grass must retain a visible green region");
            Debug.Log("[GrassDrawTests] Grass shade pixels=" + shadePixels + " lit pixels=" + litPixels);
        }

        [Test]
        public void Show_StoneShadowOnGrass_PaintsTheLitFacesWithTheGlobalCastBlue()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsComputeShaders
                || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                Assert.Ignore("Requires compute, indirect draws and graphics readback");
            }

            _scene.BuildKeyLight(20f, 20f);
            _scene.camera.transform.rotation = Quaternion.Euler(StageCalibration.PortraitPitch,
                StageCalibration.PortraitYaw, 0f);
            _scene.camera.transform.position = -_scene.camera.transform.forward * 20f;
            CreateGrassPatch(AssetDatabase.LoadAssetAtPath<Material>(grassMaterialPath));
            _scene.Render();
            Color32[] withoutStone = _scene.texture.GetPixels32();
            GameObject stone = CreateStone(AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat"));
            // Lift a broad caster clear of the taller grass. Its surface must not hide the grass colour probe.
            stone.transform.position = new Vector3(0.5f, 2.5f, 0.5f);
            stone.transform.localScale = Vector3.one * 2.4f;
            stone.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            // Where the stone's centre falls on the carpet, along the key light
            Vector3 toLight = StageKeyLight.KeyDirection.normalized;
            Vector3 shadowCentre = stone.transform.position - toLight * ((stone.transform.position.y - 0.2f) / toLight.y);

            _scene.Render();

            Vector3 viewport = _scene.camera.WorldToViewportPoint(shadowCentre);
            int x = Mathf.RoundToInt(viewport.x * 256f);
            int y = Mathf.RoundToInt(viewport.y * 256f);
            Assert.That(x, Is.InRange(4, 251), "The full grass probe must be inside the captured image.");
            Assert.That(y, Is.InRange(4, 251), "The full grass probe must be inside the captured image.");
            Color32[] withStone = _scene.texture.GetPixels32();
            Color32 study = new Color32(39, 91, 127, 255); // Shared cast blue after working-space contrast
            int changedToCastBlue = 0;
            // The plant surface keeps its own teal on faces already turned away from the sun. Locate actual
            // green-to-blue changes instead of taking a median that can fall on an unlit teal face.
            for (int dy = -20; dy <= 20; dy++)
            {
                for (int dx = -20; dx <= 20; dx++)
                {
                    int px = Mathf.Clamp(x + dx, 0, 255);
                    int py = Mathf.Clamp(y + dy, 0, 255);
                    int index = py * 256 + px;
                    Color32 before = withoutStone[index];
                    Color32 after = withStone[index];
                    bool castBlue = after.b - after.g > 25 && Mathf.Abs(after.r - study.r) <= 24
                        && Mathf.Abs(after.g - study.g) <= 24 && Mathf.Abs(after.b - study.b) <= 24;
                    if (castBlue && before.g - after.g > 35)
                    {
                        changedToCastBlue++;
                    }
                }
            }
            Assert.That(changedToCastBlue, Is.GreaterThan(20),
                "The real caster must replace visible lit grass with blue shadow");
            Debug.Log("[GrassDrawTests] Grass pixels changed from lit fill to cast blue: " + changedToCastBlue);
        }
    }
}

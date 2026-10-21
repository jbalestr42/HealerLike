using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Look
{
    public class LookBodyShaderTests
    {
        LookTestScene _scene;

        [SetUp]
        public void SetUp()
        {
            _scene = new LookTestScene();
            _scene.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _scene.Release();
        }

        ItemType Track<ItemType>(ItemType item) where ItemType : Object
        {
            return _scene.Track(item);
        }

        [Test]
        public void Render_BodyHighlight_IsLocalizedAndFollowsTheRealLight()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Requires graphics readback");
            }

            _scene.BuildKeyLight(20f, 4f);
            _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);
            LookSettings settings = _scene.look.settings;
            settings.inkStrength = 0f;
            _scene.look.settings = settings;
            Material body = Track(new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat")));
            GameObject sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            sphere.layer = LookTestScene.Layer;
            sphere.GetComponent<Renderer>().sharedMaterial = body;
            Color highlightTint = body.GetColor("_HLHighlightTint");
            float[] centres = new float[2];
            for (int side = 0; side < 2; side++)
            {
                Vector3 direction = Quaternion.Euler(0f, side == 0 ? 30f : -30f, 0f) * Vector3.back;
                RenderSettings.sun.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
                body.SetColor("_HLHighlightTint", Color.clear);
                _scene.Render();
                Color[] control = _scene.texture.GetPixels();
                body.SetColor("_HLHighlightTint", highlightTint);
                _scene.Render();
                Color[] highlighted = _scene.texture.GetPixels();
                int covered = 0;
                int changed = 0;
                float sumX = 0f;
                for (int i = 0; i < control.Length; i++)
                {
                    if (control[i].r < 0.99f || control[i].g < 0.99f || control[i].b < 0.99f)
                    {
                        covered++;
                    }
                    if (((Vector4)highlighted[i] - (Vector4)control[i]).magnitude < 0.04f)
                    {
                        continue;
                    }
                    changed++;
                    sumX += i % _scene.texture.width;
                    Assert.That(highlighted[i].r, Is.GreaterThan(control[i].r), "The highlight must brighten the lime face");
                }
                float share = changed / (float)covered;
                Assert.That(share, Is.InRange(0.004f, 0.12f), "A small glint must leave the broad cel face intact");
                centres[side] = sumX / changed;
                Debug.Log("[LookShaderTests] Body highlight side=" + side + " share=" + share.ToString("F4")
                    + " centreX=" + centres[side].ToString("F2"));
            }
            Assert.That(centres[1] - centres[0], Is.GreaterThan(20f),
                "The highlight must move across the curved surface when the actual directional light moves");
        }

        [Test]
        public void Render_BodyShadeTurn_StaysInShadowAndCastSuppressesHighlight()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Requires graphics readback");
            }

            MeshRenderer surface = LookShaderProbe.BuildLitQuad(_scene);
            Material body = Track(new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat")));
            surface.sharedMaterial = body;
            LookSettings settings = _scene.look.settings;
            settings.toonThreshold = LookSettings.Default.toonThreshold;
            settings.toonSoftness = LookSettings.Default.toonSoftness;
            _scene.look.settings = settings;
            Color turn = body.GetColor("_HLShadeTurnTint");
            Color highlight = body.GetColor("_HLHighlightTint");
            body.SetColor("_HLHighlightTint", Color.clear);
            float[] facing = { -0.8f, 0.25f, 0.65f };
            for (int i = 0; i < facing.Length; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, Mathf.Acos(facing[i]) * Mathf.Rad2Deg, 0f) * Vector3.back;
                RenderSettings.sun.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
                body.SetColor("_HLShadeTurnTint", Color.clear);
                _scene.Render();
                Color control = _scene.texture.GetPixel(128, 128);
                body.SetColor("_HLShadeTurnTint", turn);
                _scene.Render();
                Color changed = _scene.texture.GetPixel(128, 128);
                float difference = ((Vector4)changed - (Vector4)control).magnitude;
                if (i == 1)
                {
                    Assert.That(difference, Is.GreaterThan(0.05f), "The shade beside the terminator should turn toward teal");
                    Assert.That(changed.g, Is.GreaterThan(control.g));
                    Assert.That(changed.b, Is.LessThan(control.b));
                }
                else
                {
                    Assert.That(difference, Is.LessThan(0.008f), "Deep shade and bright fill must retain their endpoints");
                }
            }

            // Place a real shadow caster between the front-lit probe and sun. ShadowsOnly keeps the surface
            // visible to the camera while the production URP shadow map supplies the actual attenuation.
            RenderSettings.sun.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            body.SetColor("_HLHighlightTint", highlight);
            _scene.Render();
            Color lit = _scene.texture.GetPixel(128, 128);
            GameObject blocker = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            blocker.layer = LookTestScene.Layer;
            blocker.transform.position = new Vector3(0f, 0f, -0.5f);
            blocker.transform.localScale = new Vector3(0.6f, 0.6f, 0.2f);
            Renderer caster = blocker.GetComponent<Renderer>();
            caster.sharedMaterial = body;
            caster.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            _scene.Render();
            Color shadowWithHighlight = _scene.texture.GetPixel(128, 128);
            body.SetColor("_HLHighlightTint", Color.clear);
            _scene.Render();
            Color shadowControl = _scene.texture.GetPixel(128, 128);
            Assert.That(lit.g - shadowControl.g, Is.GreaterThan(0.3f), "The caster must actually darken the probe");
            Assert.That(((Vector4)shadowWithHighlight - (Vector4)shadowControl).magnitude, Is.LessThan(0.008f),
                "The glint must disappear in an actual received cast shadow");
        }
    }
}

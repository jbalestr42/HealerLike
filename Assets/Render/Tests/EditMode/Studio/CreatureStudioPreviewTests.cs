using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

using HealerLike.Render.Studio.Editor;

namespace HealerLike.Render.Studio
{
    public sealed class CreatureStudioPreviewTests
    {
        CreatureStudioPreview _preview;
        CreatureRecipe _recipe;

        [SetUp]
        public void SetUp()
        {
            _preview = new CreatureStudioPreview();
            _recipe = CreatureStudioAuthoring.BuildSample(0);
        }

        [TearDown]
        public void TearDown()
        {
            _preview?.Dispose();
            if (_recipe) Object.DestroyImmediate(_recipe);
        }

        [Test]
        public void BodyPreviewHasLitAndShadedPixelsEvenWhenEditorPipelineDisablesMainLight()
        {
            var original=QualitySettings.renderPipeline;
            var low=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Low_PipelineAsset.asset");
            Object.DestroyImmediate(_recipe);
            _recipe=ScriptableObject.CreateInstance<CreatureRecipe>();
            _recipe.roots.count=0;
            var palette=AssetDatabase.LoadAssetAtPath<LookPalette>("Assets/Render/Grammar/Data/LookPalette.asset");
            _recipe.parts=new[]{new CreaturePart { id="Body",parent=-1,primitive=Primitive.Sphere,role=PartRole.Body,dimensions=Vector3.one,colour=palette.plantBody }};
            _preview.ShowGround=false;
            _preview.Side=LookSide.Plant;
            Texture2D image=null;
            try
            {
                QualitySettings.renderPipeline=low;
                image=_preview.Capture(_recipe,0,256,256);
                int green=0,shade=0;
                for(int y=85;y<171;y++) for(int x=85;x<171;x++)
                {
                    Color colour=image.GetPixel(x,y);
                    if(colour.g>colour.r*1.15f && colour.g>colour.b*1.3f && colour.g>.25f) green++;
                    if(colour.b>colour.r*1.1f && colour.b>colour.g*1.04f && colour.g>.25f) shade++;
                }
                Assert.That(green,Is.GreaterThan(100),"Plant body lost its lit green surface; check the preview main-light pipeline.");
                Assert.That(shade,Is.GreaterThan(20),"Plant body lost the production body-material shade.");
                Assert.That(QualitySettings.renderPipeline,Is.SameAs(low),"Capture changed the editor's selected pipeline.");
            }
            finally { if(image) Object.DestroyImmediate(image); QualitySettings.renderPipeline=original; }
        }

        [Test]
        public void ExportAtDifferentAspectPreservesOrbitPanZoomAndCamera()
        {
            _preview.Sample(_recipe,.5f);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var type=typeof(CreatureStudioPreview);
            var configure=type.GetMethod("ConfigureCamera",flags);
            configure.Invoke(_preview,new object[]{400f,300f,true});
            var target=new Vector3(.4f,1.3f,-.2f);
            var orbit=new Vector2(24f,65f);
            type.GetField("_target",flags).SetValue(_preview,target);
            type.GetField("_distance",flags).SetValue(_preview,4.2f);
            type.GetField("_orbit",flags).SetValue(_preview,orbit);
            configure.Invoke(_preview,new object[]{400f,300f,true});
            var utility=(PreviewRenderUtility)type.GetField("_preview",flags).GetValue(_preview);
            Vector3 cameraPosition=utility.camera.transform.position;
            Quaternion cameraRotation=utility.camera.transform.rotation;
            float aspect=utility.camera.aspect;
            utility.camera.clearFlags=CameraClearFlags.Depth;
            Color background=new Color(.7f,.2f,.5f,.8f);
            utility.camera.backgroundColor=background;
            utility.camera.allowHDR=false;
            Texture2D image=null;
            try
            {
                image=_preview.Capture(_recipe,.5f,320,120);
                Assert.That(image,Is.Not.Null);
                Assert.That(type.GetField("_target",flags).GetValue(_preview),Is.EqualTo(target));
                Assert.That(type.GetField("_distance",flags).GetValue(_preview),Is.EqualTo(4.2f));
                Assert.That(type.GetField("_orbit",flags).GetValue(_preview),Is.EqualTo(orbit));
                Assert.That((bool)type.GetField("_fitRequested",flags).GetValue(_preview),Is.False);
                Assert.That(utility.camera.transform.position,Is.EqualTo(cameraPosition));
                Assert.That(Quaternion.Angle(utility.camera.transform.rotation,cameraRotation),Is.LessThan(.001f));
                Assert.That(utility.camera.aspect,Is.EqualTo(aspect));
                Assert.That(utility.camera.clearFlags,Is.EqualTo(CameraClearFlags.Depth));
                Assert.That(utility.camera.backgroundColor,Is.EqualTo(background));
                Assert.That(utility.camera.allowHDR,Is.False);
            }
            finally { if (image) Object.DestroyImmediate(image); }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void StarterRecipesBuildRealDetachedRigs(int index)
        {
            Object.DestroyImmediate(_recipe);
            _recipe = CreatureStudioAuthoring.BuildSample(index);
            _preview.Side = index == 2 ? LookSide.Stone : LookSide.Plant;
            string before = EditorJsonUtility.ToJson(_recipe);
            CreatureRig rig = _preview.Sample(_recipe, 1.25f);
            Assert.That(rig, Is.Not.Null, _preview.LastError);
            Assert.That(rig.recipe, Is.Not.SameAs(_recipe));
            Assert.That(rig.recipe.parts, Is.Not.SameAs(_recipe.parts));
            Assert.That(rig.partTransforms.Count, Is.EqualTo(_recipe.parts.Length));
            Assert.That(EditorSceneManager.IsPreviewScene(rig.root.gameObject.scene), Is.True);
            Assert.That(EditorJsonUtility.ToJson(_recipe), Is.EqualTo(before));
            foreach (Transform part in rig.partTransforms)
            {
                Assert.That(part.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(part.GetComponent<Renderer>().sharedMaterial.shader.name, Is.EqualTo("HL/Look/Primitive"));
            }
        }

        [TestCase(LookSide.Plant, 1)]
        [TestCase(LookSide.Stone, 2)]
        public void PreviewKeepsProductionBodyAndStoneMaterialProperties(LookSide side,int sample)
        {
            Object.DestroyImmediate(_recipe);
            _recipe=CreatureStudioAuthoring.BuildSample(sample);
            _preview.Side=side;
            var rig=_preview.Sample(_recipe,0);
            Assert.That(rig,Is.Not.Null,_preview.LastError);
            for (int i=0;i<_recipe.parts.Length;i++)
            {
                string filename=side==LookSide.Stone ? "Look_Stone" : _recipe.parts[i].role==PartRole.Body ? "Look_Body" : "Look_Default";
                var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/"+filename+".mat");
                var actual=rig.partTransforms[i].GetComponent<Renderer>().sharedMaterial;
                Assert.That(actual,Is.Not.SameAs(source));
                Assert.That(actual.GetFloat("_HLToonThresholdOffset"),Is.EqualTo(source.GetFloat("_HLToonThresholdOffset")));
                Assert.That(actual.GetColor("_HLShadeTint"),Is.EqualTo(source.GetColor("_HLShadeTint")));
                Assert.That(actual.GetFloat("_HLNormalEdges"),Is.EqualTo(source.GetFloat("_HLNormalEdges")));
            }
        }

        [Test]
        public void ReverseSeekAndDifferentFrameSchedulesMatchIncludingAimAndColour()
        {
            _preview.Aim = new Vector3(1.5f, .8f, .5f);
            _preview.Health = .62f;
            _preview.Charge = .7f;
            _preview.Glow = .8f;
            Snapshot[] expected = SnapshotRig(_preview.Sample(_recipe, 1.237f));
            _preview.Sample(_recipe, 4f);
            AssertSame(SnapshotRig(_preview.Sample(_recipe, 1.237f)), expected);
            _preview.Refresh();
            _preview.Sample(_recipe, .13f);
            _preview.Sample(_recipe, .59f);
            AssertSame(SnapshotRig(_preview.Sample(_recipe, 1.237f)), expected);
            using (var independent = new CreatureStudioPreview { Aim = _preview.Aim, Health = _preview.Health, Charge = _preview.Charge, Glow = _preview.Glow })
                AssertSame(SnapshotRig(independent.Sample(_recipe, 1.237f)), expected);
        }

        [Test]
        public void ReadoutChangesApplyWhilePausedAndSelectionBoundsTrackPart()
        {
            CreatureRig healthy = _preview.Sample(_recipe, .7f);
            Vector3 healthyPosition = healthy.partTransforms[1].position;
            _preview.Health = .2f;
            _preview.Charge = .9f;
            _preview.SelectedPart = 1;
            CreatureRig wilted = _preview.Sample(_recipe, .7f);
            Assert.That(Vector3.Distance(healthyPosition, wilted.partTransforms[1].position), Is.GreaterThan(.01f));
            GameObject box = wilted.root.parent.parent.Find("Selected Part Bounds").gameObject;
            Assert.That(box.activeSelf, Is.True);
            Assert.That(box.transform.childCount, Is.EqualTo(12));
            _preview.SelectedPart = -1;
            _preview.Sample(_recipe, .7f);
            Assert.That(box.activeSelf, Is.False);
        }

        [Test]
        public void InvalidRecipeReturnsDiagnosticAndClearsOldRig()
        {
            CreatureRig previous = _preview.Sample(_recipe, .1f);
            Transform oldRoot = previous.root;
            _recipe.parts[0].parent = 0;
            _preview.Refresh();
            Assert.That(_preview.Sample(_recipe, .1f), Is.Null);
            Assert.That(_preview.LastError, Is.Not.Empty);
            Assert.That(oldRoot == null, Is.True);
        }

        [Test]
        public void CleanupRestoresPreviewScenesAndLeavesSourceSceneUntouched()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            bool dirty = scene.isDirty;
            int scenes = EditorSceneManager.previewSceneCount;
            int materials = CountMaterials();
            string before = EditorJsonUtility.ToJson(_recipe);
            for (int i = 0; i < 3; i++)
            {
                using (var preview = new CreatureStudioPreview())
                {
                    preview.Sample(_recipe, .9f);
                    preview.Sample(_recipe, .2f);
                }
            }
            Assert.That(EditorSceneManager.previewSceneCount, Is.EqualTo(scenes));
            Assert.That(CountMaterials(), Is.EqualTo(materials));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(scene.isDirty, Is.EqualTo(dirty));
            CollectionAssert.AreEquivalent(roots, scene.GetRootGameObjects());
            Assert.That(EditorJsonUtility.ToJson(_recipe), Is.EqualTo(before));
        }

        [Test]
        public void CaptureKeepsCreatureVisibleInPortraitAndRestoresLookGlobals()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Image verification requires a graphics device.");
            float ink = Shader.GetGlobalFloat("_HLInkStrength"), applied = Shader.GetGlobalFloat("_HLLookApplied");
            Vector4 tint = Shader.GetGlobalVector("_HLShadowTint");
            _preview.ShowGround = false;
            Texture2D capture = null;
            try
            {
                capture = _preview.Capture(_recipe, .4f, 360, 520);
                Assert.That(capture.width, Is.EqualTo(360));
                Assert.That(capture.height, Is.EqualTo(520));
                Color32[] pixels = capture.GetPixels32();
                int green = pixels.Count(pixel => pixel.g > 65 && pixel.g > pixel.r * 1.15f && pixel.g > pixel.b * 1.2f);
                Assert.That(green, Is.GreaterThan(100), "The actual creature must retain its authored green colours.");
                Color32 background = pixels[pixels.Length - 1];
                int topSubject = 0;
                for (int y = 510; y < 520; y++)
                    for (int x = 80; x < 280; x++)
                    {
                        Color32 pixel = pixels[y * 360 + x];
                        if (System.Math.Abs(pixel.r - background.r) + System.Math.Abs(pixel.g - background.g) + System.Math.Abs(pixel.b - background.b) > 35) topSubject++;
                    }
                Assert.That(topSubject, Is.Zero, "The crown must fit inside the portrait frame.");
                Assert.That(Shader.GetGlobalFloat("_HLInkStrength"), Is.EqualTo(ink));
                Assert.That(Shader.GetGlobalFloat("_HLLookApplied"), Is.EqualTo(applied));
                Assert.That(Shader.GetGlobalVector("_HLShadowTint"), Is.EqualTo(tint));
                _preview.Dispose();
                Assert.That(capture != null, Is.True);
            }
            finally { if (capture) Object.DestroyImmediate(capture); }
        }

        [Test]
        public void NullRecipeClearsRigAndDisposalIsIdempotent()
        {
            Transform old = _preview.Sample(_recipe, 0f).root;
            Assert.That(_preview.Sample(null, 0f), Is.Null);
            Assert.That(old == null, Is.True);
            _preview.Dispose();
            Assert.DoesNotThrow(() => _preview.Dispose());
            Assert.Throws<ObjectDisposedException>(() => _preview.Sample(_recipe, 0f));
        }

        static int CountMaterials() => Resources.FindObjectsOfTypeAll<Material>().Count(material => material.name == "Creature Studio Preview Material");

        readonly struct Snapshot
        {
            public readonly Vector3 position, scale;
            public readonly Quaternion rotation;
            public readonly Color colour;
            public Snapshot(Transform part)
            {
                position = part.position;
                scale = part.localScale;
                rotation = part.rotation;
                var block = new MaterialPropertyBlock();
                part.GetComponent<Renderer>().GetPropertyBlock(block);
                colour = block.GetColor("_BaseColor");
            }
        }
        static Snapshot[] SnapshotRig(CreatureRig rig) => rig.partTransforms.Select(part => new Snapshot(part)).ToArray();
        static void AssertSame(Snapshot[] actual, Snapshot[] expected)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < actual.Length; i++)
            {
                Assert.That(Vector3.Distance(actual[i].position, expected[i].position), Is.LessThan(.0001f), "Position " + i);
                Assert.That(Vector3.Distance(actual[i].scale, expected[i].scale), Is.LessThan(.0001f), "Scale " + i);
                Assert.That(Quaternion.Angle(actual[i].rotation, expected[i].rotation), Is.LessThan(.02f), "Rotation " + i);
                Assert.That(actual[i].colour, Is.EqualTo(expected[i].colour), "Colour " + i);
            }
        }
    }
}

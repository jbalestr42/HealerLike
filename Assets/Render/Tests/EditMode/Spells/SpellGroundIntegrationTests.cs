using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Spells
{
    public class SpellGroundIntegrationTests
    {
        Ground _ground;
        GameObject _root;
        GameObject _target;

        [SetUp]
        public void SetUp()
        {
            _ground = new Ground();
            _root = new GameObject("SpellGroundTest");
            _target = new GameObject("SpellGroundTarget");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_target);
            _ground.Dispose();
        }

        static EffectRecipe Recipe(float light, float ash = 0f)
        {
            return new EffectRecipe
            {
                count = 1, colour = Color.white, cycleSeconds = 1f,
                entry = new ElementEntry
                {
                    parts = new[] { new LookPart { id = "Shape", primitive = Primitive.Sphere,
                        colour = ColourRole.Accent, size = Vector3.one } },
                    ground = new GroundEffect { light = light, ash = ash, hold = .2f, lifetime = .6f },
                    groundRadius = 1.3f, groundStrength = .6f
                }
            };
        }

        SpellEffect Create(EffectRecipe recipe)
        {
            SpellEffect effect = SpellEffect.Create(recipe, _root.transform, RenderTestAssets.LoadMeshes(),
                null, _target);
            Assert.IsNotNull(effect);
            effect.SetStatus(1, 0f, 5f);
            return effect;
        }

        [Test]
        public void Play_AuthoredImpactPublishesRadiusStrengthAndRecoversAfterLifetime()
        {
            EffectRecipe recipe = EffectComposer.Compose(RenderTestAssets.LoadEffectVocabulary(),
                EffectKey.Rise, EffectFamily.Heal, EffectTempo.Once, 0f, 1, 0f, .4f);
            Assert.IsNotNull(recipe.entry.ground);
            _target.transform.position = new Vector3(3f, 2f, 4f);
            SpellGround.Play(_ground, recipe, _target.transform.position, 2f, true);
            Assert.AreEqual(1, _ground.oneShotCount);
            Assert.IsTrue(_ground.Find(recipe.entry.ground, out Vector2 from, out _, out float radius,
                out float strength));
            Assert.AreEqual(new Vector2(3f, 4f), from);
            Assert.AreEqual(recipe.entry.groundRadius * 2f, radius);
            Assert.AreEqual(Mathf.Clamp01(recipe.entry.groundStrength * 1.35f), strength);
            _ground.Advance(.3f);
            Assert.Greater(GroundProbe.State(_ground, _target.transform.position).magnitude, 0f);
            _ground.Advance(recipe.entry.ground.lifetime + 1f);
            Assert.AreEqual(0, _ground.oneShotCount);
            Assert.AreEqual(Vector4.zero, GroundProbe.State(_ground, _target.transform.position));
        }

        [Test]
        public void BindGround_FollowsTargetAndRebindDoesNotLeakHandles()
        {
            SpellEffect effect = Create(Recipe(.7f));
            effect.BindGround(_ground, _target);
            Assert.AreEqual(1, _ground.heldCount);
            Assert.Greater(GroundProbe.State(_ground, Vector3.zero).z, 0f);
            _target.transform.position = Vector3.right * 5f;
            effect.Advance(.1f);
            Assert.AreEqual(Vector4.zero, GroundProbe.State(_ground, Vector3.zero));
            Assert.Greater(GroundProbe.State(_ground, _target.transform.position).z, 0f);
            effect.BindGround(_ground, _target);
            Assert.AreEqual(1, _ground.heldCount);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void Lifecycle_ReleasesRootAndDisabledCompositeLayers(int end)
        {
            EffectRecipe recipe = Recipe(.4f);
            recipe.additions = new[] { Recipe(0f, .8f) };
            SpellEffect effect = Create(recipe);
            effect.BindGround(_ground, _target);
            Assert.AreEqual(2, _ground.heldCount);
            Assert.Greater(GroundProbe.State(_ground, Vector3.zero).x, 0f);
            if (end == 0) effect.BeginRemoval();
            if (end == 1)
            {
                effect.gameObject.SetActive(false);
                // Non-ExecuteAlways behaviours created in EditMode never enter the native player lifecycle.
                TestHelpers.InvokePrivate(effect, "OnDisable");
                TestHelpers.InvokePrivate(effect, "OnDisable");
            }
            if (end == 2) SpellEffect.Dispose(effect.gameObject);
            if (end == 3)
            {
                TestHelpers.InvokePrivate(effect, "OnDestroy");
                Object.DestroyImmediate(effect.gameObject);
            }
            Assert.AreEqual(0, _ground.heldCount);
            Assert.AreEqual(Vector4.zero, GroundProbe.State(_ground, Vector3.zero));
        }

        [Test]
        public void Removal_StopsGroundInputAndExistingStateRecoversNaturally()
        {
            SpellEffect effect = Create(Recipe(.8f, .8f));
            effect.BindGround(_ground, _target);
            Vector4 state = Vector4.zero;
            for (int i = 0; i < 20; i++)
                state = GroundState.Step(state, GroundProbe.State(_ground, Vector3.zero), .1f,
                    GroundStateSettings.Default);
            Assert.Greater(state.x, .3f);
            Assert.Greater(state.z, .3f);
            effect.BeginRemoval();
            Assert.AreEqual(0, _ground.heldCount);
            for (int i = 0; i < 200; i++)
                state = GroundState.Step(state, GroundProbe.State(_ground, Vector3.zero), .1f,
                    GroundStateSettings.Default);
            Assert.Less(state.magnitude, .001f);
        }

        [Test]
        public void Play_CompositeLayersEachContributeTheirAuthoredReaction()
        {
            EffectRecipe recipe = Recipe(.7f);
            recipe.additions = new[] { Recipe(0f, .8f) };
            SpellGround.Play(_ground, recipe, Vector3.zero);
            Assert.AreEqual(2, _ground.oneShotCount);
            Vector4 state = GroundProbe.State(_ground, Vector3.zero);
            Assert.Greater(state.x, 0f);
            Assert.Greater(state.z, 0f);
        }
        [Test]
        public void Line_CompositeEmitsLineAndPointReactionsAtTheirIntendedLocations()
        {
            EffectRecipe recipe = Recipe(.7f);
            recipe.entry.ground.shape = GroundShape.Line;
            recipe.additions = new[] { Recipe(0f, .8f) };
            SpellGround.Line(_ground, recipe, Vector3.zero, Vector3.right * 4f);
            Assert.AreEqual(2, _ground.oneShotCount);
            Assert.Greater(GroundProbe.State(_ground, Vector3.right * 2f).z, 0f);
            Assert.AreEqual(0f, GroundProbe.State(_ground, Vector3.right * 2f).x);
            Assert.Greater(GroundProbe.State(_ground, Vector3.right * 4f).x, 0f);
        }

        [Test]
        public void Play_InvalidCompositePublishesNothing()
        {
            EffectRecipe recipe = Recipe(.7f);
            recipe.additions = new[] { recipe };
            SpellGround.Play(_ground, recipe, Vector3.zero);
            SpellGround.Line(_ground, recipe, Vector3.zero, Vector3.right);
            Assert.AreEqual(0, _ground.oneShotCount);
        }

        [Test]
        public void HeldCompositeLink_TracksEndpointsAndReleasesEveryLayer()
        {
            EffectRecipe recipe = Recipe(.7f);
            recipe.socket = recipe.entry.socket = EffectSocket.Link;
            recipe.entry.ground.shape = GroundShape.Line;
            EffectRecipe child = Recipe(0f, .8f);
            child.socket = child.entry.socket = EffectSocket.Link;
            child.entry.ground.shape = GroundShape.Line;
            recipe.additions = new[] { child };
            SpellEffect effect = Create(recipe);
            effect.SetEndpoints(Vector3.zero, Vector3.right * 4f, false);
            effect.BindGround(_ground, _target);
            Vector4 old = GroundProbe.State(_ground, Vector3.right * 2f);
            Assert.Greater(old.x, 0f);
            Assert.Greater(old.z, 0f);
            effect.SetEndpoints(Vector3.forward * 5f, Vector3.right * 4f + Vector3.forward * 5f, false);
            Assert.AreEqual(Vector4.zero, GroundProbe.State(_ground, Vector3.right * 2f));
            Vector4 moved = GroundProbe.State(_ground, Vector3.right * 2f + Vector3.forward * 5f);
            Assert.Greater(moved.x, 0f);
            Assert.Greater(moved.z, 0f);
            effect.BeginRemoval();
            Assert.AreEqual(0, _ground.heldCount);
        }

        [Test]
        public void Dispose_EditModeReleasesRootAndChildProceduralMeshes()
        {
            EffectRecipe recipe = Recipe(.7f);
            recipe.entry.parts[0].shape = ShapeProfile.Bulb();
            EffectRecipe child = Recipe(0f, .8f);
            child.entry.parts[0].shape = ShapeProfile.Leaf();
            recipe.additions = new[] { child };
            SpellEffect effect = Create(recipe);
            effect.BindGround(_ground, _target);
            MeshFilter[] filters = effect.GetComponentsInChildren<MeshFilter>(true);
            Assert.AreEqual(2, filters.Length);
            Mesh first = filters[0].sharedMesh;
            Mesh second = filters[1].sharedMesh;
            Assert.IsTrue(first && second);
            GameObject owner = effect.gameObject;
            SpellEffect.Dispose(owner);
            Assert.IsTrue(first == null, "Root procedural mesh is owned by the disposed effect.");
            Assert.IsTrue(second == null, "Disabled child procedural mesh must also be released.");
            Assert.AreEqual(0, _ground.heldCount);
            Assert.DoesNotThrow(() => SpellEffect.Dispose(owner));
            Assert.IsTrue(RenderTestAssets.LoadMeshes().sphere, "Borrowed primitive assets remain alive.");
        }

    }
}

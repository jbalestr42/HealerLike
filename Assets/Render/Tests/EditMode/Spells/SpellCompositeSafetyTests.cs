using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class SpellCompositeSafetyTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        GameObject Make(string name)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            return obj;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject obj in _objects) if (obj) Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        static EffectRecipe Recipe(EffectSocket socket, bool billboard = false)
        {
            return new EffectRecipe { element = EffectKey.Orbit, count = 1, cycleSeconds = 1f,
                colour = Color.white, motion = EffectMotionKind.Orbit, socket = socket,
                tempo = EffectTempo.ForDuration,
                entry = new ElementEntry { socket = socket, motion = EffectMotionKind.Orbit,
                    presentation = new EffectPresentation { billboard = billboard, avoidHead = false },
                    parts = new[] { new LookPart { id = "shape", primitive = Primitive.Sphere,
                        role = PartRole.Body, size = Vector3.one * .2f } } } };
        }

        SpellEffect Build(EffectRecipe recipe)
        {
            GameObject host = Make("Mixed socket composition");
            return SpellEffect.Create(recipe, host.transform, RenderTestAssets.LoadMeshes(),
                RenderTestAssets.LoadLookMaterial(), null);
        }

        [Test]
        public void LostLinkSource_RemovesOnlyItsLayerAndLeavesParentSafeToRefresh()
        {
            EffectRecipe recipe = Recipe(EffectSocket.Body);
            recipe.additions = new[] { Recipe(EffectSocket.Link) };
            SpellEffect effect = Build(recipe);
            GameObject source = Make("Temporary caster");
            effect.SetEndpoints(Vector3.zero, Vector3.right, false);
            effect.SetCastSource(source);
            Object.DestroyImmediate(source);
            effect.Advance(.1f);
            Assert.IsTrue(effect);
            Assert.AreEqual(1, effect.GetComponentsInChildren<SpellEffect>(true).Length);
            Assert.DoesNotThrow(() =>
            {
                effect.Advance(.1f);
                effect.RefreshCount(2, 3);
                effect.SetStatus(2, 1, 4);
                effect.SetSide(Entity.EntityType.Player);
                effect.ShowCritical();
                effect.SetCastSource(null);
                effect.SetEndpoints(Vector3.zero, Vector3.one, false);
                Assert.AreEqual(1f, effect.lifetime);
                effect.BeginRemoval();
                effect.Advance(1f);
                Assert.IsTrue(effect.removalComplete);
            });
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Billboard_IsPerLayerAndPreservesOtherSockets(bool parentBillboard, bool childBillboard)
        {
            EffectRecipe recipe = Recipe(EffectSocket.Body, parentBillboard);
            recipe.additions = new[] { Recipe(EffectSocket.Feet, childBillboard), Recipe(EffectSocket.AboveHead) };
            SpellEffect effect = Build(recipe);
            var anchors = new EffectAnchors { bodyCentre = Vector3.up, foot = Vector3.zero,
                neck = Vector3.up * 1.4f, headCentre = Vector3.up * 1.8f, bodyRadius = .4f, headRadius = .2f };
            EffectPlacement.Place(effect, effect.transform.parent, anchors);
            SpellEffect[] layers = effect.GetComponentsInChildren<SpellEffect>(true);
            Vector3[] positions = { layers[0].transform.position, layers[1].transform.position,
                layers[2].transform.position };
            Vector3[] scales = { layers[0].transform.lossyScale, layers[1].transform.lossyScale,
                layers[2].transform.lossyScale };
            Camera camera = Make("Composition camera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(43f, 28f, 0f);
            EffectPlacement.FaceCamera(effect, camera);
            for (int i = 0; i < layers.Length; i++)
            {
                Assert.That(Vector3.Distance(positions[i], layers[i].transform.position), Is.LessThan(.00001f));
                Assert.That(Vector3.Distance(scales[i], layers[i].transform.lossyScale), Is.LessThan(.00001f));
                Quaternion expected = layers[i].recipe.presentation.billboard
                    ? camera.transform.rotation : Quaternion.identity;
                Assert.That(Quaternion.Angle(expected, layers[i].transform.rotation), Is.LessThan(.001f));
            }
        }
    }
}

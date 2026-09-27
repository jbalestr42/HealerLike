using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class SpellEnvelopeTests
    {
        readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in objects) Object.DestroyImmediate(item);
            objects.Clear();
        }

        SpellEffect Build(EffectKey element)
        {
            EffectVocabulary vocabulary = RenderTestAssets.LoadEffectVocabulary();
            EffectRecipe recipe = EffectComposer.Compose(vocabulary, element, EffectFamily.Heal,
                EffectTempo.Once, 0, 3, 3, .5f);
            GameObject host = new GameObject("Envelope fixture");
            objects.Add(host);
            SpellEffect effect = host.AddComponent<SpellEffect>();
            effect.Init(recipe, RenderTestAssets.LoadMeshes(), null, LookSide.Plant);
            if (recipe.socket == EffectSocket.Link)
                effect.SetEndpoints(Vector3.left, Vector3.right, false);
            return effect;
        }

        [Test]
        public void GroundSocket_UsesFootHeightForCompositeAuras()
        {
            var anchors = new EffectAnchors { foot = new Vector3(2, .1f, 3),
                bodyCentre = new Vector3(2, .8f, 3) };
            Assert.AreEqual(anchors.foot, EffectPlacement.Socket(anchors, EffectSocket.Ground));
        }

        [Test]
        public void SetEndpoints_BeamInScaledCompositeStillConnectsBothWorldEndpoints()
        {
            SpellEffect effect = Build(EffectKey.Beam);
            effect.transform.localScale = new Vector3(.2f, .4f, .3f);
            effect.transform.rotation = Quaternion.Euler(10, 25, 0);
            Vector3 start = new Vector3(-2, .6f, .3f), end = new Vector3(1, .8f, -.2f);
            effect.SetEndpoints(start, end, false);
            Transform first = effect.stalks[0];
            Transform last = effect.stalks[effect.stalks.Count - 1];
            Assert.Less(Vector3.Distance(start, first.TransformPoint(Vector3.down * .5f)), .0001f);
            Assert.Less(Vector3.Distance(end, last.TransformPoint(Vector3.up * .5f)), .0001f);
        }

        [Test]
        public void FaceCamera_AuthoredDepthMovesBillboardInFrontWithoutChangingItsScale()
        {
            SpellEffect effect = Build(EffectKey.Burst);
            // Own the profile; authoring assets must remain unchanged by tests.
            effect.recipe.entry = EffectRecipeCopy.Entry(effect.recipe.entry);
            effect.recipe.presentation.cameraDepth = 1.2f;
            effect.transform.position = new Vector3(2, 1, 3);
            effect.transform.localScale = Vector3.one * .4f;
            GameObject cameraObject = new GameObject("Billboard camera");
            objects.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(35, 20, 0);
            Vector3 before = effect.transform.position;
            EffectPlacement.FaceCamera(effect, camera);
            Assert.Less(Vector3.Distance(before - camera.transform.forward * .48f,
                effect.transform.position), .00001f);
            Assert.Less(Quaternion.Angle(camera.transform.rotation, effect.transform.rotation), .001f);
            Assert.AreEqual(Vector3.one * .4f, effect.transform.localScale);
        }

        [Test]
        public void Advance_LinkPearlsHaveSamePeakSizeAcrossDifferentFrameSteps()
        {
            SpellEffect stepped = Build(EffectKey.Beam);
            SpellEffect direct = Build(EffectKey.Beam);
            float time = stepped.lifetime * .5f;
            for (int i = 0; i < 50; i++) stepped.Advance(time / 50f);
            direct.Advance(time);
            Assert.That(stepped.shapes[0].localScale.magnitude, Is.GreaterThan(.05f));
            Assert.That(Vector3.Distance(stepped.shapes[0].localScale, direct.shapes[0].localScale),
                Is.LessThan(.00001f));
        }

        [Test]
        public void Advance_CriticalHaloRecoversAfterEntranceAndResolvesWithImpact()
        {
            SpellEffect effect = Build(EffectKey.Burst);
            effect.ShowCritical();
            Transform halo = effect.rings[0];
            float authored = effect.recipe.entry.criticalRings[0].size.magnitude;
            effect.Advance(effect.lifetime * .4f);
            Assert.That(halo.localScale.magnitude, Is.EqualTo(authored).Within(.0001f));
            effect.Advance(effect.lifetime * .55f);
            Assert.That(halo.localScale.magnitude, Is.LessThan(authored * .2f));
            effect.Advance(effect.lifetime * .1f);
            Assert.That(halo.localScale.magnitude, Is.EqualTo(0).Within(.0001f));
        }

        [Test]
        public void BeginRemoval_StatusBeadsAndCasterRimFadeWithMainShapes()
        {
            SpellEffect effect = Build(EffectKey.Orbit);
            effect.SetStatus(3, 0, 8);
            effect.SetSide(Entity.EntityType.Player);
            effect.Advance(.5f);
            Transform bead = effect.transform.Find(effect.recipe.entry.stackBeads[0].id);
            Transform rim = effect.transform.Find(effect.recipe.entry.sideRim[0].id);
            float beadSize = bead.localScale.magnitude;
            float rimSize = rim.localScale.magnitude;
            effect.BeginRemoval();
            effect.Advance(effect.recipe.presentation.releaseSeconds * .8f);
            Assert.That(bead.localScale.magnitude, Is.LessThan(beadSize * .2f));
            Assert.That(rim.localScale.magnitude, Is.LessThan(rimSize * .2f));
        }
    }
}

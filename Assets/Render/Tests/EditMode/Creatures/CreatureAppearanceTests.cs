using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    public class CreatureAppearanceTests
    {
        [Test]
        public void Timeline_SupportsPrecedeBodyAndTipsAndEveryPieceSettlesExactly()
        {
            float support = CreatureAppearance.PartDelay(PartRole.Limb, 0f);
            float body = CreatureAppearance.PartDelay(PartRole.Body, 0.2f);
            float head = CreatureAppearance.PartDelay(PartRole.Head, 0.7f);
            float tip = CreatureAppearance.PartDelay(PartRole.Tip, 1f);
            Assert.That(support, Is.LessThan(body));
            Assert.That(body, Is.LessThan(head));
            Assert.That(head, Is.LessThan(tip));
            Assert.That(CreatureAppearance.Scale(0.2f, support), Is.GreaterThan(0.8f));
            Assert.That(CreatureAppearance.Scale(0.2f, head), Is.EqualTo(0.001f));
            Assert.That(CreatureAppearance.Scale(0.2f, tip), Is.EqualTo(0.001f));
            foreach (PartRole role in System.Enum.GetValues(typeof(PartRole)))
            {
                float delay = CreatureAppearance.PartDelay(role, 1f);
                for (float t = 0f; t < CreatureAppearance.Duration; t += 0.01f)
                    Assert.That(CreatureAppearance.Scale(t, delay), Is.InRange(0.001f, 1.06f));
                Assert.That(CreatureAppearance.Scale(CreatureAppearance.Duration, delay), Is.EqualTo(1f));
            }
        }

        [Test]
        public void Roots_GrowFromFootTowardHipAndStayWithinSupportWindow()
        {
            Assert.That(CreatureAppearance.RootDelay(0, 6, 3, 4),
                Is.LessThan(CreatureAppearance.RootDelay(0, 6, 0, 4)));
            Assert.That(CreatureAppearance.RootDelay(5, 6, 0, 4), Is.LessThan(0.14f));
            Assert.That(CreatureAppearance.RootDelay(0, 1, 0, 1), Is.Zero);
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Rig_AppearanceIsPoseRelativeWithoutCompoundingOrReplayingOnRecompose(LookSide side)
        {
            GameObject parent = new GameObject("Appearance fixture");
            CreatureRecipe recipe = LookComposer.Compose(RenderTestAssets.CreateChannels(side, HeadKind.Arch),
                RenderTestAssets.LoadLookVocabulary());
            recipe.idle = default;
            CreatureRig rig = RenderTestAssets.CreateRig(recipe, parent.transform, RenderTestAssets.LoadLookMaterial());
            FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);
            try
            {
                rig.Tick(0f, 0f, frame);
                Vector3[] authored = new Vector3[rig.partTransforms.Count];
                for (int i = 0; i < authored.Length; i++) authored[i] = rig.partTransforms[i].localScale;
                rig.BeginAppearance();
                rig.AdvanceAppearance(0.2f);
                rig.Tick(0f, 0f, frame);
                Vector3[] growing = new Vector3[authored.Length];
                for (int i = 0; i < growing.Length; i++) growing[i] = rig.partTransforms[i].localScale;
                for (int repetition = 0; repetition < 20; repetition++) rig.Tick(0f, 0f, frame);
                bool partial = false;
                for (int i = 0; i < growing.Length; i++)
                {
                    Assert.That(rig.partTransforms[i].localScale, Is.EqualTo(growing[i]));
                    Assert.That(rig.partTransforms[i].parent.localScale, Is.EqualTo(Vector3.one));
                    if (growing[i].sqrMagnitude < authored[i].sqrMagnitude * 0.5f) partial = true;
                }
                Assert.That(partial, Is.True);
                float elapsed = rig.appearanceElapsed;
                Assert.That(rig.Recompose(recipe, RenderTestAssets.LoadLookMaterial(),
                    RenderTestAssets.LoadLookMaterial(), RenderTestAssets.LoadMeshes()), Is.True);
                Assert.That(rig.appearanceElapsed, Is.EqualTo(elapsed));
                rig.CompleteAppearance();
                rig.Tick(0f, 0f, frame);
                for (int i = 0; i < authored.Length; i++)
                {
                    Assert.That(rig.partTransforms[i].localScale, Is.EqualTo(authored[i]));
                    Assert.That(rig.partTransforms[i].localPosition, Is.EqualTo(Vector3.zero));
                }
                // Lost mineral geometry and inactive parts stay lost when the presentation finishes.
                rig.partTransforms[0].gameObject.SetActive(false);
                rig.AdvanceAppearance(100f);
                rig.Tick(0f, 0f, frame);
                Assert.That(rig.partTransforms[0].gameObject.activeSelf, Is.False);
                Assert.That(rig.isAppearing, Is.False);
            }
            finally
            {
                rig.Dispose();
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Timeline_IgnoresInvalidClockAndProgressCannotRunBackwards()
        {
            GameObject parent = new GameObject("Appearance clock");
            CreatureRecipe recipe = RenderTestAssets.CreateRecipe();
            CreatureRig rig = RenderTestAssets.CreateRig(recipe, parent.transform, RenderTestAssets.LoadLookMaterial());
            try
            {
                rig.BeginAppearance();
                rig.AdvanceAppearance(0.4f);
                foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity }) rig.AdvanceAppearance(invalid);
                Assert.That(rig.appearanceElapsed, Is.EqualTo(0.4f));
                rig.AdvanceAppearance(10f);
                Assert.That(rig.appearanceElapsed, Is.EqualTo(CreatureAppearance.Duration));
            }
            finally
            {
                rig.Dispose();
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(parent);
            }
        }
    }
}

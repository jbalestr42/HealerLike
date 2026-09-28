using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureAttachmentLifetimeTests
    {
        readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object owned in _owned)
            {
                if (owned)
                {
                    Object.DestroyImmediate(owned);
                }
            }

            _owned.Clear();
        }

        GameObject Track(GameObject go)
        {
            _owned.Add(go);
            return go;
        }

        // A presentation dies with its source even when nothing disposes it: a leaked body used to keep drawing
        [Test]
        public void SourceDestroyed_ReleasesThePresentation()
        {
            GameObject source = new GameObject("Source");
            CreatureAttachment attachment = new CreatureAttachment(source.transform);
            GameObject presentation = Track(attachment.root.gameObject);

            Object.DestroyImmediate(source);

            Assert.That(presentation == null, "The presentation outlived its source.");
            Assert.That(attachment.root, Is.Null);
        }

        [Test]
        public void Attach_AnchorsTheSourceWithoutSavingIt()
        {
            GameObject source = Track(new GameObject("Source"));
            CreatureAttachment attachment = new CreatureAttachment(source.transform);
            Track(attachment.root.gameObject);

            CreatureAttachmentAnchor anchor = source.GetComponent<CreatureAttachmentAnchor>();

            Assert.That(anchor, Is.Not.Null);
            Assert.That(anchor.count, Is.EqualTo(1));
            Assert.That(anchor.hideFlags & HideFlags.DontSave, Is.EqualTo(HideFlags.DontSave));
            attachment.Dispose();
        }

        // A source that never woke gives its anchor no OnDestroy; the orphan sweep still releases the root
        [Test]
        public void ReleaseOrphans_SourceGone_ReleasesTheRoot()
        {
            GameObject source = new GameObject("Source");
            source.SetActive(false);
            CreatureAttachment attachment = new CreatureAttachment(source.transform);
            GameObject presentation = Track(attachment.root.gameObject);
            Object.DestroyImmediate(source);

            CreatureAttachment.ReleaseOrphans();

            Assert.That(presentation == null, "The orphan presentation was not released.");
        }

        [Test]
        public void ReleaseOrphans_SourceAlive_KeepsTheRoot()
        {
            GameObject source = Track(new GameObject("Source"));
            CreatureAttachment attachment = new CreatureAttachment(source.transform);
            GameObject presentation = Track(attachment.root.gameObject);

            CreatureAttachment.ReleaseOrphans();

            Assert.That(presentation != null);
            attachment.Dispose();
        }

        // Closing a scene does not destroy a DontSave root; the Editor sweep removes one left without a scene
        [Test]
        public void Sweep_RootLeftByAClosedScene_IsRemoved()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject presentation = Track(new GameObject(CreatureAttachment.RootName) { hideFlags = HideFlags.DontSave });
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assume.That(presentation != null, "Closing the scene destroyed the root: nothing is left to sweep.");

            Assert.That(CreatureAttachmentSweep.IsLeaked(presentation), Is.True);
            CreatureAttachmentSweep.Sweep();

            Assert.That(presentation == null, "The leaked presentation is still drawn.");
        }

        [Test]
        public void Sweep_LivePresentation_IsKept()
        {
            GameObject source = Track(new GameObject("Source"));
            CreatureAttachment attachment = new CreatureAttachment(source.transform);
            GameObject presentation = Track(attachment.root.gameObject);

            CreatureAttachmentSweep.Sweep();

            Assert.That(presentation != null);
            Assert.That(CreatureAttachmentSweep.IsLeaked(presentation), Is.False);
            attachment.Dispose();
        }
    }
}

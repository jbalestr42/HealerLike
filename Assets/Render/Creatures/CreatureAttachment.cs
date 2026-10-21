using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Creatures
{
    // Geometry follows its source but lives outside gameplay's descendant renderer scans.
    // The host owns this scene-local root and transfers its instantiated shadow into it.
    // The root is DontSave, so neither a scene unload nor closing a scene destroys it: it dies with its source. An
    // anchor on the source releases it when the source is destroyed, even when the host's own OnDestroy never runs
    // (an EditMode test, a host that never woke), and a scene unload sweeps any root whose source is gone. Without
    // that, a leaked body kept drawing at its last place in every later scene and play session, with a missing
    // material where its owner had destroyed it.
    public class CreatureAttachment : IDisposable
    {
        public static readonly string RootName = "CreaturePresentation";
        static readonly List<CreatureAttachment> live = new List<CreatureAttachment>();
        readonly Transform _source;
        bool _isVisible = true;
        public Transform root { get; private set; }

        public CreatureAttachment(Transform source)
        {
            _source = source;
            GameObject container = new GameObject(RootName);
            container.hideFlags = HideFlags.DontSave;
            container.layer = source.gameObject.layer;
            SceneManager.MoveGameObjectToScene(container, source.gameObject.scene);
            root = container.transform;
            CreatureAttachmentAnchor.On(source.gameObject).Add(this);
            Track(this);
            Sync();
        }

        // The roots still alive, for the sweeps and the tests
        public static int liveCount
        {
            get
            {
                live.RemoveAll(attachment => attachment.root == null);
                return live.Count;
            }
        }

        static void Track(CreatureAttachment attachment)
        {
            if (live.Count == 0)
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
            }

            live.Add(attachment);
        }

        static void OnSceneUnloaded(Scene scene)
        {
            ReleaseOrphans();
        }

        // Every root whose source is gone, returns how many were released
        public static int ReleaseOrphans()
        {
            int released = 0;
            foreach (CreatureAttachment attachment in live.ToArray())
            {
                if (attachment.root != null && !attachment._source)
                {
                    attachment.Dispose();
                    released++;
                }
            }

            live.RemoveAll(attachment => attachment.root == null);
            return released;
        }

        public void Sync()
        {
            ApplyVisibility();
            if (!root || !_source)
            {
                return;
            }

            root.SetPositionAndRotation(_source.position, _source.rotation);
            root.localScale = _source.lossyScale;
        }

        public void Take(Transform child)
        {
            Sync();
            child.SetParent(root, true);
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            if (!root)
            {
                return;
            }

            bool visible = _isVisible && _source && _source.gameObject.activeInHierarchy;
            if (root.gameObject.activeSelf != visible)
            {
                root.gameObject.SetActive(visible);
            }
        }

        public void Dispose()
        {
            Transform released = root;
            root = null;
            live.Remove(this);
            if (released)
            {
                released.gameObject.SetActive(false);
                RenderObjects.Release(released.gameObject);
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Creatures
{
    // Geometry follows its source but lives outside gameplay's descendant renderer scans.
    // The host owns this scene-local root and transfers its instantiated shadow into it.
    public class CreatureAttachment : IDisposable
    {
        readonly Transform _source;
        public Transform root { get; private set; }

        public CreatureAttachment(Transform source)
        {
            _source = source;
            GameObject container = new GameObject("CreaturePresentation");
            container.hideFlags = HideFlags.DontSave;
            container.layer = source.gameObject.layer;
            SceneManager.MoveGameObjectToScene(container, source.gameObject.scene);
            root = container.transform;
            Sync();
        }

        public void Sync()
        {
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
            if (root)
            {
                root.gameObject.SetActive(visible && _source && _source.gameObject.activeInHierarchy);
            }
        }

        public void Dispose()
        {
            Transform released = root;
            root = null;
            if (released)
            {
                released.gameObject.SetActive(false);
                RenderObjects.Release(released.gameObject);
            }
        }
    }
}

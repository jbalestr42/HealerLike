using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // Borrowed gameplay renderers, hidden while Render owns their presentation and restored on release
    public class HiddenRenderers
    {
        Renderer[] _renderers;
        bool[] _states;

        public void Capture(GameObject root)
        {
            _renderers = root.GetComponentsInChildren<Renderer>(true);
            _states = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _states[i] = _renderers[i].enabled;
            }

            Hide();
        }

        // Again every frame, the projectile's own code may switch one back on
        public void Hide()
        {
            if (_renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in _renderers)
            {
                if (renderer)
                {
                    renderer.enabled = false;
                }
            }
        }

        public void Restore()
        {
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i])
                    {
                        _renderers[i].enabled = _states[i];
                    }
                }
            }

            _renderers = null;
            _states = null;
        }
    }
}

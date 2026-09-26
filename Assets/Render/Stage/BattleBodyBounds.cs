using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Stage
{
    // Tracks visible combatants. Rig revisions invalidate renderer discovery after recomposition.
    public class BattleBodyBounds
    {
        class BodyView
        {
            readonly Transform _body;
            ARigHost _host;
            CreatureRig _rig;
            int _revision = -1;
            Renderer[] _renderers;

            public BodyView(Transform body)
            {
                _body = body;
            }

            public Bounds Read()
            {
                if (_host == null)
                {
                    _host = _body.GetComponentInChildren<ARigHost>();
                }
                CreatureRig rig = _host != null ? _host.rig : null;
                int revision = rig != null ? rig.revision : 0;
                if (_renderers == null || rig != _rig || revision != _revision)
                {
                    _renderers = _body.GetComponentsInChildren<Renderer>();
                    _rig = rig;
                    _revision = revision;
                }
                return BattleFocusBounds.Body(_body, _renderers);
            }
        }

        readonly Dictionary<Transform, BodyView> _bodies = new Dictionary<Transform, BodyView>();
        readonly HashSet<Transform> _live = new HashSet<Transform>();
        readonly List<Transform> _gone = new List<Transform>();

        public int count { get { return _live.Count; } }

        public bool TryRead(EntityManager manager, out Bounds bounds)
        {
            _live.Clear();
            if (manager != null && manager.entities != null)
            {
                Add(manager.GetEntities(Entity.EntityType.Player));
                Add(manager.GetEntities(Entity.EntityType.Computer));
            }

            bool hasBounds = false;
            bounds = default;
            foreach (Transform body in _live)
            {
                if (!_bodies.TryGetValue(body, out BodyView view))
                {
                    view = new BodyView(body);
                    _bodies.Add(body, view);
                }
                Bounds bodyBounds = view.Read();
                if (hasBounds)
                {
                    bounds.Encapsulate(bodyBounds);
                }
                else
                {
                    bounds = bodyBounds;
                    hasBounds = true;
                }
            }

            _gone.Clear();
            foreach (Transform body in _bodies.Keys)
            {
                if (body == null || !_live.Contains(body))
                {
                    _gone.Add(body);
                }
            }
            foreach (Transform body in _gone)
            {
                _bodies.Remove(body);
            }
            return hasBounds;
        }

        public void Clear()
        {
            _bodies.Clear();
            _live.Clear();
            _gone.Clear();
        }

        void Add(List<GameObject> entities)
        {
            foreach (GameObject entity in entities)
            {
                if (entity != null && entity.activeInHierarchy)
                {
                    _live.Add(entity.transform);
                }
            }
        }
    }
}

using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // A shot no view claims flies as its own tip fragment, looking along its travel, until it lands. The tip hangs
    // from its own object, since a projectile may be scaled unevenly.
    public class FreeShot : MonoBehaviour
    {
        // A travel shorter than this has no direction
        static readonly float stillSquared = 0.00000001f;

        Transform _holder;
        Projectile _projectile;
        GameObject _targetPoint;
        Material _material;
        Color _colour;
        float _size;
        Vector3 _lastPosition;
        bool _hasLanded;
        CharacterView _screenSource;
        CastSourceLease _source;
        Vector3 _logicalStart;
        Vector3 _previousLogicalPosition;
        float _flightDistance;
        float _travelled;

        public Vector3 visualPosition { get { return _frame.GetColumn(3); } }
        public bool fromScreen { get { return _screenSource; } }

        // Made in Init, a property block cannot be made while Unity constructs the component
        DeliveryTip _tip;
        public DeliveryTip tip { get { return _tip; } }

        Matrix4x4 _frame;
        public Matrix4x4 frame { get { return _frame; } }

        // False for a style without a tip, the thrown shard, which has nothing to show in its place
        public bool Init(Projectile projectile, DeliveryStyle style, DeliveryVocabulary vocabulary,
            PrimitiveMeshes meshes, CharacterView screenSource = null)
        {
            if (_tip == null)
            {
                _tip = new DeliveryTip();
            }

            _screenSource = screenSource;
            _source?.Dispose();
            _source = screenSource ? null : CastSourceLease.From(projectile.source);
            _tip.SetStyle(style, vocabulary, meshes);
            // A thrown creature uses its body shard; the invisible player has no shard to lend.
            if (_tip.partCount == 0 && _screenSource)
            {
                _tip.SetStyle(DeliveryStyle.Direct, vocabulary, meshes);
            }
            if (_tip.partCount == 0)
            {
                _source?.Dispose();
                _source = null;
                return false;
            }

            if (!_holder)
            {
                _holder = new GameObject("FreeShot").transform;
                _holder.gameObject.layer = gameObject.layer;
            }

            _projectile = projectile;
            _targetPoint = projectile.targetPoint;
            // The family's accent at the vocabulary's bullet size, a white bead at the default size without it
            _material = null;
            _colour = Color.white;
            _size = DeliveryVocabulary.DefaultBulletSize;
            if (vocabulary)
            {
                _material = vocabulary.material;
                _colour = vocabulary.ShotColour(projectile.onHitConsumers);
                _size = vocabulary.bulletSize;
            }

            _hasLanded = false;
            _logicalStart = transform.position;
            _previousLogicalPosition = _logicalStart;
            _travelled = 0f;
            _flightDistance = _targetPoint ? Vector3.Distance(_logicalStart, _targetPoint.transform.position) : 0f;
            _lastPosition = PresentationPosition();
            _frame = DeliveryTip.Frame(_lastPosition, StartTravel(), _size);
            return true;
        }

        // The first contact finishes the entry path; bounces continue from their real impact point.
        public void Contact()
        {
            _screenSource = null;
            _source?.Dispose();
            _source = null;
        }

        // The tip stops drawing where the shot lands, the projectile's own visual stays hidden
        public void Land()
        {
            _hasLanded = true;
            Hide();
        }

        void OnDisable()
        {
            _source?.Dispose();
            _source = null;
            Hide();
        }

        void OnDestroy()
        {
            _source?.Dispose();
            _source = null;
            if (_tip != null)
            {
                _tip.Release();
            }

            if (_holder)
            {
                RenderObjects.Release(_holder.gameObject);
            }
        }

        // After the projectile's own Update has moved it, so the tip is drawn where the shot is this frame
        void LateUpdate()
        {
            if (!_projectile || _projectile.ShouldDestroyProjectile())
            {
                Hide();
                return;
            }

            _travelled += Vector3.Distance(transform.position, _previousLogicalPosition);
            _previousLogicalPosition = transform.position;
            Vector3 position = PresentationPosition();
            Vector3 travel = position - _lastPosition;
            if (travel.sqrMagnitude < stillSquared)
            {
                travel = StartTravel();
            }

            _lastPosition = position;
            _frame = DeliveryTip.Frame(position, travel, _size);
            if (!_hasLanded)
            {
                _tip.Draw(_holder, _frame, _material, _colour, _colour);
            }
        }

        void Hide()
        {
            if (_tip != null)
            {
                _tip.Hide();
            }
        }

        Vector3 PresentationPosition()
        {
            Vector3 origin = default;
            bool resolved = _screenSource ? _screenSource.TryGetCastPoint(out origin)
                : _source != null && _source.TryGet(out origin);
            if (!resolved)
            {
                if (_source != null && _source.isExplicit)
                {
                    _hasLanded = true;
                    Hide();
                    return _lastPosition;
                }
                return transform.position;
            }
            float remaining = 1f - Mathf.Clamp01(_travelled / Mathf.Max(0.001f, _flightDistance));
            return transform.position + (origin - _logicalStart) * remaining;
        }

        // Before the first move the tip looks at its target
        Vector3 StartTravel()
        {
            if (_targetPoint)
            {
                return _targetPoint.transform.position - PresentationPosition();
            }
            return Vector3.forward;
        }
    }
}

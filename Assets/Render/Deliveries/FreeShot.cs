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

        readonly DeliveryTip _tip = new DeliveryTip();
        Transform _holder;
        Projectile _projectile;
        GameObject _targetPoint;
        Material _material;
        Color _colour;
        float _size;
        Vector3 _lastPosition;
        bool _hasLanded;

        public DeliveryTip tip { get { return _tip; } }

        Matrix4x4 _frame;
        public Matrix4x4 frame { get { return _frame; } }

        // False for a style without a tip, the thrown shard, which has nothing to show in its place
        public bool Init(Projectile projectile, DeliveryStyle style, DeliveryVocabulary vocabulary,
            PrimitiveMeshes meshes)
        {
            _tip.SetStyle(style, vocabulary, meshes);
            if (_tip.partCount == 0)
            {
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
            _lastPosition = transform.position;
            _frame = DeliveryTip.Frame(transform.position, StartTravel(), _size);
            return true;
        }

        // The tip stops drawing where the shot lands, the projectile's own visual stays hidden
        public void Land()
        {
            _hasLanded = true;
            _tip.Hide();
        }

        void OnDisable()
        {
            _tip.Hide();
        }

        void OnDestroy()
        {
            _tip.Release();
            if (_holder)
            {
                RenderObjects.Release(_holder.gameObject);
            }
        }

        void LateUpdate()
        {
            if (!_projectile || _projectile.ShouldDestroyProjectile())
            {
                _tip.Hide();
                return;
            }

            Vector3 travel = transform.position - _lastPosition;
            if (travel.sqrMagnitude < stillSquared)
            {
                travel = StartTravel();
            }

            _lastPosition = transform.position;
            _frame = DeliveryTip.Frame(transform.position, travel, _size);
            if (!_hasLanded)
            {
                _tip.Draw(_holder, _frame, _material, _colour, _colour);
            }
        }

        // Before the first move the tip looks at its target
        Vector3 StartTravel()
        {
            if (_targetPoint)
            {
                return _targetPoint.transform.position - transform.position;
            }
            return Vector3.forward;
        }
    }
}

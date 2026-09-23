using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Creatures
{
    [DefaultExecutionOrder(100)]
    public class HLProjectileVisualObserver : AProjectileBehaviour
    {
        static int _nextToken;

        [FormerlySerializedAs("presentation")]
        [SerializeField] HLGestureKind _presentation = HLGestureKind.Attack;
        [FormerlySerializedAs("deliveryStyle")]
        [SerializeField] HLDeliveryStyle _deliveryStyle = HLDeliveryStyle.Direct;
        [FormerlySerializedAs("preserveContactPath")]
        [SerializeField] bool _preserveContactPath;

        readonly List<HLProjectileContact> _contacts = new List<HLProjectileContact>();
        IHLDeliverySource _delivery;
        MonoBehaviour _deliveryComponent;
        Projectile _subscribed;
        HLCreatureBuilder _builder;
        HLCreatureRig _rig;
        Renderer[] _renderers;
        bool[] _rendererStates;
        int _token;
        int _contactFrame = -1;
        bool _isInitialized;

        public HLDeliveryStyle deliveryStyle { get { return _deliveryStyle; } }

        public IReadOnlyList<HLProjectileContact> contacts { get { return _contacts; } }

        public GameObject capturedTarget { get; private set; }

        public GameObject capturedTargetPoint { get; private set; }

        public int gestureToken { get { return _token; } }

        public override void Init(GameObject source)
        {
            Unbind();
            _contacts.Clear();
            _contactFrame = -1;
            _isInitialized = true;
            if (!projectile)
            {
                projectile = GetComponent<Projectile>();
            }

            if (!projectile)
            {
                return;
            }

            // Bind before any Start callback can apply synchronous chain hits
            _subscribed = projectile;
            _subscribed.OnHit.AddListener(OnProjectileHit);
            capturedTarget = projectile.target;
            capturedTargetPoint = projectile.targetPoint;
            Entity entity = source ? source.GetComponent<Entity>() : null;
            GameObject model = entity && entity.model ? entity.model.gameObject : source;
            if (++_nextToken == 0)
            {
                ++_nextToken;
            }

            if (model)
            {
                foreach (MonoBehaviour component in model.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (!(component is IHLDeliverySource candidate) || !component.isActiveAndEnabled)
                    {
                        continue;
                    }

                    HLDeliveryStyle style = _preserveContactPath ? HLDeliveryStyle.ChainSync : _deliveryStyle;
                    Vector3 end = projectile.transform.position;
                    if (capturedTargetPoint)
                    {
                        end = capturedTargetPoint.transform.position;
                    }

                    if (!candidate.BeginDelivery(_nextToken, style, projectile.transform, end))
                    {
                        continue;
                    }

                    _delivery = candidate;
                    _deliveryComponent = component;
                    _token = _nextToken;
                    _builder = component as HLCreatureBuilder;
                    _rig = _builder ? _builder.rig : null;
                    break;
                }
            }

            if (_token == 0)
            {
                return;
            }

            _renderers = GetComponentsInChildren<Renderer>(true);
            _rendererStates = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _rendererStates[i] = _renderers[i].enabled;
            }

            HideRenderers();
        }

        void OnEnable()
        {
            if (_isInitialized && projectile && projectile.source)
            {
                Init(projectile.source);
            }
        }

        void OnDisable()
        {
            Unbind();
        }

        void OnDestroy()
        {
            Unbind();
        }

        void LateUpdate()
        {
            if (_token == 0)
            {
                return;
            }

            if (!_subscribed || !_subscribed.source || !_deliveryComponent || !_deliveryComponent.isActiveAndEnabled
                || (_builder && _builder.rig != _rig))
            {
                EndLease();
                return;
            }

            HideRenderers();
            // Retarget listeners have all finished by now. Never replace ordered hit contacts
            // with the final target, which can already be null for an instant chain.
            if (_subscribed.ShouldDestroyProjectile())
            {
                EndLease();
                return;
            }

            bool isPathHeld = _preserveContactPath && _contacts.Count > 0;
            if (_subscribed.target && _contactFrame != Time.frameCount && !isPathHeld)
            {
                if (_delivery != null)
                {
                    _delivery.UpdateDelivery(_token, _subscribed.transform.position);
                }
            }
        }

        void HideRenderers()
        {
            if (_token == 0 || _renderers == null)
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

        void OnProjectileHit(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null || _token == 0)
            {
                return;
            }

            Vector3 point = transform.position;
            if (hit.target)
            {
                point = HLCreatureBuilder.TargetPosition(hit.target);
            }
            else if (_contacts.Count > 0)
            {
                point = _contacts[_contacts.Count - 1].position;
            }

            _contacts.Add(new HLProjectileContact(hit.target, point));
            _contactFrame = Time.frameCount;
            if (_preserveContactPath && _builder && _rig != null)
            {
                _rig.ContactDeliveryPath(_token, point, true);
            }
            else if (_delivery != null)
            {
                _delivery.ContactDelivery(_token, point, hit.target);
            }
        }

        void EndLease()
        {
            if (_token != 0 && _deliveryComponent && _delivery != null)
            {
                _delivery.EndDelivery(_token);
            }

            _token = 0;
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i])
                    {
                        _renderers[i].enabled = _rendererStates[i];
                    }
                }
            }

            _renderers = null;
            _rendererStates = null;
        }

        void Unbind()
        {
            if (_subscribed)
            {
                _subscribed.OnHit.RemoveListener(OnProjectileHit);
            }

            _subscribed = null;
            EndLease();
            _rig = null;
            _builder = null;
            _delivery = null;
            _deliveryComponent = null;
        }
    }
}

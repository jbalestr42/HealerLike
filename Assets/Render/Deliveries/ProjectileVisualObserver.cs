using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Deliveries
{
    // Signals begin, contact and end to the source's view, which follows the projectile itself
    public class ProjectileVisualObserver : AProjectileBehaviour
    {
        [FormerlySerializedAs("presentation")]
        [SerializeField] GestureKind _presentation = GestureKind.Attack;
        [FormerlySerializedAs("deliveryStyle")]
        [SerializeField] DeliveryStyle _deliveryStyle = DeliveryStyle.Direct;
        [FormerlySerializedAs("preserveContactPath")]
        [SerializeField] bool _preserveContactPath;

        readonly List<ProjectileContact> _contacts = new List<ProjectileContact>();
        readonly List<LianaArm> _arms = new List<LianaArm>();
        RenderManager _manager;
        DeliveryVocabulary _vocabulary;
        IDeliverySource _delivery;
        MonoBehaviour _deliveryComponent;
        Projectile _subscribed;
        CreatureBuilder _builder;
        CreatureRig _rig;
        Renderer[] _renderers;
        bool[] _rendererStates;
        List<AConsumerFactory> _consumers;
        int _token;
        int _lease;
        bool _isInitialized;

        public DeliveryStyle deliveryStyle { get { return _deliveryStyle; } }

        public IReadOnlyList<ProjectileContact> contacts { get { return _contacts; } }

        public GameObject capturedTarget { get; private set; }

        public GameObject capturedTargetPoint { get; private set; }

        public int gestureToken { get { return _token; } }

        // The arms the delivery took, their tips carry its family
        public IReadOnlyList<LianaArm> arms { get { return _arms; } }

        // The manager adds the observer to a spawned projectile and calls this before Projectile.Init
        public void Init(RenderManager manager, ProjectileLook look)
        {
            _manager = manager;
            if (look != null)
            {
                _deliveryStyle = look.style;
                _presentation = look.presentation;
                _preserveContactPath = look.preserveContactPath;
            }
        }

        public override void Init(GameObject source)
        {
            Unbind();
            _contacts.Clear();
            _isInitialized = true;
            if (!projectile)
            {
                projectile = GetComponent<Projectile>();
            }

            if (!projectile)
            {
                return;
            }

            if (!_vocabulary)
            {
                _vocabulary = DeliveryVocabulary.Load();
            }

            // Bind before any Start callback can apply synchronous chain hits
            _subscribed = projectile;
            _subscribed.OnHit.AddListener(OnProjectileHit);
            capturedTarget = projectile.target;
            capturedTargetPoint = projectile.targetPoint;
            _consumers = OnHitConsumers();
            Entity entity = source ? source.GetComponent<Entity>() : null;
            GameObject model = entity && entity.model ? entity.model.gameObject : source;
            int token = NextToken();

            if (model)
            {
                foreach (MonoBehaviour component in model.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (!(component is IDeliverySource candidate) || !component.isActiveAndEnabled)
                    {
                        continue;
                    }

                    DeliveryStyle style = _preserveContactPath ? DeliveryStyle.ChainSync : _deliveryStyle;
                    Vector3 end = projectile.transform.position;
                    if (capturedTargetPoint)
                    {
                        end = capturedTargetPoint.transform.position;
                    }

                    if (!candidate.BeginDelivery(token, style, projectile.transform, end))
                    {
                        continue;
                    }

                    _delivery = candidate;
                    _deliveryComponent = component;
                    _token = token;
                    _builder = component as CreatureBuilder;
                    _rig = _builder ? _builder.rig : null;
                    break;
                }
            }

            if (_token == 0)
            {
                return;
            }

            FindArms(true);
            TintArms();
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
            TintArms();
            // Retarget listeners have all finished by now. Never replace ordered hit contacts
            // with the final target, which can already be null for an instant chain.
            if (_subscribed.ShouldDestroyProjectile())
            {
                EndLease();
            }
        }

        // Zero means no delivery, a projectile the manager did not set up draws no gesture
        int NextToken()
        {
            return _manager ? _manager.NextDeliveryToken() : 0;
        }

        // What the projectile applies on hit decides the family its tip shows
        List<AConsumerFactory> OnHitConsumers()
        {
#if HEALERLIKE_SEAMS
            return projectile.onHitConsumers;
#else
            // TODO: read Projectile.onHitConsumers once it is public, until then the tip keeps its rest colour
            return null;
#endif
        }

        // The family of the first consumer, or no accent when the projectile carries none
        bool TryAccent(out Color accent)
        {
            accent = Color.clear;
            if (_consumers == null || !_vocabulary || !_vocabulary.palette)
            {
                return false;
            }

            foreach (AConsumerFactory consumer in _consumers)
            {
                if (consumer != null)
                {
                    accent = _vocabulary.palette.Accent(EffectDerivation.ConsumerFamily(consumer, 1f, false));
                    return true;
                }
            }

            return false;
        }

        // The rig begins one arm per delivery and its lease is the newest token among its arms. A chain
        // contact later branches new arms under the same lease, so a hit looks again.
        void FindArms(bool isNewLease)
        {
            _arms.Clear();
            Transform root = _rig != null ? _rig.root : null;
            if (!root && _deliveryComponent)
            {
                root = _deliveryComponent.transform;
            }

            if (!root)
            {
                return;
            }

            LianaArmView[] views = root.GetComponentsInChildren<LianaArmView>(true);
            if (isNewLease)
            {
                _lease = 0;
                foreach (LianaArmView view in views)
                {
                    if (view.arm != null && !view.arm.isAvailable && view.arm.token > _lease)
                    {
                        _lease = view.arm.token;
                    }
                }
            }

            if (_lease == 0)
            {
                return;
            }

            foreach (LianaArmView view in views)
            {
                if (view.arm != null && view.arm.token == _lease)
                {
                    _arms.Add(view.arm);
                }
            }
        }

        void TintArms()
        {
            if (!TryAccent(out Color accent))
            {
                return;
            }

            foreach (LianaArm arm in _arms)
            {
                arm.SetTipAccent(_lease, accent);
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
                point = CreatureBuilder.TargetPosition(hit.target);
            }
            else if (_contacts.Count > 0)
            {
                point = _contacts[_contacts.Count - 1].position;
            }

            _contacts.Add(new ProjectileContact(hit.target, point));
            if (_preserveContactPath && _builder && _rig != null)
            {
                _rig.ContactDeliveryPath(_token, point, true);
            }
            else if (_delivery != null)
            {
                _delivery.ContactDelivery(_token, point, hit.target);
            }

            FindArms(false);
            TintArms();
        }

        void EndLease()
        {
            if (_token != 0 && _deliveryComponent && _delivery != null)
            {
                _delivery.EndDelivery(_token);
            }

            _token = 0;
            _lease = 0;
            _arms.Clear();
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

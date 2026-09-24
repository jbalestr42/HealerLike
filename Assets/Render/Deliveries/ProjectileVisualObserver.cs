using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Deliveries
{
    // Signals begin, contact and end to the source's view, which follows the projectile itself.
    // A shot no view claims flies as its tip fragment instead of the projectile's own visual.
    public class ProjectileVisualObserver : AProjectileBehaviour
    {
        // A travel shorter than this has no direction
        static readonly float stillSquared = 0.00000001f;

        DeliveryStyle _deliveryStyle = DeliveryStyle.Direct;
        bool _preserveContactPath;

        readonly List<ProjectileContact> _contacts = new List<ProjectileContact>();
        readonly List<LianaArm> _arms = new List<LianaArm>();
        readonly DeliveryTip _freeTip = new DeliveryTip();
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
        Vector3 _lastPosition;
        bool _hasLanded;
        bool _isInitialized;

        public DeliveryStyle deliveryStyle { get { return _deliveryStyle; } }

        public IReadOnlyList<ProjectileContact> contacts { get { return _contacts; } }

        GameObject _capturedTarget;
        public GameObject capturedTarget { get { return _capturedTarget; } }

        GameObject _capturedTargetPoint;
        public GameObject capturedTargetPoint { get { return _capturedTargetPoint; } }

        public int gestureToken { get { return _token; } }

        // True while the shot is unclaimed and draws its own tip
        bool _isFree;
        public bool isFree { get { return _isFree; } }

        public DeliveryTip freeTip { get { return _freeTip; } }

        Matrix4x4 _freeTipFrame;
        public Matrix4x4 freeTipFrame { get { return _freeTipFrame; } }

        // The arms the delivery took, their tips carry its family
        public IReadOnlyList<LianaArm> arms { get { return _arms; } }

        // The manager adds the observer to a spawned projectile and calls this before Projectile.Init
        public void Init(RenderManager manager, ProjectileLook look)
        {
            _manager = manager;
            if (look != null)
            {
                _deliveryStyle = look.style;
                _preserveContactPath = look.preserveContactPath;
            }
        }

        public override void Init(GameObject source)
        {
            Unbind();
            _contacts.Clear();
            _hasLanded = false;
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
            _capturedTarget = projectile.target;
            _capturedTargetPoint = projectile.targetPoint;
            // What the projectile applies on hit decides the family its tip shows
            _consumers = projectile.onHitConsumers;
            // An item can grant the bounce, so the live behaviour decides and not the prefab
            if (GetComponent<BounceProjectileBehaviour>() && _deliveryStyle != DeliveryStyle.ChainSync
                && _deliveryStyle != DeliveryStyle.Thrown && !_preserveContactPath)
            {
                _deliveryStyle = DeliveryStyle.Bounce;
            }

            Entity entity = source ? source.GetComponent<Entity>() : null;
            GameObject model = entity && entity.model ? entity.model.gameObject : source;
            int token = NextToken();
            if (token == 0)
            {
                return;
            }

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

            if (_token != 0)
            {
                FindArms(true);
                TintArms();
                CaptureRenderers();
                return;
            }

            StartFree();
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
            if (_isFree)
            {
                UpdateFree();
                return;
            }

            if (_token == 0)
            {
                return;
            }

            if (!_subscribed || !_subscribed.source || !_deliveryComponent || !_deliveryComponent.isActiveAndEnabled
                || (_builder && _builder.rig != _rig))
            {
                // The view that claimed the shot is gone, the shot keeps flying as its own tip
                EndLease();
                StartFree();
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
                    accent = _vocabulary.palette.Accent(EffectDerivation.ConsumerFamily(consumer, false));
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

        void StartFree()
        {
            if (!_manager || !_subscribed || _hasLanded)
            {
                return;
            }

            DeliveryStyle style = _preserveContactPath ? DeliveryStyle.ChainSync : _deliveryStyle;
            PrimitiveMeshes meshes = null;
            if (_vocabulary)
            {
                meshes = _vocabulary.meshes;
            }
            _freeTip.SetStyle(style, _vocabulary, meshes);
            // A style without a tip, the thrown shard, has nothing to show in place of the projectile's own visual
            if (_freeTip.partCount == 0)
            {
                return;
            }

            _isFree = true;
            _lastPosition = transform.position;
            _freeTipFrame = DeliveryTip.Frame(transform.position, FreeTravel(), FreeSize());
            CaptureRenderers();
        }

        void UpdateFree()
        {
            if (!_subscribed || _subscribed.ShouldDestroyProjectile())
            {
                StopFree();
                return;
            }

            HideRenderers();
            Vector3 travel = transform.position - _lastPosition;
            if (travel.sqrMagnitude < stillSquared)
            {
                travel = FreeTravel();
            }

            _lastPosition = transform.position;
            _freeTipFrame = DeliveryTip.Frame(transform.position, travel, FreeSize());
            if (_hasLanded || !_vocabulary)
            {
                return;
            }

            _freeTip.Draw(_freeTipFrame, _vocabulary.material, FreeColour(), FreeColour(), gameObject.layer);
        }

        void StopFree()
        {
            _isFree = false;
            RestoreRenderers();
        }

        // Before the first move the tip looks at its target
        Vector3 FreeTravel()
        {
            if (capturedTargetPoint)
            {
                return capturedTargetPoint.transform.position - transform.position;
            }
            return Vector3.forward;
        }

        float FreeSize()
        {
            return _vocabulary ? _vocabulary.bulletSize : DeliveryVocabulary.DefaultBulletSize;
        }

        // The family's accent, or the damage accent for a shot that carries no consumer
        Color FreeColour()
        {
            if (TryAccent(out Color accent))
            {
                return accent;
            }

            if (_vocabulary && _vocabulary.palette)
            {
                return _vocabulary.palette.Accent(EffectFamily.Damage);
            }
            return Color.white;
        }

        void CaptureRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _rendererStates = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _rendererStates[i] = _renderers[i].enabled;
            }

            HideRenderers();
        }

        void HideRenderers()
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

        void RestoreRenderers()
        {
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

        void OnProjectileHit(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null || (_token == 0 && !_isFree))
            {
                return;
            }

            Vector3 point = transform.position;
            if (hit.target)
            {
                point = RenderTargets.Point(hit.target);
            }
            else if (_contacts.Count > 0)
            {
                point = _contacts[_contacts.Count - 1].position;
            }

            _contacts.Add(new ProjectileContact(hit.target, point));
            DropSplash(point);
            if (_isFree)
            {
                // A bounce keeps the tip flying toward its next target
                _hasLanded = !GetComponent<BounceProjectileBehaviour>();
                return;
            }

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

        // An area item shows as one pod falling from the tip at the first contact
        void DropSplash(Vector3 point)
        {
            if (_contacts.Count != 1 || !GetComponent<AreaOfEffectProjectileBehaviour>() || !_vocabulary
                || !_vocabulary.meshes || !_vocabulary.material)
            {
                return;
            }

            Color colour = FreeColour();
            float size = FreeSize();
            if (_arms.Count > 0)
            {
                colour = _arms[0].tipColour;
                size = _arms[0].tipWidth;
            }

            TipDrop drop = new GameObject("TipDrop").AddComponent<TipDrop>();
            drop.Init(_vocabulary.splashPod, _vocabulary.meshes.GetMesh(_vocabulary.splashPod.primitive),
                _vocabulary.material, colour, point, size);
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
            RestoreRenderers();
        }

        void Unbind()
        {
            if (_subscribed)
            {
                _subscribed.OnHit.RemoveListener(OnProjectileHit);
            }

            _subscribed = null;
            EndLease();
            StopFree();
            _rig = null;
            _builder = null;
            _delivery = null;
            _deliveryComponent = null;
        }
    }
}

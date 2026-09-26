using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Deliveries
{
    // Signals begin, contact and end to the source's view, which follows the projectile itself.
    // A shot no view claims flies as a FreeShot, its tip fragment in place of the projectile's own visual.
    // A chain also draws a thread between the successive targets it hits.
    public class ProjectileVisualObserver : AProjectileBehaviour
    {
        DeliveryStyle _deliveryStyle = DeliveryStyle.Direct;

        readonly List<ProjectileContact> _contacts = new List<ProjectileContact>();
        RenderManager _manager;
        Vector3 _boltEnd;
        bool _hasBolt;
        DeliveryVocabulary _vocabulary;
        readonly DeliveryClaim _claim = new DeliveryClaim();
        readonly HiddenRenderers _hidden = new HiddenRenderers();
        readonly ContactThread _thread = new ContactThread();
        Projectile _subscribed;
        List<AConsumerFactory> _consumers;
        FreeShot _freeShot;
        bool _hasLanded;

        public DeliveryStyle deliveryStyle { get { return _deliveryStyle; } }

        public IReadOnlyList<ProjectileContact> contacts { get { return _contacts; } }

        GameObject _capturedTargetPoint;
        public GameObject capturedTargetPoint { get { return _capturedTargetPoint; } }

        public int gestureToken { get { return _claim.token; } }

        // The manager adds the observer to a spawned projectile and calls this before Projectile.Init
        public void Init(RenderManager manager, DeliveryStyle style)
        {
            _manager = manager;
            _vocabulary = null;
            _thread.Init(null);
            if (manager)
            {
                _vocabulary = manager.deliveryVocabulary;
                _thread.Init(manager.spellSink);
            }

            _deliveryStyle = style;
        }

        public override void Init(GameObject source)
        {
            Unbind();
            _contacts.Clear();
            _hasLanded = false;
            _hasBolt = false;
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
            _capturedTargetPoint = projectile.targetPoint;
            // What the projectile applies on hit decides the family its tip shows
            _consumers = projectile.onHitConsumers;
            // An item can grant the bounce, so the live behaviour decides and not the prefab
            if (GetComponent<BounceProjectileBehaviour>() && _deliveryStyle != DeliveryStyle.ChainSync
                && _deliveryStyle != DeliveryStyle.Thrown)
            {
                _deliveryStyle = DeliveryStyle.Bounce;
            }

            // A projectile the manager did not set up takes no token and draws no gesture
            int token = NextToken();
            if (token == 0)
            {
                return;
            }

            Vector3 end = projectile.transform.position;
            if (_capturedTargetPoint)
            {
                end = _capturedTargetPoint.transform.position;
            }

            if (!CharacterView.ScreenSource(source)
                && _claim.TryClaim(Model(source), token, _deliveryStyle, projectile.transform, end))
            {
                TintTip();
                _hidden.Capture(gameObject);
                return;
            }

            StartFree();
        }

        void OnDisable()
        {
            Unbind();
        }

        void OnDestroy()
        {
            Unbind();
        }

        // LateUpdate and not the manager's tick: the projectile's own Update and its retarget listeners move and
        // retarget it first, and whether it is done can only be read after them
        void LateUpdate()
        {
            if (IsFree())
            {
                UpdateFree();
                return;
            }

            if (!_claim.isClaimed)
            {
                return;
            }

            if (!_subscribed || !_subscribed.source || _claim.isLost)
            {
                // The view that claimed the shot is gone, the shot keeps flying as its own tip
                EndLease();
                StartFree();
                return;
            }

            _hidden.Hide();
            TintTip();
            // Retarget listeners have all finished by now. Never replace ordered hit contacts
            // with the final target, which can already be null for an instant chain.
            if (_subscribed.ShouldDestroyProjectile())
            {
                EndLease();
            }
        }

        int NextToken()
        {
            if (!_manager)
            {
                return 0;
            }
            return _manager.NextDeliveryToken();
        }

        // The shooter's model carries its view, a source without an entity is its own model
        static GameObject Model(GameObject source)
        {
            Entity entity = null;
            if (source)
            {
                entity = source.GetComponent<Entity>();
            }

            if (entity && entity.model)
            {
                return entity.model.gameObject;
            }
            return source;
        }

        // The claimer tints the tip its delivery took, when it can and the shot carries a family
        void TintTip()
        {
            if (_vocabulary && _vocabulary.TryAccent(_consumers, out Color accent))
            {
                _claim.Tint(accent);
            }
        }

        // True while the shot is unclaimed and flies as its own tip
        bool IsFree()
        {
            return _freeShot && _freeShot.enabled;
        }

        void StartFree()
        {
            if (!_manager || !_subscribed || _hasLanded)
            {
                return;
            }

            if (!_freeShot)
            {
                _freeShot = gameObject.AddComponent<FreeShot>();
            }

            _freeShot.enabled = _freeShot.Init(_subscribed, _deliveryStyle, _vocabulary, _manager.meshes,
                CharacterView.ScreenSource(_subscribed.source));
            if (_freeShot.enabled)
            {
                _hidden.Capture(gameObject);
            }
        }

        // The free shot draws itself, the projectile's own visual stays hidden until it is done
        void UpdateFree()
        {
            if (!_subscribed || _subscribed.ShouldDestroyProjectile())
            {
                StopFree();
                return;
            }

            _hidden.Hide();
        }

        void StopFree()
        {
            if (_freeShot)
            {
                _freeShot.enabled = false;
            }

            _hidden.Restore();
        }

        void OnProjectileHit(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null)
            {
                return;
            }

            if (_subscribed is ChainLightningProjectile)
            {
                _thread.Contact(hit.target);
                Scorch(hit.target);
            }

            if (!_claim.isClaimed && !IsFree())
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
            // An area item shows as one pod falling from the tip at the first contact
            if (_contacts.Count == 1 && GetComponent<AreaOfEffectProjectileBehaviour>() && _manager)
            {
                TipDrop.Splash(_vocabulary, _manager.meshes, _consumers, point);
            }

            if (IsFree())
            {
                _freeShot.Contact();
                // A bounce keeps the tip flying toward its next target
                _hasLanded = !GetComponent<BounceProjectileBehaviour>();
                if (_hasLanded)
                {
                    _freeShot.Land();
                }
                return;
            }

            // A chain shot began as ChainSync, so its source keeps the path of its contacts
            _claim.Contact(point, hit.target);
            TintTip();
        }

        // Each bolt of a chain burns a jagged line into the grass, from the caster to its first target, then from
        // target to target
        void Scorch(GameObject target)
        {
            if (!_manager || !_manager.zones || !target)
            {
                return;
            }

            Vector3 to = target.transform.position;
            _manager.zones.AddScorch(_hasBolt ? _boltEnd : transform.position, to);
            _boltEnd = to;
            _hasBolt = true;
        }

        void EndLease()
        {
            _claim.Release();
            _hidden.Restore();
        }

        void Unbind()
        {
            if (_subscribed)
            {
                _subscribed.OnHit.RemoveListener(OnProjectileHit);
            }

            _subscribed = null;
            _thread.Clear();
            EndLease();
            StopFree();
        }
    }
}

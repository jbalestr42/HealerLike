using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // The spell owns presentation from spawn to contact. A creature only supplies its cast outlet.
    public class ProjectileVisualObserver : AProjectileBehaviour
    {
        readonly List<ProjectileContact> _contacts = new List<ProjectileContact>();
        readonly GroundLightningTrail _scorch = new GroundLightningTrail();
        readonly HiddenRenderers _hidden = new HiddenRenderers();
        RenderManager _manager;
        Projectile _subscribed;
        FreeShot _shot;
        DeliveryPathView _path;
        DeliveryChannels _channels;
        DeliveryStyle _style;
        GameObject _capturedTargetPoint;
        public DeliveryStyle deliveryStyle => _channels.style;
        public IReadOnlyList<ProjectileContact> contacts => _contacts;
        public GameObject capturedTargetPoint => _capturedTargetPoint;
        public DeliveryPathView path => _path;

        public void Init(RenderManager manager, DeliveryStyle style)
        {
            _manager = manager;
            _style = style;
            _channels.style = style;
        }

        public override void Init(GameObject source)
        {
            Unbind();
            _contacts.Clear();
            _scorch.Clear();
            if (!projectile)
            {
                projectile = GetComponent<Projectile>();
            }

            if (!projectile || !_manager)
            {
                return;
            }

            _channels = DeliveryDerivation.Read(projectile, _style);
            _subscribed = projectile;
            _subscribed.OnHit.AddListener(OnProjectileHit);
            _capturedTargetPoint = projectile.targetPoint;
            DeliveryVocabulary vocabulary = _manager.deliveryVocabulary;
            uint sequence = (uint)_manager.NextDeliveryToken();
            if (_channels.path != DeliveryPathKind.Projectile)
            {
                _path = DeliveryPathView.Create(source, _channels, vocabulary, gameObject.layer, sequence);
                if (_path)
                {
                    _hidden.Capture(gameObject);
                }
            }
            else
            {
                if (!_shot)
                {
                    _shot = gameObject.AddComponent<FreeShot>();
                }

                _shot.enabled = _shot.Init(projectile, _channels.style, vocabulary, _manager.meshes,
                    CharacterView.ScreenSource(source), sequence);
                if (_shot.enabled)
                {
                    _hidden.Capture(gameObject);
                }
            }
        }

        void LateUpdate()
        {
            if (!_subscribed || _subscribed.ShouldDestroyProjectile())
            {
                StopVisuals();
                return;
            }
            if (_path || _shot && _shot.enabled)
            {
                _hidden.Hide();
            }
        }

        void OnProjectileHit(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null || !_subscribed)
            {
                return;
            }

            Vector3 point = hit.target ? RenderTargets.Point(hit.target) : transform.position;
            if (!RenderMath.IsFinite(point))
            {
                return;
            }

            _contacts.Add(new ProjectileContact(hit.target, point));
            if (_path)
            {
                _path.Contact(hit.target, point);
                _scorch.Contact(_manager.ground, transform.position, hit.target);
            }
            if (_contacts.Count == 1 && _channels.splash)
            {
                TipDrop.Splash(_manager.deliveryVocabulary, _manager.meshes, _subscribed.onHitConsumers, point);
            }
            if (_shot && _shot.enabled)
            {
                _shot.Contact();
                if (!_channels.bouncing)
                {
                    _shot.Land();
                }
            }
        }

        void StopVisuals()
        {
            if (_shot)
            {
                _shot.Land();
                _shot.enabled = false;
            }
            if (_path)
            {
                if (Application.isPlaying)
                {
                    _path.Release();
                }
                else
                {
                    _path.Dispose();
                }

                _path = null;
            }
            _hidden.Restore();
        }

        void Unbind()
        {
            if (_subscribed)
            {
                _subscribed.OnHit.RemoveListener(OnProjectileHit);
            }

            _subscribed = null;
            StopVisuals();
        }

        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();
    }
}

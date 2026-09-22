using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public readonly struct HLProjectileContact
    {
        public readonly GameObject target;
        public readonly Vector3 position;
        public HLProjectileContact(GameObject target, Vector3 position) { this.target = target; this.position = position; }
    }
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class HLProjectileVisualObserver : AProjectileBehaviour
    {
        [SerializeField] HLGestureKind presentation = HLGestureKind.Attack;
        [SerializeField] HLDeliveryStyle deliveryStyle = HLDeliveryStyle.Direct;
        public HLDeliveryStyle DeliveryStyle => deliveryStyle;
        static int nextToken;
        IHLDeliverySource delivery;
        MonoBehaviour deliveryComponent;
        [SerializeField] bool preserveContactPath;
        readonly List<HLProjectileContact> contacts = new List<HLProjectileContact>();
        Projectile subscribed;
        HLCreatureBuilder builder;
        HLCreatureRig rig;
        Renderer[] renderers;
        bool[] rendererStates;
        int token, contactFrame = -1;
        bool initialized;
        public IReadOnlyList<HLProjectileContact> Contacts => contacts;
        public GameObject CapturedTarget { get; private set; }
        public GameObject CapturedTargetPoint { get; private set; }
        public int GestureToken => token;
        public override void Init(GameObject source)
        {
            Unbind(); contacts.Clear(); contactFrame = -1; initialized = true;
            if (!projectile) projectile = GetComponent<Projectile>();
            if (!projectile) return;
            // Bind before any Start callback can apply synchronous chain hits.
            subscribed = projectile; subscribed.OnHit.AddListener(OnProjectileHit);
            CapturedTarget = projectile.target; CapturedTargetPoint = projectile.targetPoint;
            var entity = source ? source.GetComponent<Entity>() : null;
            builder = entity && entity.model ? entity.model.GetComponent<HLCreatureBuilder>() : null;
            rig = builder && builder.isActiveAndEnabled ? builder.Rig : null;
            var model = entity && entity.model ? entity.model.gameObject : source;
            if (++nextToken == 0) ++nextToken;
            if (model) foreach (var component in model.GetComponentsInChildren<MonoBehaviour>())
            {
                if (!(component is IHLDeliverySource candidate) || !component.isActiveAndEnabled) continue;
                if (!candidate.BeginDelivery(nextToken, preserveContactPath ? HLDeliveryStyle.ChainSync : deliveryStyle,
                    projectile.transform, CapturedTargetPoint ? CapturedTargetPoint.transform.position : projectile.transform.position)) continue;
                delivery = candidate; deliveryComponent = component; token = nextToken; break;
            }
            if (token == 0) return;
            renderers = GetComponentsInChildren<Renderer>(true);
            rendererStates = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) rendererStates[i] = renderers[i].enabled;
            HideRenderers();
        }

        void HideRenderers()
        {
            if (token == 0 || renderers == null) return;
            foreach (var renderer in renderers) if (renderer) renderer.enabled = false;
        }
        void OnProjectileHit(OnHitData hit)
        {
            if (!isActiveAndEnabled || hit == null || token == 0) return;
            Vector3 point = hit.target ? HLCreatureBuilder.TargetPosition(hit.target)
                : contacts.Count > 0 ? contacts[contacts.Count - 1].position : transform.position;
            contacts.Add(new HLProjectileContact(hit.target, point)); contactFrame = Time.frameCount;
            if (preserveContactPath && builder && rig != null) rig.ContactDeliveryPath(token, point, true);
            else delivery?.ContactDelivery(token, point, hit.target);
        }
        void LateUpdate()
        {
            if (token == 0) return;
            if (!subscribed || !subscribed.source || !deliveryComponent || !deliveryComponent.isActiveAndEnabled || (builder && builder.Rig != rig))
            { EndLease(); return; }
            HideRenderers();
            // Retarget listeners have all finished by now. Never replace ordered hit contacts
            // with the final target, which can already be null for an instant chain.
            if (subscribed.ShouldDestroyProjectile()) { EndLease(); return; }
            if (subscribed.target && contactFrame != Time.frameCount && !(preserveContactPath && contacts.Count > 0))
                delivery?.UpdateDelivery(token, subscribed.transform.position);
        }
        void EndLease()
        {
            if (token != 0 && deliveryComponent) delivery?.EndDelivery(token);
            token = 0;
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++) if (renderers[i]) renderers[i].enabled = rendererStates[i];
            renderers = null; rendererStates = null;
        }
        void Unbind()
        {
            if (subscribed) subscribed.OnHit.RemoveListener(OnProjectileHit);
            subscribed = null; EndLease(); rig = null; builder = null; delivery = null; deliveryComponent = null;
        }
        void OnEnable() { if (initialized && projectile && projectile.source) Init(projectile.source); }
        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();
    }
}

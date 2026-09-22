using UnityEngine;
namespace HealerLike.Render.Spells
{
    /// <summary>HitArmor is an attribute, including instant grants that emit no buff-start event.</summary>
    [DisallowMultipleComponent]
    public sealed class HLAttributeShieldView : MonoBehaviour, IVisualBehaviour
    {
        AttributeManager attributes;
        Entity entity;
        Transform anchor;
        Material material;
        HLSpellEffect effect;
        float born;
        public HLSpellEffect Effect => effect;
        public void Init(Entity entity)
        {
            this.entity = entity;
            if (!entity) { Bind(null, null); return; }
            Bind(entity.attributeManager, entity.targetPoint ? entity.targetPoint.transform : entity.transform);
        }
        public void Bind(AttributeManager manager, Transform target, Material sharedMaterial = null)
        {
            if (attributes != manager || anchor != target) Clear();
            attributes = manager; anchor = target; material = sharedMaterial;
            Refresh();
        }
        public void Refresh()
        {
            if (entity && entity.targetPoint) anchor = entity.targetPoint.transform;
            float charges = attributes && attributes.Has(AttributeType.HitArmor) ? attributes.Get(AttributeType.HitArmor).Value : 0;
            if (!isActiveAndEnabled || !anchor || !(charges > 0) || float.IsInfinity(charges)) { Clear(); return; }
            if (!effect)
            {
                var go = new GameObject("HLObservedHitArmor"); go.transform.SetParent(anchor, false);
                effect = go.AddComponent<HLSpellEffect>(); effect.kind = HLSpellEffectKind.Shield;
                effect.material = material ? material : (HLRenderRegistry.Current?.SpellSink as HLSpellVisualSink)?.material;
                effect.Initialize(); born = Time.time;
                effect.SetStatus(1, 0, float.PositiveInfinity, HLClockKind.Simulation,
                    new HLSpellSignature { operation = HLOperation.Attribute, sign = HLSign.Positive, hasAttribute = true,
                        attribute = AttributeType.HitArmor, topology = HLTopology.Single, duration = HLDurationShape.Infinite, tempo = HLTempo.Continuous });
            }
            effect.SetStatus(1, Mathf.Max(0,Time.time-born), float.PositiveInfinity, HLClockKind.Simulation, effect.Signature);
            effect.SetShieldState(charges);
        }
        void LateUpdate() => Refresh();
        void OnDisable() => Clear();
        void OnDestroy() => Clear();
        void Clear()
        {
            if (!effect) return;
            effect.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(effect.gameObject);
            else { effect.ReleaseResources(); DestroyImmediate(effect.gameObject); }
            effect = null;
        }
    }
}

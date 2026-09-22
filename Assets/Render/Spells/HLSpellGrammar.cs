using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    /// <summary>Reads data fields only. Never constructs gameplay consumers, buffs, skills or validators.</summary>
    public sealed class HLSpellGrammar
    {
        public HLVisualRecipe Describe(ABuffHandlerFactory factory, GameObject source, GameObject target) =>
            DescribeData(factory, Context(source, source != null && source == target ? HLTopology.Self : HLTopology.Single));
        public HLVisualRecipe Describe(ASkill skill, Entity source) => DescribeData(Data(skill), Context(source ? source.gameObject : null, HLTopology.Single));
        public HLVisualRecipe Describe(ACharacterSkill skill, Character source) => DescribeData(Data(skill), Context(source ? source.gameObject : null, HLTopology.Single));
        public HLVisualRecipe Describe(Projectile projectile) => DescribeData(projectile, Context(projectile ? projectile.source : null, HLTopology.Single));

        static HLGrammarContext Context(GameObject source, HLTopology topology)
        {
            var attributes = source ? source.GetComponent<AttributeManager>() : null;
            var entity = source ? source.GetComponent<Entity>() : null;
            return new HLGrammarContext { Topology = topology, Side = entity ? entity.entityType : Entity.EntityType.None,
                SourceAttribute = t => attributes != null && attributes.Has(t) ? attributes.Get(t).Value : null,
                SourceHealth = entity && entity.health ? entity.health.Value : null,
                SourceMaxHealth = entity && entity.health ? entity.health.Max : null,
                SelectionData = entity && entity.data != null ? Snapshot(new object[] { entity.data.targetBehaviourType, entity.data.targetValidators }) : "" };
        }
        // Only the public generic data field is inspected; no polymorphic gameplay getters execute.
        static object Data(object value) => value?.GetType().GetField("data")?.GetValue(value);
        public HLVisualRecipe DescribeData(object data, HLGrammarContext context = null) =>
            Compile(data, context ?? new HLGrammarContext(), HLDurationShape.Instant, HLTempo.Immediate, 0, 0, 0);

        HLVisualRecipe Compile(object input, HLGrammarContext c, HLDurationShape duration, HLTempo tempo, float seconds, float period, int depth)
        {
            if (depth > 32) return Unknown("Cyclic or excessively deep data graph.");
            if (input == null || input is UnityEngine.Object u && !u) return Unknown("Missing data.");
            if (input is ConsumerFactory consumer) input = consumer.data;
            if (input is BuffHandlerFactory handler) input = handler.data;
            if (input is ConsumerData cd) return Consumer(cd, c, duration, tempo, seconds, period);
            if (input is BuffHandlerBaseData hd)
            {
                var shape = hd.durationType == DurationType.Instant ? HLDurationShape.Instant : hd.durationType == DurationType.Infinite ? HLDurationShape.Infinite : HLDurationShape.Timed;
                var beat = hd.isPeriodic && shape != HLDurationShape.Instant ? HLTempo.HandlerTick : HLTempo.Continuous;
                var children = new List<HLVisualRecipe>();
                if (hd.buffFactoryList != null) foreach (var f in hd.buffFactoryList) children.Add(Compile(f, c, shape, beat, hd.duration, hd.periodDuration, depth + 1));
                return Bundle(children, c, shape, beat, hd.duration, hd.periodDuration,
                    hd.buffFactoryList == null ? "Missing buff list." : (shape == HLDurationShape.Timed && (!Finite(hd.duration) || hd.duration <= 0) || beat == HLTempo.HandlerTick && (!Finite(hd.periodDuration) || hd.periodDuration <= 0)) ? "Invalid handler clock." : null);
            }
            if (input is ABuffFactory bf) return Compile(Data(bf), c, duration, tempo, seconds, period, depth + 1);
            if (input is BaseData md)
            {
                float value = md is FlatModifierData flat ? flat.value : md is UpgradeModifierData upgrade ? upgrade.value : 0;
                HLExpression expression = md is HPBasedModifierData ? HLExpression.RecipientHealth : md is CurrentWaveModifierData ? HLExpression.Round : md is SlowModifierData || md is TimeModifierData ? HLExpression.Decay : HLExpression.Flat;
                var law = md is FlatModifierData ? HLStackLaw.FlatPowers : md is UpgradeModifierData ? HLStackLaw.Linear : md is SlowModifierData ? HLStackLaw.Logarithmic : md is TimeModifierData ? HLStackLaw.Refresh : HLStackLaw.None;
                HLSign sign = expression != HLExpression.Flat || md.modifierType == AttributeModifierType.Override ? HLSign.Conditional : Sign(value);
                if (md.modifierType == AttributeModifierType.Multiply && duration == HLDurationShape.Instant) sign = Sign(value - 1);
                if (md.type == AttributeType.FlatArmor || md.type == AttributeType.Vulnerability) sign = Reverse(sign);
                if (md.type == AttributeType.AttackRate || md.type == AttributeType.Speed || md.type == AttributeType.PercentArmor) sign = HLSign.Conditional;
                return new HLVisualRecipe(Key(HLOperation.Attribute, sign, md.type, c.Topology, duration, tempo), expression,
                    scalar: value, modifier: md.modifierType, stackLaw: law, duration: seconds, period: period, side: c.Side,
                    deliveryData: Snapshot(md), diagnostic: md is SlowModifierData || md is TimeModifierData ? "Gameplay modifier constructor reads an unassigned handler." : null);
            }
            if (input is ApplyConsumerBuffData ac) return Compile(ac.consumerFactory, c, duration, tempo, seconds, period, depth + 1);
            if (input is InvincibilityBuffData) return Atom(HLOperation.Prevention, c, duration, tempo, seconds, period);
            if (input is MultipleShootBuffData multi) return new HLVisualRecipe(Key(HLOperation.TargetCount, Sign(multi.value), null, c.Topology, duration, tempo), scalar: multi.value, stackLaw: HLStackLaw.Linear, duration: seconds, period: period);
            if (input is AddSkillBuffData install) return Bundle(new[] { Compile(install.skillFactory, c, duration, tempo, seconds, period, depth + 1) }, c, duration, tempo, seconds, period, operation: HLOperation.InstallSkill);
            if (input is ProjectileBehaviourBuffData pb) return Bundle(new[] { Compile(pb.projectileBehaviour, c, duration, tempo, seconds, period, depth + 1) }, c, duration, tempo, seconds, period, operation: HLOperation.InstallBehaviour);
            if (input is ManaOnRoundEndBuffData mana) return ConsumerFactoryRecipe(mana.consumerFactory, c, AttributeType.ManaMax, HLTopology.Self, HLTempo.RoundEnd, duration, seconds);
            if (input is HealAllEntitiesOnRoundEndBuffData heal) return ConsumerFactoryRecipe(heal.consumerFactory, c, AttributeType.HealthMax, HLTopology.Group, HLTempo.RoundEnd, duration, seconds);
            if (input is DamageAllEntityOnEntityDieBuffData death) return ConsumerFactoryRecipe(death.damageToAllEntity, c, AttributeType.HealthMax, HLTopology.Group, HLTempo.Death, duration, seconds);
            if (input is ASkillFactory || input is ACharacterSkillFactory || input is ASkillStepFactory || input is AProjectileBehaviourFactory)
                return Compile(Data(input), c, duration, tempo, seconds, period, depth + 1);
            if (input is ResourceValidatorData cost) return ConsumerFactoryRecipe(cost.consumer, c, AttributeType.ManaMax, HLTopology.Self, HLTempo.Immediate, HLDurationShape.Instant, 0);
            if (input is ApplyConsumerCharacterSkillData cs)
            {
                var cc = Copy(c, cs.isSingle ? HLTopology.Single : HLTopology.Group);
                var payload = cs.consumer is ConsumerFactory cf ? Consumer(cf.data, cc, HLDurationShape.Instant, HLTempo.Immediate, 0, 0, cs.multiplier) : Unknown("Unknown consumer type.");
                var children = new List<HLVisualRecipe> { payload };
                AddCosts(cs, c, children);
                return Bundle(children, cc, HLDurationShape.Instant, HLTempo.Immediate, 0, 0, delivery: Snapshot(cs), clock: HLClockKind.Realtime);
            }
            if (input is BuffCharacterSkillData bs)
            {
                var cc = Copy(c, bs.isSingle ? HLTopology.Single : HLTopology.Group);
                var list = new List<HLVisualRecipe>();
                if (bs.buffHandlerFactory != null) foreach (var f in bs.buffHandlerFactory) list.Add(Compile(f, cc, duration, tempo, seconds, period, depth + 1));
                AddCosts(bs, c, list);
                return Bundle(list, cc, duration, tempo, seconds, period, bs.buffHandlerFactory == null ? "Missing handlers." : null, Snapshot(bs), clock: HLClockKind.Realtime);
            }
            if (input is SkillDataBase || input is SkillStepDataBase)
            {
                var children = new List<HLVisualRecipe>();
                var cc = Copy(c, input is ApplyConsumerOnTimeData || input is ApplyBuffPeriodicallySkillData ? HLTopology.Self : input is AreaOfEffectSkillData ? HLTopology.Area : c.Topology);
                var beat = input is ConfigurableSkillData || input is RepeatSkillStepData ? HLTempo.Sequence : input is DurationSkillStepData ? HLTempo.Continuous : HLTempo.Cooldown;
                // Traverse only payload-bearing fields; their serialized order is preserved in Snapshot.
                foreach (var field in input.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.Name == "consumerFactory" || field.Name == "buffHandlerFactory" || field.Name == "periodicBuff" || field.Name == "skillStepFactories")
                        AddChildren(field.GetValue(input), children, cc, duration, beat, depth);
                    if (field.Name == "projectiles" && field.GetValue(input) is IEnumerable phases)
                        foreach (var phase in phases)
                        {
                            var atoms = new List<HLVisualRecipe>();
                            AddChildren(phase.GetType().GetField("onHitConsumer")?.GetValue(phase), atoms, cc, HLDurationShape.Transit, HLTempo.Collision, depth);
                            children.Add(Bundle(atoms, cc, HLDurationShape.Transit, HLTempo.Collision, 0, 0, delivery: Snapshot(phase)));
                        }
                }
                return Bundle(children, cc, duration, beat, seconds, period, delivery: Snapshot(input) + c.SelectionData,
                    operation: input is DurationSkillStepData ? HLOperation.Wait : HLOperation.Delivery);
            }
            if (input is Projectile projectile)
                return new HLVisualRecipe(Key(HLOperation.Delivery, HLSign.Unknown, null, c.Topology, HLDurationShape.Transit, projectile is ChainLightningProjectile ? HLTempo.Synchronous : HLTempo.Collision), deliveryData: SnapshotProjectile(projectile.gameObject), diagnostic: "Projectile callback does not identify its original skill payload.");
            // Behaviour data has no resource outcome of its own; preserve the full authored graph.
            if (input.GetType().Name.EndsWith("ProjectileBehaviourData", StringComparison.Ordinal))
                return new HLVisualRecipe(Key(HLOperation.InstallBehaviour, HLSign.Conditional, null,
                    input.GetType().Name.StartsWith("Bounce", StringComparison.Ordinal) ? HLTopology.Sequential : c.Topology, duration, tempo), deliveryData: Snapshot(input));
            return Unknown("Unsupported data type: " + input.GetType().FullName);
        }
        void AddCosts(CharacterSkillData skill, HLGrammarContext c, List<HLVisualRecipe> children)
        {
            if (skill.validators == null) return;
            foreach (var validator in skill.validators)
                if (validator is ResourceValidatorFactory cost)
                    children.Add(DescribeData(cost.data, c));
        }
        void AddChildren(object value, List<HLVisualRecipe> children, HLGrammarContext c, HLDurationShape duration, HLTempo tempo, int depth)
        {
            if (value is IEnumerable list) foreach (var item in list) children.Add(Compile(item, c, duration, tempo, 0, 0, depth + 1));
            else children.Add(Compile(value, c, duration, tempo, 0, 0, depth + 1));
        }
        HLVisualRecipe ConsumerFactoryRecipe(AConsumerFactory f, HLGrammarContext c, AttributeType resource, HLTopology topology, HLTempo tempo, HLDurationShape duration, float seconds) =>
            f is ConsumerFactory cf ? Consumer(cf.data, Copy(c, topology), duration, tempo, seconds, 0, 1, resource) : Unknown("Unknown consumer type.");
        static HLGrammarContext Copy(HLGrammarContext c, HLTopology topology) => new HLGrammarContext { Topology = topology, Side = c.Side, SourceAttribute = c.SourceAttribute, SourceHealth = c.SourceHealth, SourceMaxHealth = c.SourceMaxHealth, SelectionData = c.SelectionData };
        public HLVisualRecipe Consumer(ConsumerData data, HLGrammarContext c, HLDurationShape duration = HLDurationShape.Instant,
            HLTempo tempo = HLTempo.Immediate, float seconds = 0, float period = 0, float multiplier = 1, AttributeType resource = AttributeType.HealthMax)
        {
            if (data == null) return Unknown("Missing consumer data.");
            HLExpression expression = HLExpression.Unknown; AttributeType read = default; float scalar = 0; float? value = null;
            switch (data.value)
            {
                case FlatValue f when f.data != null: expression = HLExpression.Flat; scalar = f.data.value; value = scalar; break;
                case AttributeValue a when a.data != null: expression = HLExpression.SourceAttribute; read = a.data.type; scalar = a.data.multiplier; value = c.SourceAttribute?.Invoke(read) * scalar; break;
                case CurrentHealthValue h when h.data != null: expression = h.data.inverse ? HLExpression.SourceMissingHealth : HLExpression.SourceCurrentHealth; scalar = h.data.multiplier; value = (h.data.inverse ? c.SourceMaxHealth - c.SourceHealth : c.SourceHealth) * scalar; break;
                case MaxHealthValue m when m.data != null: expression = HLExpression.SourceMaxHealth; scalar = m.data.multiplier; value = c.SourceMaxHealth * scalar; break;
            }
            float? raw = -value * multiplier;
            bool invalid = expression == HLExpression.Unknown || !Finite(scalar) || !Finite(multiplier) || raw.HasValue && !Finite(raw.Value);
            // Without reduction bypass a positive raw value is clamped to zero before multiplier.
            // Defense/critical state can still invert or suppress any preview: never emit from this value.
            HLSign sign = raw.HasValue ? Sign(raw.Value) : HLSign.Conditional;
            if (!data.ignoreDamageReduction && value.HasValue && -value.Value > 0) sign = HLSign.Conditional;
            return new HLVisualRecipe(Key(HLOperation.Resource, invalid ? HLSign.Unknown : sign, resource, c.Topology, duration, tempo),
                expression, read, scalar, multiplier, invalid ? null : raw, data.ignoreDamageReduction, data.ignoreConsumerPrevention,
                stackLaw: HLStackLaw.Linear, duration: seconds, period: period, side: c.Side, diagnostic: invalid ? "Invalid or unsupported value expression." : null);
        }
        static HLSpellSignature Key(HLOperation op, HLSign sign, AttributeType? attribute, HLTopology topology, HLDurationShape duration, HLTempo tempo) =>
            new HLSpellSignature { operation = op, sign = sign, hasAttribute = attribute.HasValue, attribute = attribute ?? default, topology = topology, duration = duration, tempo = tempo };
        static HLVisualRecipe Atom(HLOperation op, HLGrammarContext c, HLDurationShape d, HLTempo t, float seconds, float period) => new HLVisualRecipe(Key(op, HLSign.Conditional, null, c.Topology, d, t), duration: seconds, period: period);
        static HLVisualRecipe Unknown(string reason) => new HLVisualRecipe(default, diagnostic: reason);
        static HLVisualRecipe Bundle(IEnumerable<HLVisualRecipe> children, HLGrammarContext c, HLDurationShape d, HLTempo t, float seconds, float period,
            string diagnostic = null, string delivery = null, HLOperation operation = HLOperation.Delivery, HLClockKind clock = HLClockKind.Simulation)
        {
            var list = new List<HLVisualRecipe>(children); HLSign sign = list.Count == 0 ? HLSign.Unknown : list[0].Signature.sign;
            foreach (var child in list) if (child.Signature.sign != sign) sign = HLSign.Mixed;
            return new HLVisualRecipe(Key(operation, sign, null, c.Topology, d, t), duration: seconds, period: period, clock: clock, side: c.Side, diagnostic: diagnostic, deliveryData: delivery, children: list);
        }
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static HLSign Sign(float value) => !Finite(value) ? HLSign.Unknown : value > 0 ? HLSign.Positive : value < 0 ? HLSign.Negative : HLSign.Zero;
        static HLSign Reverse(HLSign sign) => sign == HLSign.Positive ? HLSign.Negative : sign == HLSign.Negative ? HLSign.Positive : sign;

        /// <summary>Canonical data description for delivery/provenance comparison, excluding display identity and old art.</summary>
        public static string Snapshot(object data) => Snapshot(data, 0);
        static string Snapshot(object data, int depth)
        {
            if (data == null) return "null";
            if (depth > 32) return "invalid-depth";
            if (data is string) return "";
            if (data is ParticleSystem.MinMaxCurve curve)
                return "curve:" + curve.mode + ":" + curve.curveMultiplier.ToString("R", CultureInfo.InvariantCulture) + ":" + curve.constantMin.ToString("R", CultureInfo.InvariantCulture) + ":" + curve.constantMax.ToString("R", CultureInfo.InvariantCulture) + ":" + Snapshot(curve.curveMin, depth + 1) + ":" + Snapshot(curve.curveMax, depth + 1);
            if (data is AnimationCurve animation)
            {
                var curveText = new StringBuilder().Append(animation.preWrapMode).Append(':').Append(animation.postWrapMode);
                foreach (var key in animation.keys) curveText.Append('|').Append(key.time.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(key.value.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(key.inWeight.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(key.outWeight.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(key.weightedMode);
                return curveText.ToString();
            }
            if (data is GameObject go) return SnapshotProjectile(go);
            var type = data.GetType();
            if (type.IsEnum || type.IsPrimitive) return Convert.ToString(data, CultureInfo.InvariantCulture);
            if (data is IEnumerable sequence)
            { var b = new StringBuilder("["); foreach (var item in sequence) b.Append(Snapshot(item, depth + 1)).Append(';'); return b.Append(']').ToString(); }
            if (data is UnityEngine.Object) { var field = type.GetField("data"); return field != null ? Snapshot(field.GetValue(data), depth + 1) : "unsupported-reference"; }
            var result = new StringBuilder(type.Name).Append('{');
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance); Array.Sort(fields, (a,b) => string.CompareOrdinal(a.Name,b.Name));
            foreach (var f in fields)
            {
                if (f.Name == "name" || f.Name == "description" || f.Name == "icon" || f.Name == "uniqueID" || f.Name == "buffEffect" || f.Name == "tags" || f.Name == "onSkillTriggerFactory") continue;
                if (data is MaxHealthValueData && f.Name == "inverse") continue;
                result.Append(f.Name).Append('=').Append(Snapshot(f.GetValue(data), depth + 1)).Append(';');
            }
            return result.Append('}').ToString();
        }
        static string SnapshotProjectile(GameObject go)
        {
            if (!go) return "missing-projectile";
            var b = new StringBuilder();
            foreach (var component in go.GetComponents<Component>())
            {
                if (!(component is Projectile) && !(component is AProjectileBehaviour) && !(component is AreaOfEffect)) continue;
                b.Append(component.GetType().Name).Append('{');
                foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!field.IsDefined(typeof(SerializeField), true) && !field.IsPublic) continue;
                    if (field.Name == "source" || field.Name == "target" || field.Name.Contains("Timer")) continue;
                    b.Append(field.Name).Append('=').Append(Snapshot(field.GetValue(component), 1)).Append(';');
                }
                b.Append('}');
            }
            return b.ToString();
        }
    }
}

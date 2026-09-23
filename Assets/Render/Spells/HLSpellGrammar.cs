using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellGrammar
    {
        public HLVisualRecipe Describe(ABuffHandlerFactory factory, GameObject source, GameObject target)
        {
            return DescribeData(
                factory,
                Context(source, source != null && source == target ? HLTopology.Self : HLTopology.Single)
            );
        }

        public HLVisualRecipe Describe(ASkill skill, Entity source)
        {
            return DescribeData(Data(skill), Context(source ? source.gameObject : null, HLTopology.Single));
        }

        public HLVisualRecipe Describe(ACharacterSkill skill, Character source)
        {
            return DescribeData(Data(skill), Context(source ? source.gameObject : null, HLTopology.Single));
        }

        public HLVisualRecipe Describe(Projectile projectile)
        {
            return DescribeData(projectile, Context(projectile ? projectile.source : null, HLTopology.Single));
        }

        static HLGrammarContext Context(GameObject source, HLTopology topology)
        {
            AttributeManager attributes = source ? source.GetComponent<AttributeManager>() : null;
            Entity entity = source ? source.GetComponent<Entity>() : null;
            return new HLGrammarContext
            {
                topology = topology,
                side = entity ? entity.entityType : Entity.EntityType.None,
                sourceAttribute = t => attributes != null && attributes.Has(t) ? attributes.Get(t).Value : null,
                sourceHealth = entity && entity.health ? entity.health.Value : null,
                sourceMaxHealth = entity && entity.health ? entity.health.Max : null,
                selectionData =
                    entity && entity.data != null
                        ? Snapshot(new object[] { entity.data.targetBehaviourType, entity.data.targetValidators })
                        : ""
            };
        }

        // Only the public generic data field is inspected; no polymorphic gameplay getters execute.
        static object Data(object value)
        {
            return value?.GetType().GetField("data")?.GetValue(value);
        }

        public HLVisualRecipe DescribeData(object data, HLGrammarContext context = null)
        {
            return Compile(
                data,
                context ?? new HLGrammarContext(),
                HLDurationShape.Instant,
                HLTempo.Immediate,
                0f,
                0f,
                0
            );
        }

        HLVisualRecipe Compile(
            object input,
            HLGrammarContext c,
            HLDurationShape duration,
            HLTempo tempo,
            float seconds,
            float period,
            int depth
        )
        {
            if (depth > 32)
            {
                return Unknown("Cyclic or excessively deep data graph.");
            }
            if (input == null || input is UnityEngine.Object u && !u)
            {
                return Unknown("Missing data.");
            }
            if (input is ConsumerFactory consumer)
            {
                input = consumer.data;
            }
            if (input is BuffHandlerFactory handler)
            {
                input = handler.data;
            }
            if (input is ConsumerData cd)
            {
                return Consumer(cd, c, duration, tempo, seconds, period);
            }
            if (input is BuffHandlerBaseData hd)
            {
                HLDurationShape shape = HLDurationShape.Timed;
                if (hd.durationType == DurationType.Instant)
                {
                    shape = HLDurationShape.Instant;
                }
                else if (hd.durationType == DurationType.Infinite)
                {
                    shape = HLDurationShape.Infinite;
                }
                HLTempo beat = HLTempo.Continuous;
                if (hd.isPeriodic && shape != HLDurationShape.Instant)
                {
                    beat = HLTempo.HandlerTick;
                }
                List<HLVisualRecipe> children = new List<HLVisualRecipe>();
                if (hd.buffFactoryList != null)
                {
                    foreach (ABuffFactory f in hd.buffFactoryList)
                    {
                        children.Add(Compile(f, c, shape, beat, hd.duration, hd.periodDuration, depth + 1));
                    }
                }
                bool invalidDuration = !Finite(hd.duration) || hd.duration <= 0f;
                bool invalidPeriod = !Finite(hd.periodDuration) || hd.periodDuration <= 0f;
                string diagnostic = null;
                if (hd.buffFactoryList == null)
                {
                    diagnostic = "Missing buff list.";
                }
                else if (
                    shape == HLDurationShape.Timed && invalidDuration
                    || beat == HLTempo.HandlerTick && invalidPeriod
                )
                {
                    diagnostic = "Invalid handler clock.";
                }
                return Bundle(children, c, shape, beat, hd.duration, hd.periodDuration, diagnostic);
            }
            if (input is ABuffFactory bf)
            {
                return Compile(Data(bf), c, duration, tempo, seconds, period, depth + 1);
            }
            if (input is BaseData md)
            {
                float value = 0f;
                if (md is FlatModifierData flat)
                {
                    value = flat.value;
                }
                else if (md is UpgradeModifierData upgrade)
                {
                    value = upgrade.value;
                }
                HLExpression expression = HLExpression.Flat;
                if (md is HPBasedModifierData)
                {
                    expression = HLExpression.RecipientHealth;
                }
                else if (md is CurrentWaveModifierData)
                {
                    expression = HLExpression.Round;
                }
                else if (md is SlowModifierData || md is TimeModifierData)
                {
                    expression = HLExpression.Decay;
                }
                HLStackLaw law = HLStackLaw.None;
                if (md is FlatModifierData)
                {
                    law = HLStackLaw.FlatPowers;
                }
                else if (md is UpgradeModifierData)
                {
                    law = HLStackLaw.Linear;
                }
                else if (md is SlowModifierData)
                {
                    law = HLStackLaw.Logarithmic;
                }
                else if (md is TimeModifierData)
                {
                    law = HLStackLaw.Refresh;
                }
                HLSign sign =
                    expression != HLExpression.Flat || md.modifierType == AttributeModifierType.Override
                        ? HLSign.Conditional
                        : Sign(value);
                if (md.modifierType == AttributeModifierType.Multiply && duration == HLDurationShape.Instant)
                {
                    sign = Sign(value - 1f);
                }
                if (md.type == AttributeType.FlatArmor || md.type == AttributeType.Vulnerability)
                {
                    sign = Reverse(sign);
                }
                if (
                    md.type == AttributeType.AttackRate
                    || md.type == AttributeType.Speed
                    || md.type == AttributeType.PercentArmor
                )
                {
                    sign = HLSign.Conditional;
                }
                return new HLVisualRecipe(
                    Key(HLOperation.Attribute, sign, md.type, c.topology, duration, tempo),
                    expression,
                    scalar: value,
                    modifier: md.modifierType,
                    stackLaw: law,
                    duration: seconds,
                    period: period,
                    side: c.side,
                    deliveryData: Snapshot(md),
                    diagnostic: md is SlowModifierData || md is TimeModifierData
                        ? "Gameplay modifier constructor reads an unassigned handler."
                        : null
                );
            }
            if (input is ApplyConsumerBuffData ac)
            {
                return Compile(ac.consumerFactory, c, duration, tempo, seconds, period, depth + 1);
            }
            if (input is InvincibilityBuffData)
            {
                return Atom(HLOperation.Prevention, c, duration, tempo, seconds, period);
            }
            if (input is MultipleShootBuffData multi)
            {
                return new HLVisualRecipe(
                    Key(HLOperation.TargetCount, Sign(multi.value), null, c.topology, duration, tempo),
                    scalar: multi.value,
                    stackLaw: HLStackLaw.Linear,
                    duration: seconds,
                    period: period
                );
            }
            if (input is AddSkillBuffData install)
            {
                return Bundle(
                    new[] { Compile(install.skillFactory, c, duration, tempo, seconds, period, depth + 1) },
                    c,
                    duration,
                    tempo,
                    seconds,
                    period,
                    operation: HLOperation.InstallSkill
                );
            }
            if (input is ProjectileBehaviourBuffData pb)
            {
                return Bundle(
                    new[] { Compile(pb.projectileBehaviour, c, duration, tempo, seconds, period, depth + 1) },
                    c,
                    duration,
                    tempo,
                    seconds,
                    period,
                    operation: HLOperation.InstallBehaviour
                );
            }
            if (input is ManaOnRoundEndBuffData mana)
            {
                return ConsumerFactoryRecipe(
                    mana.consumerFactory,
                    c,
                    AttributeType.ManaMax,
                    HLTopology.Self,
                    HLTempo.RoundEnd,
                    duration,
                    seconds
                );
            }
            if (input is HealAllEntitiesOnRoundEndBuffData heal)
            {
                return ConsumerFactoryRecipe(
                    heal.consumerFactory,
                    c,
                    AttributeType.HealthMax,
                    HLTopology.Group,
                    HLTempo.RoundEnd,
                    duration,
                    seconds
                );
            }
            if (input is DamageAllEntityOnEntityDieBuffData death)
            {
                return ConsumerFactoryRecipe(
                    death.damageToAllEntity,
                    c,
                    AttributeType.HealthMax,
                    HLTopology.Group,
                    HLTempo.Death,
                    duration,
                    seconds
                );
            }
            if (
                input is ASkillFactory
                || input is ACharacterSkillFactory
                || input is ASkillStepFactory
                || input is AProjectileBehaviourFactory
            )
            {
                return Compile(Data(input), c, duration, tempo, seconds, period, depth + 1);
            }
            if (input is ResourceValidatorData cost)
            {
                return ConsumerFactoryRecipe(
                    cost.consumer,
                    c,
                    AttributeType.ManaMax,
                    HLTopology.Self,
                    HLTempo.Immediate,
                    HLDurationShape.Instant,
                    0f
                );
            }
            if (input is ApplyConsumerCharacterSkillData cs)
            {
                HLGrammarContext cc = Copy(c, cs.isSingle ? HLTopology.Single : HLTopology.Group);
                HLVisualRecipe payload = cs.consumer is ConsumerFactory cf
                    ? Consumer(cf.data, cc, HLDurationShape.Instant, HLTempo.Immediate, 0f, 0f, cs.multiplier)
                    : Unknown("Unknown consumer type.");
                List<HLVisualRecipe> children = new List<HLVisualRecipe> { payload };
                AddCosts(cs, c, children);
                return Bundle(
                    children,
                    cc,
                    HLDurationShape.Instant,
                    HLTempo.Immediate,
                    0f,
                    0f,
                    delivery: Snapshot(cs),
                    clock: HLClockKind.Realtime
                );
            }
            if (input is BuffCharacterSkillData bs)
            {
                HLGrammarContext cc = Copy(c, bs.isSingle ? HLTopology.Single : HLTopology.Group);
                List<HLVisualRecipe> list = new List<HLVisualRecipe>();
                if (bs.buffHandlerFactory != null)
                {
                    foreach (ABuffHandlerFactory f in bs.buffHandlerFactory)
                    {
                        list.Add(Compile(f, cc, duration, tempo, seconds, period, depth + 1));
                    }
                }
                AddCosts(bs, c, list);
                return Bundle(
                    list,
                    cc,
                    duration,
                    tempo,
                    seconds,
                    period,
                    bs.buffHandlerFactory == null ? "Missing handlers." : null,
                    Snapshot(bs),
                    clock: HLClockKind.Realtime
                );
            }
            if (input is SkillDataBase || input is SkillStepDataBase)
            {
                List<HLVisualRecipe> children = new List<HLVisualRecipe>();
                HLTopology topology = c.topology;
                if (input is ApplyConsumerOnTimeData || input is ApplyBuffPeriodicallySkillData)
                {
                    topology = HLTopology.Self;
                }
                else if (input is AreaOfEffectSkillData)
                {
                    topology = HLTopology.Area;
                }
                HLGrammarContext cc = Copy(c, topology);
                HLTempo beat = HLTempo.Cooldown;
                if (input is ConfigurableSkillData || input is RepeatSkillStepData)
                {
                    beat = HLTempo.Sequence;
                }
                else if (input is DurationSkillStepData)
                {
                    beat = HLTempo.Continuous;
                }
                // Traverse only payload-bearing fields; their serialized order is preserved in Snapshot.
                foreach (FieldInfo field in input.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (
                        field.Name == "consumerFactory"
                        || field.Name == "buffHandlerFactory"
                        || field.Name == "periodicBuff"
                        || field.Name == "skillStepFactories"
                    )
                    {
                        AddChildren(field.GetValue(input), children, cc, duration, beat, depth);
                    }
                    if (field.Name == "projectiles" && field.GetValue(input) is IEnumerable phases)
                    {
                        foreach (object phase in phases)
                        {
                            List<HLVisualRecipe> atoms = new List<HLVisualRecipe>();
                            AddChildren(
                                phase.GetType().GetField("onHitConsumer")?.GetValue(phase),
                                atoms,
                                cc,
                                HLDurationShape.Transit,
                                HLTempo.Collision,
                                depth
                            );
                            children.Add(
                                Bundle(
                                    atoms,
                                    cc,
                                    HLDurationShape.Transit,
                                    HLTempo.Collision,
                                    0f,
                                    0f,
                                    delivery: Snapshot(phase)
                                )
                            );
                        }
                    }
                }
                return Bundle(
                    children,
                    cc,
                    duration,
                    beat,
                    seconds,
                    period,
                    delivery: Snapshot(input) + c.selectionData,
                    operation: input is DurationSkillStepData ? HLOperation.Wait : HLOperation.Delivery
                );
            }
            if (input is Projectile projectile)
            {
                return new HLVisualRecipe(
                    Key(
                        HLOperation.Delivery,
                        HLSign.Unknown,
                        null,
                        c.topology,
                        HLDurationShape.Transit,
                        projectile is ChainLightningProjectile ? HLTempo.Synchronous : HLTempo.Collision
                    ),
                    deliveryData: SnapshotProjectile(projectile.gameObject),
                    diagnostic: "Projectile callback does not identify its original skill payload."
                );
            }
            // Behaviour data has no resource outcome of its own; preserve the full authored graph.
            if (input.GetType().Name.EndsWith("ProjectileBehaviourData", StringComparison.Ordinal))
            {
                return new HLVisualRecipe(
                    Key(
                        HLOperation.InstallBehaviour,
                        HLSign.Conditional,
                        null,
                        input.GetType().Name.StartsWith("Bounce", StringComparison.Ordinal)
                            ? HLTopology.Sequential
                            : c.topology,
                        duration,
                        tempo
                    ),
                    deliveryData: Snapshot(input)
                );
            }
            return Unknown("Unsupported data type: " + input.GetType().FullName);
        }

        void AddCosts(CharacterSkillData skill, HLGrammarContext c, List<HLVisualRecipe> children)
        {
            if (skill.validators == null)
            {
                return;
            }
            foreach (ACharacterSkillValidatorFactory validator in skill.validators)
            {
                if (validator is ResourceValidatorFactory cost)
                {
                    children.Add(DescribeData(cost.data, c));
                }
            }
        }

        void AddChildren(
            object value,
            List<HLVisualRecipe> children,
            HLGrammarContext c,
            HLDurationShape duration,
            HLTempo tempo,
            int depth
        )
        {
            if (value is IEnumerable list)
            {
                foreach (object item in list)
                {
                    children.Add(Compile(item, c, duration, tempo, 0f, 0f, depth + 1));
                }
            }
            else
            {
                children.Add(Compile(value, c, duration, tempo, 0f, 0f, depth + 1));
            }
        }

        HLVisualRecipe ConsumerFactoryRecipe(
            AConsumerFactory f,
            HLGrammarContext c,
            AttributeType resource,
            HLTopology topology,
            HLTempo tempo,
            HLDurationShape duration,
            float seconds
        )
        {
            return f is ConsumerFactory cf
                ? Consumer(cf.data, Copy(c, topology), duration, tempo, seconds, 0f, 1f, resource)
                : Unknown("Unknown consumer type.");
        }

        static HLGrammarContext Copy(HLGrammarContext c, HLTopology topology)
        {
            return new HLGrammarContext
            {
                topology = topology,
                side = c.side,
                sourceAttribute = c.sourceAttribute,
                sourceHealth = c.sourceHealth,
                sourceMaxHealth = c.sourceMaxHealth,
                selectionData = c.selectionData
            };
        }

        public HLVisualRecipe Consumer(
            ConsumerData data,
            HLGrammarContext c,
            HLDurationShape duration = HLDurationShape.Instant,
            HLTempo tempo = HLTempo.Immediate,
            float seconds = 0f,
            float period = 0f,
            float multiplier = 1f,
            AttributeType resource = AttributeType.HealthMax
        )
        {
            if (data == null)
            {
                return Unknown("Missing consumer data.");
            }
            HLExpression expression = HLExpression.Unknown;
            AttributeType read = default;
            float scalar = 0f;
            float? value = null;
            switch (data.value)
            {
                case FlatValue f when f.data != null:
                    expression = HLExpression.Flat;
                    scalar = f.data.value;
                    value = scalar;
                    break;
                case AttributeValue a when a.data != null:
                    expression = HLExpression.SourceAttribute;
                    read = a.data.type;
                    scalar = a.data.multiplier;
                    value = c.sourceAttribute?.Invoke(read) * scalar;
                    break;
                case CurrentHealthValue h when h.data != null:
                    expression = h.data.inverse ? HLExpression.SourceMissingHealth : HLExpression.SourceCurrentHealth;
                    scalar = h.data.multiplier;
                    value = (h.data.inverse ? c.sourceMaxHealth - c.sourceHealth : c.sourceHealth) * scalar;
                    break;
                case MaxHealthValue m when m.data != null:
                    expression = HLExpression.SourceMaxHealth;
                    scalar = m.data.multiplier;
                    value = c.sourceMaxHealth * scalar;
                    break;
            }
            float? raw = -value * multiplier;
            bool invalid =
                expression == HLExpression.Unknown
                || !Finite(scalar)
                || !Finite(multiplier)
                || raw.HasValue && !Finite(raw.Value);
            // Without reduction bypass a positive raw value is clamped to zero before multiplier.
            // Defense/critical state can still invert or suppress any preview: never emit from this value.
            HLSign sign = raw.HasValue ? Sign(raw.Value) : HLSign.Conditional;
            if (!data.ignoreDamageReduction && value.HasValue && -value.Value > 0f)
            {
                sign = HLSign.Conditional;
            }
            return new HLVisualRecipe(
                Key(HLOperation.Resource, invalid ? HLSign.Unknown : sign, resource, c.topology, duration, tempo),
                expression,
                read,
                scalar,
                multiplier,
                invalid ? null : raw,
                data.ignoreDamageReduction,
                data.ignoreConsumerPrevention,
                stackLaw: HLStackLaw.Linear,
                duration: seconds,
                period: period,
                side: c.side,
                diagnostic: invalid ? "Invalid or unsupported value expression." : null
            );
        }

        static HLSpellSignature Key(
            HLOperation op,
            HLSign sign,
            AttributeType? attribute,
            HLTopology topology,
            HLDurationShape duration,
            HLTempo tempo
        )
        {
            return new HLSpellSignature
            {
                operation = op,
                sign = sign,
                hasAttribute = attribute.HasValue,
                attribute = attribute ?? default,
                topology = topology,
                duration = duration,
                tempo = tempo
            };
        }

        static HLVisualRecipe Atom(
            HLOperation op,
            HLGrammarContext c,
            HLDurationShape d,
            HLTempo t,
            float seconds,
            float period
        )
        {
            return new HLVisualRecipe(
                Key(op, HLSign.Conditional, null, c.topology, d, t),
                duration: seconds,
                period: period
            );
        }

        static HLVisualRecipe Unknown(string reason)
        {
            return new HLVisualRecipe(default, diagnostic: reason);
        }

        static HLVisualRecipe Bundle(
            IEnumerable<HLVisualRecipe> children,
            HLGrammarContext c,
            HLDurationShape d,
            HLTempo t,
            float seconds,
            float period,
            string diagnostic = null,
            string delivery = null,
            HLOperation operation = HLOperation.Delivery,
            HLClockKind clock = HLClockKind.Simulation
        )
        {
            List<HLVisualRecipe> list = new List<HLVisualRecipe>(children);
            HLSign sign = list.Count == 0 ? HLSign.Unknown : list[0].signature.sign;
            foreach (HLVisualRecipe child in list)
            {
                if (child.signature.sign != sign)
                {
                    sign = HLSign.Mixed;
                }
            }
            return new HLVisualRecipe(
                Key(operation, sign, null, c.topology, d, t),
                duration: seconds,
                period: period,
                clock: clock,
                side: c.side,
                diagnostic: diagnostic,
                deliveryData: delivery,
                children: list
            );
        }

        public static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static HLSign Sign(float value)
        {
            if (!Finite(value))
            {
                return HLSign.Unknown;
            }
            if (value > 0f)
            {
                return HLSign.Positive;
            }
            if (value < 0f)
            {
                return HLSign.Negative;
            }
            return HLSign.Zero;
        }

        static HLSign Reverse(HLSign sign)
        {
            if (sign == HLSign.Positive)
            {
                return HLSign.Negative;
            }
            if (sign == HLSign.Negative)
            {
                return HLSign.Positive;
            }
            return sign;
        }

        public static string Snapshot(object data)
        {
            return Snapshot(data, 0);
        }

        static string Snapshot(object data, int depth)
        {
            if (data == null)
            {
                return "null";
            }
            if (depth > 32)
            {
                return "invalid-depth";
            }
            if (data is string)
            {
                return "";
            }
            if (data is ParticleSystem.MinMaxCurve curve)
            {
                return "curve:"
                    + curve.mode
                    + ":"
                    + curve.curveMultiplier.ToString("R", CultureInfo.InvariantCulture)
                    + ":"
                    + curve.constantMin.ToString("R", CultureInfo.InvariantCulture)
                    + ":"
                    + curve.constantMax.ToString("R", CultureInfo.InvariantCulture)
                    + ":"
                    + Snapshot(curve.curveMin, depth + 1)
                    + ":"
                    + Snapshot(curve.curveMax, depth + 1);
            }
            if (data is AnimationCurve animation)
            {
                StringBuilder curveText = new StringBuilder()
                    .Append(animation.preWrapMode)
                    .Append(':')
                    .Append(animation.postWrapMode);
                foreach (Keyframe key in animation.keys)
                {
                    curveText
                        .Append('|')
                        .Append(key.time.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.value.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.inWeight.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.outWeight.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.weightedMode);
                }
                return curveText.ToString();
            }
            if (data is GameObject go)
            {
                return SnapshotProjectile(go);
            }
            Type type = data.GetType();
            if (type.IsEnum || type.IsPrimitive)
            {
                return Convert.ToString(data, CultureInfo.InvariantCulture);
            }
            if (data is IEnumerable sequence)
            {
                StringBuilder b = new StringBuilder("[");
                foreach (object item in sequence)
                {
                    b.Append(Snapshot(item, depth + 1)).Append(';');
                }
                return b.Append(']').ToString();
            }
            if (data is UnityEngine.Object)
            {
                FieldInfo field = type.GetField("data");
                return field != null ? Snapshot(field.GetValue(data), depth + 1) : "unsupported-reference";
            }
            StringBuilder result = new StringBuilder(type.Name).Append('{');
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach (FieldInfo f in fields)
            {
                if (
                    f.Name == "name"
                    || f.Name == "description"
                    || f.Name == "icon"
                    || f.Name == "uniqueID"
                    || f.Name == "buffEffect"
                    || f.Name == "tags"
                    || f.Name == "onSkillTriggerFactory"
                )
                {
                    continue;
                }
                if (data is MaxHealthValueData && f.Name == "inverse")
                {
                    continue;
                }
                result.Append(f.Name).Append('=').Append(Snapshot(f.GetValue(data), depth + 1)).Append(';');
            }
            return result.Append('}').ToString();
        }

        static string SnapshotProjectile(GameObject go)
        {
            if (!go)
            {
                return "missing-projectile";
            }
            StringBuilder b = new StringBuilder();
            foreach (Component component in go.GetComponents<Component>())
            {
                if (!(component is Projectile) && !(component is AProjectileBehaviour) && !(component is AreaOfEffect))
                {
                    continue;
                }
                b.Append(component.GetType().Name).Append('{');
                foreach (
                    FieldInfo field in component
                        .GetType()
                        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                )
                {
                    if (!field.IsDefined(typeof(SerializeField), true) && !field.IsPublic)
                    {
                        continue;
                    }
                    if (field.Name == "source" || field.Name == "target" || field.Name.Contains("Timer"))
                    {
                        continue;
                    }
                    b.Append(field.Name).Append('=').Append(Snapshot(field.GetValue(component), 1)).Append(';');
                }
                b.Append('}');
            }
            return b.ToString();
        }
    }
}

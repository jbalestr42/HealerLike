using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;


// Le plan:
// Renommer ABuffHandler en GameplayEffect
// supprimer AliveBuffHandler et gérer la logic de duration/period dans un seul buffHandler
// Créer un InstantGameplayEffect qui aura sa propre logic pour appliquer quelque chose une seule fois
// On aura donc un InstatAttributeModifier qui va faire la logic pour modifier la base value
// On pourra donc remettre ABuff comme avant et supprimer la fonction Instant

// Est-ce que ça suffit pour:
// instant
// duration (start/stop or add/remove buff)
// period avec instant

// Modifier le buffhandler pour qu'il ai une liste de buffFactory et pas une seule factory

public enum DurationType
{
    Instant,
    Duration,
    Infinite
}

[InlineEditor]
public abstract class ABuffHandlerFactory : SerializedScriptableObject
{
    [HideInInlineEditors]
    public string uniqueID = Guid.NewGuid().ToString();
    public abstract ABuffHandler GetBuffHandler();
    public abstract ABuffFactory GetBuffFactory();
    public abstract GameObject GetBuffEffect();
}

public class BuffHandlerFactory<BuffHandlerType, DataType> : ABuffHandlerFactory
                                            where BuffHandlerType : ABuffHandler<DataType>, new()
                                            where DataType : BuffHandlerBaseData
{
    [InlineProperty]
    [HideLabel]
    public DataType data;

    public override ABuffHandler GetBuffHandler()
    {
        return new BuffHandlerType() { data = this.data };
    }

    public override ABuffFactory GetBuffFactory()
    {
        return data.buffFactory;
    }

    public override GameObject GetBuffEffect()
    {
        return data.buffEffect;
    }
}

public abstract class ABuffHandler
{
    public abstract bool IsDone();
    public abstract void Start(GameObject source, GameObject target);
    public abstract void Update(float deltaTime);
    public abstract void Stop(GameObject source, GameObject target);
    public abstract void Refresh(GameObject source, GameObject target);
    public abstract void ResetDuration();
    public abstract void ResetPeriodDuration();
    public abstract DurationType durationType { get; }
    public abstract float duration { get; }
    public abstract bool hasDuration { get; }
    public abstract bool isDone { get; }
    public abstract bool isPeriodDone { get; }
}

public class BuffHandlerBaseData
{
    public DurationType durationType;

    //TODO hide if durationType is instant
    public float duration;
    //TODO hide if durationType is instant
    public bool isPeriodic;
    //TODO hide if isPeriodic is false
    public float periodDuration;

    [CreateDataButton]
    public ABuffFactory buffFactory;

    [AssetsOnly]
    public GameObject buffEffect; // TODO IBuffEffect ? to manage start and stop visual effect
}

public abstract class ABuffHandler<DataType> : ABuffHandler where DataType : BuffHandlerBaseData
{
    public DataType data;
    public float durationTimer;
    public float periodDurationTimer;

    public override void Update(float deltaTime)
    {
        if (hasDuration)
        {
            durationTimer += deltaTime;
            if (data.durationType == DurationType.Duration && durationTimer > data.duration)
            {
                durationTimer = data.duration;
            }
            
            if (data.isPeriodic)
            {
                periodDurationTimer += deltaTime;
                if (periodDurationTimer > data.periodDuration)
                {
                    periodDurationTimer = data.periodDuration;
                }
            }
        }
    }

    public override void ResetDuration() => durationTimer = 0f;
    public override void ResetPeriodDuration() => periodDurationTimer = 0f;
    public override DurationType durationType => data.durationType;
    public override float duration => data.duration;
    public override bool hasDuration => data.durationType == DurationType.Duration || data.durationType == DurationType.Infinite;
    public override bool isDone =>
        data.durationType switch
        {
            DurationType.Instant => true,
            DurationType.Duration => durationTimer >= data.duration,
            DurationType.Infinite => false,
            _ => false
        };
    public override bool isPeriodDone =>
        data.durationType switch
        {
            DurationType.Instant => true,
            DurationType.Duration or DurationType.Infinite => periodDurationTimer >= data.periodDuration,
            _ => false
        };
}
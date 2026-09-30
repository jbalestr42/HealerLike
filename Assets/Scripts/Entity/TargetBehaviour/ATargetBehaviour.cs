using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TargetBehaviourType
{
    First = 0,
    Nearest = 1,
    Fastest = 2,
    LowestHealth = 3,
    Random = 4,
    Farest = 5,
}

public abstract class ATargetBehaviour
{
    public static readonly int MaxTarget = 100;

    List<ATargetValidator> _targetValidators = new List<ATargetValidator>();
    public List<ATargetValidator> targetValidators { get { return _targetValidators; } set { _targetValidators = value; } }

    int _targetCount = 1;
    public int targetCount { get { return _targetCount; } set { _targetCount = value; } }

    protected List<GameObject> _targets = new List<GameObject>();

    // Entities with this tag are targeted first (e.g. a taunt), null to ignore it
    GameplayTag _tauntTag;
    public GameplayTag tauntTag { get { return _tauntTag; } set { _tauntTag = value; } }
    // Some ways to pick targets don't care about the taunt (e.g. at random)
    protected virtual bool isTauntable => true;

    public virtual List<GameObject> GetTargets(GameObject source, Vector3 position, float range, Entity.EntityType entityType)
    {
        _targets.Clear();

        foreach (GameObject target in EntityManager.instance.GetEntities(entityType))
        {
            if (Vector3.Distance(target.transform.position, position) <= range)
            {
                if (CanAddTarget(source, target))
                {
                    _targets.Add(target);
                }
            }
        }

        ApplyBehaviour(_targets, position, range);
        PrioritizeTaunting(_targets);

        // Focus marked entity first
        //if at some point we want multiple marked entity
        //_targets.Sort((GameObject a, GameObject b) =>
        //{
        //    return MarkManager.instance.IsEntityMarked(b).CompareTo(MarkManager.instance.IsEntityMarked(a));
        //});

        // Move single marked entity at first index
        GameObject marked = _targets.Find((GameObject a) => MarkManager.instance.IsEntityMarked(a));
        if (marked)
        {
            _targets.Remove(marked);
            _targets.Insert(0, marked);
        }

        if (_targetCount > 0 && _targetCount < _targets.Count)
        {
            _targets.RemoveRange(_targetCount, _targets.Count - _targetCount);
        }
        return _targets;
    }

    // Moves the taunting targets first, keeping the order of the behaviour among them and among the others
    public void PrioritizeTaunting(List<GameObject> targets)
    {
        if (_tauntTag == null || !isTauntable)
        {
            return;
        }

        List<GameObject> taunting = targets.FindAll(IsTaunting);
        if (taunting.Count == 0)
        {
            return;
        }
        targets.RemoveAll(IsTaunting);
        targets.InsertRange(0, taunting);
    }

    bool IsTaunting(GameObject target)
    {
        Entity entity = target != null ? target.GetComponent<Entity>() : null;
        return entity != null && entity.HasTag(_tauntTag);
    }

    public bool CanAddTarget(GameObject source, GameObject target)
    {
        foreach (ATargetValidator targetValidator in _targetValidators)
        {
            if (!targetValidator.IsValid(source, target))
            {
                return false;
            }
        }
        return true;
    }

    public bool IsValidTarget(GameObject target)
    {
        return target != null && _targets.Contains(target);
    }

    public abstract TargetBehaviourType targetType { get; }
    public abstract void ApplyBehaviour(List<GameObject> targets, Vector3 position, float range);

    // Types that have an implementation (Fastest has none)
    static readonly Dictionary<TargetBehaviourType, System.Func<ATargetBehaviour>> Constructors = new Dictionary<TargetBehaviourType, System.Func<ATargetBehaviour>>
    {
        { TargetBehaviourType.First, () => new FirstTargetBehaviour() },
        { TargetBehaviourType.Nearest, () => new NearestTargetBehaviour() },
        { TargetBehaviourType.LowestHealth, () => new LowestHealthTargetBehaviour() },
        { TargetBehaviourType.Random, () => new RandomTargetBehaviour() },
        { TargetBehaviourType.Farest, () => new FarestTargetBehaviour() },
    };

    public static bool IsSupported(TargetBehaviourType type)
    {
        return Constructors.ContainsKey(type);
    }

    // Next type in the enum order that has an implementation, back to the first one after the last
    public static TargetBehaviourType GetNextSupportedType(TargetBehaviourType type)
    {
        TargetBehaviourType[] types = (TargetBehaviourType[])System.Enum.GetValues(typeof(TargetBehaviourType));
        int index = System.Array.IndexOf(types, type);
        for (int i = 1; i <= types.Length; i++)
        {
            TargetBehaviourType next = types[(index + i) % types.Length];
            if (IsSupported(next))
            {
                return next;
            }
        }
        return type;
    }

    public static ATargetBehaviour Create(TargetBehaviourType type)
    {
        if (Constructors.TryGetValue(type, out System.Func<ATargetBehaviour> constructor))
        {
            return constructor();
        }

        Debug.LogError($"[ATargetBehaviour] Unkown target behaviour '{type}'");
        return null;
    }
}
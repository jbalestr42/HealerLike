using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ReviveOnDeathBuff")]
public class ReviveOnDeathBuffFactory : BuffFactory<ReviveOnDeathBuff, ReviveOnDeathBuffData> { }

[Serializable]
public class ReviveOnDeathBuffData
{
    // Part (0-1) of the max health the holder comes back with
    [Range(0f, 1f)]
    public float healthRatio = 0.3f;
    // Health added to that part (e.g. 1 with no ratio: survives with 1 health)
    [Min(0f)]
    public float flatHealth = 0f;
    // Given to the holder when it is kept alive, from itself (e.g. a short invincibility), none: nothing more
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
    // Also given to the holder when it is kept alive (e.g. Blessed), none: nothing more
    public StatusData status;
}

// Keeps the holder alive the first time it dies in a battle and heals it back (e.g. a phylactery, or the Halo
// leaving it with 1 health, invincible for a while and blessed)
public class ReviveOnDeathBuff : ABuff<ReviveOnDeathBuffData>
{
    Entity _owner;
    bool _isUsed = false;
    public bool isUsed => _isUsed;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.AddDeathPrevention(Revive);
        AscensionGameType.OnBattleStart.AddListener(Rearm);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.RemoveDeathPrevention(Revive);
        }
        AscensionGameType.OnBattleStart.RemoveListener(Rearm);
    }

    // Each battle can revive it once
    public void Rearm()
    {
        _isUsed = false;
    }

    public bool Revive(Entity entity)
    {
        if (_isUsed)
        {
            return false;
        }

        _isUsed = true;
        // A heal through the regular flow, applied on the next update: ignores the armor and the invincibility
        ResourceModifier revive = new ResourceModifier { source = entity.gameObject };
        revive.consumers.Add(new RuntimeConsumer(entity.health.Max * data.healthRatio + data.flatHealth));
        entity.health.AddResourceModifier(revive);
        if (data.buffHandlerFactory != null)
        {
            entity.GetComponent<BuffManager>().AddHandler(data.buffHandlerFactory, entity.gameObject, entity.gameObject);
        }
        entity.ApplyStatus(data.status, entity.gameObject);
        return true;
    }
}

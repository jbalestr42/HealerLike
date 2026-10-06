using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ApplyBuffBelowHealthBuff")]
public class ApplyBuffBelowHealthBuffFactory : BuffFactory<ApplyBuffBelowHealthBuff, ApplyBuffBelowHealthBuffData> { }

[Serializable]
public class ApplyBuffBelowHealthBuffData
{
    // Health percent (0-1) the holder has to fall below
    [Range(0f, 1f)]
    public float threshold = 0.25f;
    // Given to the holder the first time it falls below the threshold in a battle
    [CreateDataButton]
    public ABuffHandlerFactory buffHandlerFactory;
}

// Gives a buff to the holder the first time its health falls below a threshold in a battle (e.g. an
// invincibility to survive a burst)
public class ApplyBuffBelowHealthBuff : ABuff<ApplyBuffBelowHealthBuffData>
{
    Entity _owner;
    bool _isTriggered = false;
    public bool isTriggered => _isTriggered;

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.health.OnValueChanged.AddListener(OnHealthChanged);
        AscensionGameType.OnBattleStart.AddListener(Rearm);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null && _owner.health != null)
        {
            _owner.health.OnValueChanged.RemoveListener(OnHealthChanged);
        }
        AscensionGameType.OnBattleStart.RemoveListener(Rearm);
    }

    // Each battle can trigger it once
    public void Rearm()
    {
        _isTriggered = false;
    }

    void OnHealthChanged(ResourceAttribute health)
    {
        // Too late once dead
        if (_isTriggered || health.Value <= 0f || health.percent >= data.threshold)
        {
            return;
        }

        _isTriggered = true;
        _owner.AddBuffHandler(data.buffHandlerFactory, _owner.gameObject, _owner.gameObject);
    }
}

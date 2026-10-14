using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ReduceCooldownBuff")]
public class ReduceCooldownBuffFactory : BuffFactory<ReduceCooldownBuff, ReduceCooldownBuffData> { }

[Serializable]
public class ReduceCooldownBuffData
{
    // Time taken off the cooldown running of each skill of the holder
    [Min(0f)]
    public float seconds = 1f;
}

// Instant: brings the next use of every skill of the holder closer (e.g. the Purifier when it gets blessed)
public class ReduceCooldownBuff : ABuff<ReduceCooldownBuffData>
{
    public override void Instant(GameObject source, GameObject target)
    {
        if (target == null)
        {
            return;
        }

        foreach (ICooldownSkill skill in target.GetComponents<ICooldownSkill>())
        {
            skill.ReduceCooldown(data.seconds);
        }
    }

    public override void Add(GameObject source, GameObject target) { }

    public override void Remove(GameObject source, GameObject target) { }
}

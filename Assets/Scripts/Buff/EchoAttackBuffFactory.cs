using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/EchoAttackBuff")]
public class EchoAttackBuffFactory : BuffFactory<EchoAttackBuff, EchoAttackBuffData> { }

[Serializable]
public class EchoAttackBuffData
{
    // Every attackCount attacks, the last one is made twice
    [Min(1)]
    public int attackCount = 4;
    // Seconds before the attack is repeated, so both can be told apart
    [Min(0)]
    public float delay = 0.2f;
}

public class EchoAttackBuff : ABuff<EchoAttackBuffData>
{
    Entity _owner;
    int _attacks = 0;

    public void OnAttack(ProjectileAttack attack)
    {
        _attacks++;
        if (_attacks < data.attackCount)
        {
            return;
        }

        _attacks = 0;
        if (data.delay > 0f)
        {
            _owner.StartCoroutine(RepeatAfterDelay(attack));
        }
        else
        {
            attack.Repeat();
        }
    }

    IEnumerator RepeatAfterDelay(ProjectileAttack attack)
    {
        yield return new WaitForSeconds(data.delay);
        attack.Repeat();
    }

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.OnAttack.AddListener(OnAttack);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        _owner.OnAttack.RemoveListener(OnAttack);
    }
}

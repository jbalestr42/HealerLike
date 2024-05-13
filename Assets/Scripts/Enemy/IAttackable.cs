using UnityEngine;

public class OnHitData
{
    public ResourceModifier resourceModifier = new ResourceModifier();
    public IAttacker attacker;
    public GameObject source;
    public IAttackable attackable;
    public GameObject target;
}

public interface IAttackable
{
    void OnHit(ResourceModifier resourceModifier);
    void OnHit(OnHitData onHitData);
}
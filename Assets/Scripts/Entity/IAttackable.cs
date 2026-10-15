using System.Collections.Generic;
using UnityEngine;

public class OnHitData
{
    public ResourceModifier resourceModifier = new ResourceModifier();
    public IAttacker attacker;
    public GameObject source;
    public IAttackable attackable;
    public GameObject target;
    // Given to the target along with the on hit effects of the attacker (e.g. the ones of a projectile)
    public List<ABuffHandlerFactory> onHitEffects = new List<ABuffHandlerFactory>();
}

public interface IAttackable
{
    void OnHit(ResourceModifier resourceModifier);
    void OnHit(OnHitData onHitData);
    GameObject owner { get; }
}
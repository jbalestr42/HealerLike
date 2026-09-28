using System.Collections.Generic;
using UnityEngine;

// Turns damage into a heal on the most wounded ally (Drain Life, Blood Cultist, ...)
public static class LifeSteal
{
    // The living ally with the lowest health percent, null without any
    public static Entity FindMostWounded(List<GameObject> allies)
    {
        Entity mostWounded = null;
        foreach (GameObject ally in allies)
        {
            Entity entity = ally != null ? ally.GetComponent<Entity>() : null;
            if (entity == null || entity.health == null || entity.health.Value <= 0f)
            {
                continue;
            }
            if (mostWounded == null || entity.health.percent < mostWounded.health.percent)
            {
                mostWounded = entity;
            }
        }
        return mostWounded;
    }

    // The heal goes through the regular resource flow, so heal modifiers and feedbacks apply
    public static Entity HealMostWounded(GameObject source, List<GameObject> allies, float amount)
    {
        if (amount <= 0f)
        {
            return null;
        }

        Entity mostWounded = FindMostWounded(allies);
        if (mostWounded != null)
        {
            ResourceModifier heal = new ResourceModifier { source = source };
            heal.consumers.Add(new RuntimeConsumer(amount));
            mostWounded.health.AddResourceModifier(heal);
        }
        return mostWounded;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightningProjectile : Projectile
{
    public enum EffectMode
    {
        FixedDuration,
        AttackRateDuration
    }

    [SerializeField] EffectMode _effectMode = EffectMode.FixedDuration;
    [SerializeField] float _effectDuration = 0.25f;
    bool _shouldDestroyProjectile = false;

    IEnumerator Start()
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        List<GameObject> targets = new List<GameObject>();
        while (target)
        {
            targets.Add(target);
            ApplyOnHit(target, source);
        }
        lineRenderer.positionCount = targets.Count + 1;

        float effectDuration = _effectMode == EffectMode.FixedDuration ? _effectDuration : (1f / source.GetComponent<AttributeManager>().Get(AttributeType.AttackRate).Value) + 0.05f;
        float timer = 0f;
        while (timer <= effectDuration)
        {
            if (source == null)
            {
                break;
            }

            timer += Time.deltaTime;
            Vector3 sourcePosition = source.GetComponent<Entity>().skillStartPoint.transform.position;
            int position = 0;
            bool hasValidTarget = false;
            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    hasValidTarget = true;
                    lineRenderer.SetPosition(position, sourcePosition);
                    sourcePosition = target.GetComponent<Entity>().targetPoint.transform.position;
                    position += 1;
                    lineRenderer.SetPosition(position, sourcePosition);
                }
            }

            if (!hasValidTarget)
            {
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        _shouldDestroyProjectile = true;
    }

    public override bool ShouldDestroyProjectile()
    {
        return _shouldDestroyProjectile;
    }
}
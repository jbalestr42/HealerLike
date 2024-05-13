using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class AreaOfEffectSkillData : SkillDataBase
{
    [HorizontalGroup("Split", 75)]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public GameObject areaOfEffectPrefab;
}

public class AreaOfEffectSkill : ASkill<AreaOfEffectSkillData>
{
    Attribute _cooldownDuration;
    Attribute _range;

    void Start()
    {
        requirements = new List<IRequirement>();
        requirements.Add(new TargetRequirement(gameObject));
        _cooldownDuration = GetComponent<AttributeManager>().Get(AttributeType.AttackRate);
        _range = GetComponent<AttributeManager>().Get(AttributeType.Range);
    }

    public override bool Execute(GameObject source)
    {
        if (IsRequirementValidated())
        {
            GameObject areaOfEffectGo = Instantiate(data.areaOfEffectPrefab, source.transform.position, Quaternion.identity);
            AreaOfEffect areaOfEffect = areaOfEffectGo.GetComponent<AreaOfEffect>();
            areaOfEffect.source = source;
            areaOfEffect.radius = _range.Value;
            return true;
        }
        return false;
    }

    public override float cooldownDuration => 1f / _cooldownDuration.Value;
}
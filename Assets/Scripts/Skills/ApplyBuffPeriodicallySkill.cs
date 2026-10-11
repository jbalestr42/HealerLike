using System;
using System.Collections.Generic;
using UnityEngine;
using Oisif.Inspector;

[Serializable]
public class ApplyBuffPeriodicallySkillData : SkillDataBase
{
    [CreateDataButton]
    public List<ABuffHandlerFactory> periodicBuff;
}

public class ApplyBuffPeriodicallySkill : ACooldownSkill<ApplyBuffPeriodicallySkillData>
{
    [ReadOnly]
    [SerializeField]
    int _currentBuff = 0;

    BuffManager _buffManager;

    void Start()
    {
        _buffManager = GetComponent<BuffManager>();
    }

    public override bool Execute(GameObject source)
    {
        _currentBuff++;
        if (_currentBuff >= data.periodicBuff.Count)
        {
            _currentBuff = 0;
        }

        _buffManager.AddHandler(data.periodicBuff[_currentBuff], gameObject, gameObject);
        return true;
    }

    public override float cooldownDuration => data.periodicBuff[_currentBuff].duration;
}
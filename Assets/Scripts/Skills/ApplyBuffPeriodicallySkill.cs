using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ApplyBuffPeriodicallySkillData : SkillDataBase
{
    public List<DurationBuffHandlerFactory> periodicBuff;
}

public class ApplyBuffPeriodicallySkill : ASkill<ApplyBuffPeriodicallySkillData>
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

    public override float cooldownDuration => data.periodicBuff[_currentBuff].data.duration;
}
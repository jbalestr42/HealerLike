using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShootProjectileSkillData : SkillDataBase
{
    public List<ProjectileData> projectiles;
}

public class ShootProjectileSkill : ACooldownSkill<ShootProjectileSkillData>, IAttackSkill
{
    Attribute _cooldownDuration;
    int _projectileIndex = 0;

    void Start()
    {
        requirements = new List<IRequirement>();
        requirements.Add(new TargetRequirement(gameObject));
        _cooldownDuration = GetComponent<AttributeManager>().Get(AttributeType.AttackRate);
        StaggerFirstShot();
    }

    // Called after each fight: the units staying for the next one stagger their first shot of it too
    public override void Reset()
    {
        base.Reset();
        StaggerFirstShot();
    }

    // The first shot of a fight waits a random part of the attack cooldown, so the units don't all shoot at once
    void StaggerFirstShot()
    {
        // Reset() is also called by the editor when the component is added, before Start()
        if (_cooldownDuration == null)
        {
            return;
        }

        DataManager dataManager = DataManager.existingInstance;
        if (StaggersFirstShot(dataManager != null ? dataManager.data : null))
        {
            InitCooldown(UnityEngine.Random.Range(0f, cooldownDuration));
        }
    }

    // Whether the units don't all shoot at once when a fight starts (GameData.staggerFirstAttacks, on without data)
    public static bool StaggersFirstShot(GameData data)
    {
        return data == null || data.staggerFirstAttacks;
    }

    public override bool Execute(GameObject source)
    {
        if (IsRequirementValidated())
        {
            ProjectileAttack.Shoot(source, data.projectiles[_projectileIndex]);
            _projectileIndex = (_projectileIndex + 1) % data.projectiles.Count;
            return true;
        }
        return false;
    }

    public override float cooldownDuration => _cooldownDuration.Value;

    #region IAttackSkill

    public bool attacksTargets => true;

    #endregion
}

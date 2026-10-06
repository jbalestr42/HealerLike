using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// Projectiles shot by an attack, at each of the current targets of the attacker
[Serializable]
public class ProjectileData
{
    [HorizontalGroup("Split", 75)]
    [PreviewField(75)]
    [HideLabel]
    [AssetsOnly]
    public GameObject projectilePrefab;

    [ListDrawerSettings(OnTitleBarGUI = "@GUIUtils.CreateDataButton<List<AConsumerFactory>, AConsumerFactory>(onHitConsumer)")]
    public List<AConsumerFactory> onHitConsumer;

    public int numberOfProjectileToShootPerTarget = 1;
}

// An attack of an entity shooting projectiles, reported to it through Entity.OnAttack
public class ProjectileAttack
{
    public GameObject source;
    public ProjectileData projectileData;
    // Projectiles of the last shot (e.g. to empower their hits)
    public List<Projectile> projectiles = new List<Projectile>();

    // Shoots the projectiles at every target of the source, reported as an attack
    public static ProjectileAttack Shoot(GameObject source, ProjectileData projectileData)
    {
        ProjectileAttack attack = new ProjectileAttack { source = source, projectileData = projectileData };
        attack.Fire();
        source.GetComponent<Entity>().OnAttack.Invoke(attack);
        return attack;
    }

    // Shoots the same projectiles again at the current targets, not reported as a new attack
    public virtual void Repeat()
    {
        if (source == null)
        {
            return;
        }

        Fire();
    }

    void Fire()
    {
        Entity entity = source.GetComponent<Entity>();
        projectiles.Clear();
        foreach (GameObject target in source.GetComponent<ITargetProvider>().GetTargets())
        {
            for (int i = 0; i < projectileData.numberOfProjectileToShootPerTarget; i++)
            {
                SkillSource skillSource = entity.skillStartPoint;
                skillSource.OnUseSkill();

                GameObject projectileGo = EntityManager.instance.SpawnProjectile(projectileData.projectilePrefab, skillSource.transform.position, Quaternion.identity);
                Projectile projectile = projectileGo.GetComponent<Projectile>();
                projectile.Init(source, target, entity.projectileBehaviours, projectileData.onHitConsumer);
                projectiles.Add(projectile);
            }
        }
    }
}

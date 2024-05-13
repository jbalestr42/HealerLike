using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ProjectileBehaviourBuff")]
public class ProjectileBehaviourBuffFactory : BuffFactory<ProjectileBehaviourBuff, ProjectileBehaviourBuffData> {}

[Serializable]
public class ProjectileBehaviourBuffData
{
    [CreateDataButton]
    public AProjectileBehaviourFactory projectileBehaviour;
}

public class ProjectileBehaviourBuff : ABuff<ProjectileBehaviourBuffData>, IStackableBuff
{
    AProjectileBehaviour _projectileBehaviourInstance;

    public override void Add(GameObject source, GameObject target)
    {
        _projectileBehaviourInstance = data.projectileBehaviour.AddBehaviour(target);
    }

    public override void Remove(GameObject source, GameObject target)
    {
        GameObject.Destroy(_projectileBehaviourInstance);
    }

    public override bool isStackable => _projectileBehaviourInstance is IStackableBuff;

    public void Stack(GameObject source, GameObject target)
    {
        // If the projectile behaviour isn't stackable, it has no effect
        IStackableBuff stackableBehaviour = _projectileBehaviourInstance as IStackableBuff;
        stackableBehaviour.Stack(source, target);
    }

    public void Unstack(GameObject source, GameObject target)
    {
        IStackableBuff stackableBehaviour = _projectileBehaviourInstance as IStackableBuff;
        stackableBehaviour.Unstack(source, target);
    }
}
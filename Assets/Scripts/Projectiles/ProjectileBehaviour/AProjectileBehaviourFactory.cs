using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
public abstract class AProjectileBehaviourFactory : SerializedScriptableObject
{
    public abstract AProjectileBehaviour AddBehaviour(GameObject target);
}

public class ProjectileBehaviourFactory<ProjectileBehaviourType, ProjectileBehaviourData> : AProjectileBehaviourFactory where ProjectileBehaviourType : AProjectileBehaviour<ProjectileBehaviourData>, new()
{
    [InlineProperty]
    [HideLabel]
    public ProjectileBehaviourData data;

    public override AProjectileBehaviour AddBehaviour(GameObject target)
    {
        ProjectileBehaviourType projectileBehaviour = target.AddComponent<ProjectileBehaviourType>();
        projectileBehaviour.data = data;
        return projectileBehaviour;
    }
}

public abstract class AProjectileBehaviour : MonoBehaviour
{
    Projectile _projectile;
    public Projectile projectile { get { return _projectile; } set { _projectile = value; } }

    public abstract void Init(GameObject source);
}

public abstract class AProjectileBehaviour<ProjectileBehaviourData> : AProjectileBehaviour
{
    [InlineProperty]
    [HideLabel]
    public ProjectileBehaviourData data;
}
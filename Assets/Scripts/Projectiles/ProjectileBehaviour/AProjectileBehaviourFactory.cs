using Oisif.Inspector;
using UnityEngine;

[InlineEditor]
public abstract class AProjectileBehaviourFactory : ScriptableObject
{
    public abstract AProjectileBehaviour AddBehaviour(GameObject target);
}

public class ProjectileBehaviourFactory<ProjectileBehaviourType, ProjectileBehaviourData> : AProjectileBehaviourFactory where ProjectileBehaviourType : AProjectileBehaviour<ProjectileBehaviourData>, new()
{
    [InlineProperty]
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
    public ProjectileBehaviourData data;
}
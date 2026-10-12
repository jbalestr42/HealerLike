public class CollidingProjectile : Projectile
{
    void Start()
    {
        OnCollisionEnter.AddListener(ApplyOnHit);
    }
}
namespace HealerLike.Render
{
    /// How an attack travels, authored per projectile prefab variant and read by the delivery observer.
    /// Frozen contract (wave 5). Values are stable; append only.
    public enum HLDeliveryStyle : byte
    {
        Direct = 0,     // liana extends straight to the target
        Arc = 1,        // liana arcs up and over (curved projectiles)
        Rigid = 2,      // a fast rigid rod, a laser
        Swarm = 3,      // several thin tendrils, one per shot
        Bounce = 4,     // re-aims between successive contacts
        ChainSync = 5,  // synchronous multi-tip, chain lightning
        Thrown = 6,     // a shard leaves the body and follows the projectile (stones)
    }
}

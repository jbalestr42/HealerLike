namespace HealerLike.Render.Grammar
{
    public enum LookSide
    {
        Plant,
        Stone
    }

    // How the primary skill delivers, the unit's main silhouette
    public enum HeadKind
    {
        Bud,
        Spear,
        Arch,
        Conductor,
        Fork,
        GiftHeal,
        GiftBoonDefence,
        GiftBoonOffence,
        GiftBane,
        Ward,
        Pulse,
        SelfTick
    }

    public enum CountBand
    {
        One,
        Few,
        Many
    }

    public enum StemBand
    {
        Quick,
        Steady,
        Slow
    }

    public enum MassBand
    {
        Light,
        Sturdy,
        Heavy
    }

    public enum ReachBand
    {
        Short,
        Mid,
        Long
    }

    public enum AccessoryKind
    {
        None,
        MiniHead,
        Hook,
        Antenna,
        ThornCollar,
        TierRings,
        TwinSeeds,
        StalkBeads,
        SmallTorus,
        ConeCrown,
        DripBeads,
        ShardBarbs
    }

    // Everything the look of a unit reads from its data, decided once at spawn
    public struct UnitChannels
    {
        public LookSide side;
        public HeadKind head;
        public CountBand count;
        public StemBand stem;
        public MassBand mass;
        public ReachBand reach;
        public AccessoryKind accessory;
        // The head drawn small when the accessory is a mini head
        public HeadKind accessoryHead;
        public EffectFamily accent;
    }
}

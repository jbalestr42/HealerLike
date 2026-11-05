using System.Collections.Generic;

// What IconPainter paints: for each data asset, its glyph, frame and colour, and where its icon is saved
public static class IconCatalog
{
    public enum Frame
    {
        // A status: a disc, like the spells
        Disc,
        // An item: a rounded square in the colour of its role
        Tile,
    }

    // What an item does, giving its tile's colour
    public enum Role
    {
        Damage,
        Speed,
        Defense,
        Life,
        // Around the holder: its neighbours get something
        Aura,
        // Plays with Blessed
        Holy,
        // Puts a status or a malus on the target
        Affliction,
        // On a death: rising, coming back, dying
        Death,
    }

    public static readonly Dictionary<Role, string> roleColors = new Dictionary<Role, string>
    {
        { Role.Damage, "#B04A3A" },
        { Role.Speed, "#C9772E" },
        { Role.Defense, "#3E6A8C" },
        { Role.Life, "#3A8C4E" },
        { Role.Aura, "#2E8C8C" },
        { Role.Holy, "#B8922E" },
        { Role.Affliction, "#7A5AA8" },
        { Role.Death, "#4E5566" },
    };

    public struct Entry
    {
        // The data the icon is set on (a status sets it on the buff putting it there)
        public string assetPath;
        // Under Assets/Icons, without the extension
        public string iconPath;
        public string glyph;
        public Frame frame;
        public string color;

        public Entry(string assetPath, string iconPath, string glyph, Frame frame, string color)
        {
            this.assetPath = assetPath;
            this.iconPath = iconPath;
            this.glyph = glyph;
            this.frame = frame;
            this.color = color;
        }

        public string iconAssetPath => "Assets/Icons/" + iconPath + ".png";
    }

    public static readonly List<Entry> entries = new List<Entry>
    {
        Status("Blessed", "halo", "#C9A23A"),
        Status("Blighted", "heartCracked", "#6A4A7A"),
        Status("Chilled", "snowflake", "#3F93AE"),
        Status("Empowered", "swordUp", "#B04A3A"),
        Status("Fortified", "shieldPlus", "#3E6A8C"),
        Status("Hasted", "chevrons", "#C9772E"),
        Status("Invulnerable", "bubble", "#7A6AB8"),
        Status("Poisoned", "drop", "#5E8C3A"),
        Status("Regenerating", "heartPlus", "#2E9C7C"),
        Status("Sundered", "shieldCracked", "#7A5A3A"),
        Status("Vulnerable", "reticle", "#9C3A62"),

        EntityItem("Adrenaline", "syringe", Role.Speed),
        EntityItem("Arc", "boltChain3", Role.Damage),
        EntityItem("Archon", "crown", Role.Damage),
        EntityItem("ArmorBreaker", "hammer", Role.Affliction),
        EntityItem("BalanceArc", "boltChain2", Role.Damage),
        EntityItem("BalanceBounce", "bounceScales", Role.Damage),
        EntityItem("BalanceMortarShell", "shellScales", Role.Damage),
        EntityItem("Banner", "banner", Role.Aura),
        EntityItem("Beacon", "beacon", Role.Aura),
        EntityItem("BlightAcolyte", "acolyteBlight", Role.Death),
        EntityItem("BloodBond", "bloodBond", Role.Life),
        EntityItem("BloodPrice", "bloodDagger", Role.Damage),
        EntityItem("BloodWard", "shieldDrop", Role.Aura),
        EntityItem("BoneCharm", "skull", Role.Death),
        EntityItem("BoostCell", "battery", Role.Aura),
        EntityItem("Bounce", "bounce", Role.Damage),
        EntityItem("BriarThorns", "briar", Role.Life),
        EntityItem("Carrion", "crow", Role.Death),
        EntityItem("Censer", "censer", Role.Holy),
        EntityItem("CharmOfMercy", "pendant", Role.Life),
        EntityItem("Conclave", "cycle", Role.Speed),
        EntityItem("CurrentHealthDamage", "heartBitten", Role.Damage),
        EntityItem("CursedIdol", "idol", Role.Damage),
        EntityItem("Echo", "echo", Role.Speed),
        EntityItem("Executioner", "axe", Role.Damage),
        EntityItem("ExecutionersEdge", "swordSpark", Role.Damage),
        EntityItem("ExplodeOnHit", "burst", Role.Damage),
        EntityItem("FaithfulGuard", "shieldHalo", Role.Holy),
        EntityItem("Frenzy", "claws", Role.Speed),
        EntityItem("FrostAcolyte", "acolyteFrost", Role.Death),
        EntityItem("Frost", "iceShard", Role.Affliction),
        EntityItem("GlassCannon", "cannon", Role.Damage),
        EntityItem("Gratitude", "heartSword", Role.Damage),
        EntityItem("GrowingSeed", "sprout", Role.Life),
        EntityItem("HairTrigger", "stopwatch", Role.Speed),
        EntityItem("Halo", "wings", Role.Holy),
        EntityItem("HardenedFoes", "helmet", Role.Defense),
        EntityItem("HeartStone", "heartGem", Role.Life),
        EntityItem("HexAcolyte", "acolyteHex", Role.Death),
        EntityItem("HolyWater", "flaskCross", Role.Holy),
        EntityItem("HuntersMark", "bow", Role.Affliction),
        EntityItem("IncreaseDamagePerHit", "bars", Role.Affliction),
        EntityItem("IncreaseDamageWithProjectileDistance", "arrowDistance", Role.Damage),
        EntityItem("IronPlating", "plate", Role.Defense),
        EntityItem("LoneWolf", "wolf", Role.Damage),
        EntityItem("LuckyCoin", "coin", Role.Damage),
        EntityItem("MartyrsHeart", "heartRays", Role.Aura),
        EntityItem("MissingHealthDamage", "heartEmpty", Role.Damage),
        EntityItem("MortarShell", "shellBurst", Role.Damage),
        EntityItem("MultipleShoot", "arrows2", Role.Damage),
        EntityItem("Pact", "scroll", Role.Defense),
        EntityItem("Phalanx", "shields3", Role.Defense),
        EntityItem("Phylactery", "phylactery", Role.Death),
        EntityItem("Plague", "plagueMask", Role.Affliction),
        EntityItem("Poison", "poisonVial", Role.Affliction),
        EntityItem("PunchingBagRegen", "punchingBag", Role.Life),
        EntityItem("Radiant", "sun", Role.Holy),
        EntityItem("Rage", "fire", Role.Damage),
        EntityItem("RegenHp", "herb", Role.Life),
        EntityItem("Sap", "tree", Role.Life),
        EntityItem("SecondWind", "wind", Role.Defense),
        EntityItem("SelfDestruct", "bomb", Role.Death),
        EntityItem("SharpenedBlades", "crossedSwords", Role.Damage),
        EntityItem("Siphon", "crystalDrain", Role.Affliction),
        EntityItem("Slow", "snail", Role.Affliction),
        EntityItem("SparkAcolyte", "acolyteSpark", Role.Death),
        EntityItem("Spark", "boltChain1", Role.Damage),
        EntityItem("Strangle", "noose", Role.Damage),
        EntityItem("SwiftGrace", "feather", Role.Holy),
        EntityItem("TauntTotem", "totem", Role.Defense),
        EntityItem("ThornsOfLife", "heartThorns", Role.Life),
        EntityItem("TitanFury", "hornedHelm", Role.Damage),
        EntityItem("TowerShield", "towerShield", Role.Defense),
        EntityItem("Trinity", "trinity", Role.Damage),
        EntityItem("TwinBlade", "twinBlades", Role.Death),
        EntityItem("Vengeance", "tombstone", Role.Speed),
        EntityItem("Venom", "fangs", Role.Affliction),
        EntityItem("Volley", "arrowsRain", Role.Damage),
        EntityItem("WarBanner", "warBanner", Role.Aura),
        EntityItem("Whetstone", "whetstone", Role.Damage),
        EntityItem("Zeal", "fist", Role.Holy),
    };

    static Entry Status(string name, string glyph, string color)
    {
        return new Entry($"Assets/Data/Statuses/{name}Status/{name}Status.asset", "Statuses/" + name, glyph, Frame.Disc, color);
    }

    static Entry EntityItem(string name, string glyph, Role role)
    {
        return new Entry($"Assets/Data/EntityItems/{name}Item/{name}Item.asset", "Items/" + name, glyph, Frame.Tile, roleColors[role]);
    }

    public static IconCanvas Paint(Entry entry, int size)
    {
        var canvas = new IconCanvas(size);
        IconColor color = IconColor.Hex(entry.color);
        IconPen pen = entry.frame == Frame.Tile ? IconFrames.Tile(canvas, color) : IconFrames.Disc(canvas, color);
        IconGlyphs.all[entry.glyph](pen);
        return canvas;
    }
}

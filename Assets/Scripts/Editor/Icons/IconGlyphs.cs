using System;
using System.Collections.Generic;

// The pictograms of the painted icons, by name (see IconCatalog), each drawn in the 0-100 box of its pen (the items'
// ones in IconGlyphs.Items and IconGlyphs.PlayerItems, the bosses' effects in IconGlyphs.Effects)
public static partial class IconGlyphs
{
    public static readonly Dictionary<string, Action<IconPen>> all = new Dictionary<string, Action<IconPen>>
    {
        { "halo", Halo },
        { "heartCracked", HeartCracked },
        { "heartPlus", HeartPlus },
        { "snowflake", Snowflake },
        { "swordUp", SwordUp },
        { "shieldPlus", ShieldPlus },
        { "shieldCracked", ShieldCracked },
        { "chevrons", Chevrons },
        { "bubble", Bubble },
        { "drop", Drop },
        { "reticle", Reticle },

        { "syringe", Syringe },
        { "boltChain3", BoltChain3 },
        { "boltChain2", BoltChain2 },
        { "crown", Crown },
        { "hammer", Hammer },
        { "bounceScales", BounceScales },
        { "shellScales", ShellScales },
        { "banner", Banner },
        { "beacon", Beacon },
        { "acolyteBlight", AcolyteBlight },
        { "bloodBond", BloodBond },
        { "bloodDagger", BloodDagger },
        { "shieldDrop", ShieldDrop },
        { "skull", Skull },
        { "battery", Battery },
        { "bounce", Bounce },
        { "briar", Briar },
        { "crow", Crow },
        { "censer", Censer },
        { "pendant", Pendant },
        { "cycle", Cycle },
        { "heartBitten", HeartBitten },
        { "idol", Idol },
        { "echo", Echo },
        { "axe", Axe },
        { "swordSpark", SwordSpark },
        { "burst", Burst },
        { "shieldHalo", ShieldHalo },
        { "claws", Claws },
        { "acolyteFrost", AcolyteFrost },
        { "acolyteHex", AcolyteHex },
        { "acolyteSpark", AcolyteSpark },
        { "iceShard", IceShard },
        { "cannon", Cannon },
        { "heartSword", HeartSword },
        { "sprout", Sprout },
        { "stopwatch", Stopwatch },
        { "wings", Wings },
        { "helmet", Helmet },
        { "heartGem", HeartGem },
        { "flaskCross", FlaskCross },
        { "bow", Bow },
        { "bars", Bars },
        { "arrowDistance", ArrowDistance },
        { "plate", Plate },
        { "wolf", Wolf },
        { "coin", Coin },
        { "heartRays", HeartRays },
        { "heartEmpty", HeartEmpty },
        { "shellBurst", ShellBurst },
        { "arrows2", Arrows2 },
        { "arrowsRain", ArrowsRain },
        { "scroll", Scroll },
        { "shields3", Shields3 },
        { "phylactery", Phylactery },
        { "plagueMask", PlagueMask },
        { "poisonVial", PoisonVial },
        { "punchingBag", PunchingBag },
        { "sun", Sun },
        { "fire", Fire },
        { "herb", Herb },
        { "tree", Tree },
        { "wind", Wind },
        { "bomb", Bomb },
        { "crossedSwords", CrossedSwords },
        { "whetstone", Whetstone },
        { "crystalDrain", CrystalDrain },
        { "snail", Snail },
        { "boltChain1", BoltChain1 },
        { "noose", Noose },
        { "feather", Feather },
        { "totem", Totem },
        { "heartThorns", HeartThorns },
        { "hornedHelm", HornedHelm },
        { "towerShield", TowerShield },
        { "trinity", Trinity },
        { "twinBlades", TwinBlades },
        { "tombstone", Tombstone },
        { "fangs", Fangs },
        { "warBanner", WarBanner },
        { "fist", Fist },

        { "dove", Dove },
        { "chalice", Chalice },
        { "crystalUp", CrystalUp },
        { "ghost", Ghost },
        { "crystalEmpty", CrystalEmpty },
        { "bell", Bell },
        { "candleOut", CandleOut },
        { "moonHeart", MoonHeart },
        { "staff", Staff },
        { "crystal", Crystal },
        { "hourglass", Hourglass },
        { "bookCoin", BookCoin },
        { "bookSkull", BookSkull },
        { "fountain", Fountain },
        { "beads", Beads },
        { "lantern", Lantern },
        { "campfire", Campfire },
        { "rosary", Rosary },
        { "offering", Offering },
        { "flower", Flower },
        { "drum", Drum },
        { "well", Well },
        { "runeBrand", RuneBrand },
        { "bookEye", BookEye },
        { "bookDrop", BookDrop },
        { "scythe", Scythe },
        { "bookCross", BookCross },
        { "drumstick", Drumstick },
        { "bookWings", BookWings },
        { "maskTeeth", MaskTeeth },
        { "leech", Leech },
        { "coinSkull", CoinSkull },
        { "scrollCoin", ScrollCoin },
        { "thornCrown", ThornCrown },
        { "bookChevrons", BookChevrons },
        { "quill", Quill },

        { "scales", Scales },
        { "swordDown", SwordDown },
        { "bolt", Bolt },
        { "groundSlam", GroundSlam },
        { "strikes", Strikes },
        { "shieldBlock", ShieldBlock },
    };

    // Blessed: a halo over a sparkle
    static void Halo(IconPen pen)
    {
        pen.EllipseRing(pen.main, 50f, 20f, 32f, 11f, 7f);
        Sparkle(pen, pen.main, 48f, 64f, 32f, 9f);
        Sparkle(pen, pen.accent, 80f, 40f, 12f, 4f);
    }

    // Blighted: a heart split by a crack
    static void HeartCracked(IconPen pen)
    {
        Heart(pen, pen.main);
        pen.Poly(pen.back, 50f, 30f, 57f, 30f, 47f, 50f, 60f, 60f, 50f, 86f, 52f, 64f, 40f, 52f);
    }

    // Regenerating: a heart with a cross
    static void HeartPlus(IconPen pen)
    {
        Heart(pen, pen.main);
        pen.Rect(pen.accent, 43f, 36f, 14f, 36f, 3f);
        pen.Rect(pen.accent, 32f, 47f, 36f, 14f, 3f);
    }

    // Chilled: a six-armed flake
    static void Snowflake(IconPen pen)
    {
        for (int arm = 0; arm < 6; arm++)
        {
            double angle = arm * System.Math.PI / 3 - System.Math.PI / 2;
            float dx = (float)System.Math.Cos(angle);
            float dy = (float)System.Math.Sin(angle);
            pen.Line(pen.main, 50f, 50f, 50f + dx * 44f, 50f + dy * 44f, 9f);
            // A V near the tip
            float bx = 50f + dx * 28f;
            float by = 50f + dy * 28f;
            foreach (double side in new[] { -0.8, 0.8 })
            {
                float sx = (float)System.Math.Cos(angle + side);
                float sy = (float)System.Math.Sin(angle + side);
                pen.Line(pen.main, bx, by, bx + sx * 15f, by + sy * 15f, 7f);
            }
        }
        pen.Circle(pen.accent, 50f, 50f, 10f);
    }

    // Empowered: a sword raised
    static void SwordUp(IconPen pen)
    {
        pen.Poly(pen.main, 50f, 2f, 64f, 16f, 64f, 64f, 36f, 64f, 36f, 16f);
        pen.Poly(pen.accent, 50f, 2f, 64f, 16f, 64f, 64f, 50f, 64f);
        pen.Rect(pen.main, 22f, 61f, 56f, 12f, 6f);
        pen.Rect(pen.main, 43f, 70f, 14f, 18f);
        pen.Circle(pen.main, 50f, 90f, 8f);
    }

    // Fortified: a shield with a cross
    static void ShieldPlus(IconPen pen)
    {
        Shield(pen, pen.main, pen.accent);
        pen.Rect(pen.back, 44f, 30f, 12f, 38f, 3f);
        pen.Rect(pen.back, 31f, 43f, 38f, 12f, 3f);
    }

    // Sundered: a shield split by a crack
    static void ShieldCracked(IconPen pen)
    {
        Shield(pen, pen.main, pen.accent);
        pen.Poly(pen.back, 46f, 6f, 55f, 6f, 46f, 34f, 60f, 48f, 47f, 70f, 52f, 94f, 42f, 70f, 51f, 50f, 37f, 36f);
    }

    // Hasted: two chevrons onward
    static void Chevrons(IconPen pen)
    {
        pen.Poly(pen.accent, 6f, 14f, 28f, 14f, 58f, 50f, 28f, 86f, 6f, 86f, 36f, 50f);
        pen.Poly(pen.main, 40f, 14f, 62f, 14f, 92f, 50f, 62f, 86f, 40f, 86f, 70f, 50f);
    }

    // Invulnerable: a shield inside a bubble
    static void Bubble(IconPen pen)
    {
        pen.Ring(pen.main, 50f, 50f, 49f, 8f);
        pen.Line(pen.accent, 18f, 36f, 30f, 18f, 6f);
        Shield(pen, pen.main, pen.accent, 50f, 52f, 0.66f);
    }

    // Poisoned: a drop with bubbles
    static void Drop(IconPen pen)
    {
        pen.Poly(pen.main, 50f, 4f, 76f, 50f, 24f, 50f);
        pen.Circle(pen.main, 50f, 62f, 28f);
        pen.Circle(pen.back, 41f, 66f, 7f);
        pen.Circle(pen.back, 60f, 76f, 5f);
        pen.Circle(pen.back, 58f, 54f, 4f);
    }

    // Vulnerable: a target's reticle
    static void Reticle(IconPen pen)
    {
        pen.Ring(pen.main, 50f, 50f, 32f, 8f);
        pen.Line(pen.main, 50f, 4f, 50f, 26f, 9f);
        pen.Line(pen.main, 50f, 74f, 50f, 96f, 9f);
        pen.Line(pen.main, 4f, 50f, 26f, 50f, 9f);
        pen.Line(pen.main, 74f, 50f, 96f, 50f, 9f);
        pen.Circle(pen.accent, 50f, 50f, 9f);
    }

    // The heart filling the box
    static void Heart(IconPen pen, IconColor color)
    {
        pen.Curve(color, t =>
        {
            double a = t * 2 * System.Math.PI;
            double x = 16 * System.Math.Pow(System.Math.Sin(a), 3);
            double y = -(13 * System.Math.Cos(a) - 5 * System.Math.Cos(2 * a) - 2 * System.Math.Cos(3 * a) - System.Math.Cos(4 * a));
            return (50f + (float)x * 2.8f, 46f + (float)y * 2.8f);
        });
    }

    // A shield, its right half in the shade, centred on (cx, cy) at a scale of the box
    static void Shield(IconPen pen, IconColor color, IconColor shade, float cx = 50f, float cy = 50f, float scale = 1f)
    {
        float X(float x) => cx + (x - 50f) * scale;
        float Y(float y) => cy + (y - 50f) * scale;
        pen.Poly(color, X(50f), Y(6f), X(86f), Y(18f), X(82f), Y(58f), X(50f), Y(94f), X(18f), Y(58f), X(14f), Y(18f));
        pen.Poly(shade, X(50f), Y(6f), X(86f), Y(18f), X(82f), Y(58f), X(50f), Y(94f));
    }

    // A four-pointed star
    static void Sparkle(IconPen pen, IconColor color, float cx, float cy, float radius, float waist)
    {
        pen.Poly(color, cx, cy - radius, cx + waist, cy - waist, cx + radius, cy, cx + waist, cy + waist,
            cx, cy + radius, cx - waist, cy + waist, cx - radius, cy, cx - waist, cy - waist);
    }
}

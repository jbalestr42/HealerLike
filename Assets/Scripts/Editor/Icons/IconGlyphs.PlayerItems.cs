// The pictograms of the player's and the events' items (see IconCatalog), each in the 0-100 box of its pen
public static partial class IconGlyphs
{
    // Blessing: a dove, its wings up
    static void Dove(IconPen pen)
    {
        pen.Ellipse(pen.main, 46f, 62f, 30f, 16f);
        pen.Circle(pen.main, 76f, 48f, 12f);
        pen.Poly(pen.main, 86f, 44f, 98f, 50f, 86f, 54f);
        pen.Poly(pen.main, 6f, 58f, 24f, 62f, 10f, 78f);
        pen.Poly(pen.accent, 30f, 56f, 18f, 10f, 44f, 26f, 58f, 56f);
        pen.Poly(pen.main, 40f, 56f, 44f, 6f, 64f, 30f, 66f, 56f);
        pen.Circle(pen.back, 79f, 46f, 3f);
    }

    // Chalice of Plenty: a cup brimming over
    static void Chalice(IconPen pen)
    {
        pen.Poly(pen.main, 16f, 20f, 84f, 20f, 76f, 46f, 60f, 58f, 40f, 58f, 24f, 46f);
        pen.Rect(pen.main, 44f, 56f, 12f, 24f);
        pen.Rect(pen.main, 26f, 80f, 48f, 12f, 5f);
        pen.Ellipse(pen.accent, 50f, 20f, 34f, 8f);
        DropShape(pen, pen.accent, 84f, 34f, 0.28f);
        DropShape(pen, pen.accent, 16f, 40f, 0.22f);
    }

    // A crystal, its facets shaded
    static void CrystalShape(IconPen pen, IconColor color, IconColor shade, float cx, float cy, float scale)
    {
        float X(float x) => cx + (x - 50f) * scale;
        float Y(float y) => cy + (y - 50f) * scale;
        pen.Poly(color, X(50f), Y(4f), X(78f), Y(36f), X(50f), Y(96f), X(22f), Y(36f));
        pen.Poly(shade, X(50f), Y(4f), X(78f), Y(36f), X(50f), Y(96f));
        pen.Poly(shade, X(22f), Y(36f), X(78f), Y(36f), X(74f), Y(40f), X(26f), Y(40f));
    }

    // Costly Spells: a crystal with an arrow up, the price rising
    static void CrystalUp(IconPen pen)
    {
        CrystalShape(pen, pen.main, pen.accent, 40f, 52f, 0.9f);
        pen.Rect(pen.main, 76f, 34f, 10f, 56f, 3f);
        pen.Poly(pen.main, 64f, 38f, 81f, 10f, 98f, 38f);
    }

    // Damage All Enemies: a wailing ghost
    static void Ghost(IconPen pen)
    {
        pen.Circle(pen.main, 50f, 38f, 30f);
        pen.Rect(pen.main, 20f, 38f, 60f, 42f);
        pen.Poly(pen.main, 20f, 78f, 20f, 96f, 34f, 84f, 42f, 96f, 50f, 84f, 58f, 96f, 66f, 84f, 80f, 96f, 80f, 78f);
        pen.Ellipse(pen.back, 39f, 36f, 6f, 9f);
        pen.Ellipse(pen.back, 61f, 36f, 6f, 9f);
        pen.Ellipse(pen.accent, 50f, 60f, 8f, 11f);
    }

    // Drained: an empty crystal, only its outline left
    static void CrystalEmpty(IconPen pen)
    {
        CrystalShape(pen, pen.main, pen.accent, 50f, 50f, 1f);
        CrystalShape(pen, pen.back, pen.back, 50f, 52f, 0.62f);
        pen.Poly(pen.accent, 44f, 74f, 56f, 74f, 50f, 84f);
    }

    // Emergency Beacon: a ringing bell
    static void Bell(IconPen pen)
    {
        pen.Rect(pen.main, 44f, 6f, 12f, 12f, 5f);
        pen.Poly(pen.main, 50f, 14f, 70f, 24f, 74f, 64f, 86f, 78f, 14f, 78f, 26f, 64f, 30f, 24f);
        pen.Circle(pen.main, 50f, 18f + 6f, 20f);
        pen.Circle(pen.accent, 50f, 88f, 9f);
        ArcBand(pen, pen.accent, 50f, 50f, 46f, 6f, -40f, -10f);
        ArcBand(pen, pen.accent, 50f, 50f, 46f, 6f, 190f, 220f);
    }

    // Faltering Faith: a candle snuffed, its smoke rising
    static void CandleOut(IconPen pen)
    {
        pen.Rect(pen.main, 34f, 44f, 32f, 48f, 4f);
        pen.Rect(pen.main, 24f, 86f, 52f, 10f, 4f);
        pen.Ellipse(pen.accent, 50f, 44f, 16f, 5f);
        pen.Rect(pen.back, 48f, 32f, 4f, 12f);
        pen.Ring(pen.accent, 56f, 22f, 7f, 4f);
        pen.Ring(pen.accent, 44f, 10f, 6f, 4f);
    }

    // Heal All Entities On Round End: a crescent moon over a heart
    static void MoonHeart(IconPen pen)
    {
        pen.Circle(pen.accent, 40f, 34f, 30f);
        pen.Circle(pen.back, 54f, 26f, 26f);
        HeartAt(pen, pen.main, 62f, 64f, 1.6f);
    }

    // Heal Power: a staff topped by a cross
    static void Staff(IconPen pen)
    {
        pen.Line(pen.main, 26f, 94f, 62f, 38f, 10f);
        pen.Ring(pen.main, 68f, 28f, 18f, 7f);
        pen.Rect(pen.accent, 64f, 16f, 8f, 24f, 2f);
        pen.Rect(pen.accent, 56f, 24f, 24f, 8f, 2f);
    }

    // Mana Crystal: a big crystal
    static void Crystal(IconPen pen)
    {
        CrystalShape(pen, pen.main, pen.accent, 50f, 50f, 1f);
        Sparkle(pen, pen.accent, 84f, 18f, 12f, 4f);
    }

    // Mana On Round End: an hourglass, its sand a mana blue
    static void Hourglass(IconPen pen)
    {
        pen.Rect(pen.main, 18f, 4f, 64f, 10f, 4f);
        pen.Rect(pen.main, 18f, 86f, 64f, 10f, 4f);
        pen.Poly(pen.main, 26f, 14f, 74f, 14f, 54f, 50f, 74f, 86f, 26f, 86f, 46f, 50f);
        pen.Poly(pen.back, 32f, 18f, 68f, 18f, 52f, 48f, 48f, 48f);
        pen.Poly(pen.accent, 40f, 30f, 60f, 30f, 52f, 46f, 48f, 46f);
        pen.Poly(pen.accent, 50f, 62f, 70f, 82f, 30f, 82f);
    }

    // A closed book, its spine on the left, room in the middle of its cover for its emblem
    static void Book(IconPen pen)
    {
        pen.Rect(pen.main, 18f, 6f, 66f, 88f, 6f);
        pen.Rect(pen.accent, 18f, 6f, 12f, 88f, 4f);
        pen.Rect(pen.accent, 76f, 10f, 4f, 80f);
    }

    // Merchant's Ledger: a book with a coin
    static void BookCoin(IconPen pen)
    {
        Book(pen);
        pen.Circle(pen.back, 54f, 48f, 18f);
        pen.Ring(pen.accent, 54f, 48f, 12f, 4f);
        pen.Rect(pen.accent, 52f, 40f, 4f, 16f);
    }

    // Necronomicon: a book with a skull
    static void BookSkull(IconPen pen)
    {
        Book(pen);
        pen.Circle(pen.back, 54f, 44f, 17f);
        pen.Rect(pen.back, 45f, 50f, 18f, 16f, 3f);
        pen.Circle(pen.main, 48f, 44f, 5f);
        pen.Circle(pen.main, 60f, 44f, 5f);
        pen.Rect(pen.main, 52f, 58f, 4f, 8f);
    }

    // Overflowing Font: a basin with a jet falling back
    static void Fountain(IconPen pen)
    {
        pen.Poly(pen.main, 8f, 62f, 92f, 62f, 80f, 84f, 20f, 84f);
        pen.Rect(pen.main, 40f, 82f, 20f, 8f);
        pen.Rect(pen.main, 28f, 88f, 44f, 8f, 3f);
        pen.Rect(pen.accent, 46f, 26f, 8f, 36f, 3f);
        ArcBand(pen, pen.accent, 34f, 30f, 14f, 6f, 180f, 360f);
        ArcBand(pen, pen.accent, 66f, 30f, 14f, 6f, 180f, 360f);
        DropShape(pen, pen.accent, 20f, 46f, 0.2f);
        DropShape(pen, pen.accent, 80f, 46f, 0.2f);
        DropShape(pen, pen.accent, 50f, 14f, 0.24f);
    }

    // Prayer Beads: a string of beads, one bigger
    static void Beads(IconPen pen)
    {
        pen.EllipseRing(pen.accent, 50f, 44f, 34f, 32f, 3f);
        for (int i = 0; i < 10; i++)
        {
            double a = i * System.Math.PI / 5 - System.Math.PI / 2;
            pen.Circle(pen.main, 50f + (float)System.Math.Cos(a) * 34f, 44f + (float)System.Math.Sin(a) * 32f, 7f);
        }
        pen.Line(pen.main, 50f, 76f, 50f, 88f, 4f);
        pen.Circle(pen.accent, 50f, 90f, 8f);
    }

    // Relic of Light: a lantern shining
    static void Lantern(IconPen pen)
    {
        pen.Ring(pen.main, 50f, 12f, 8f, 4f);
        pen.Poly(pen.main, 30f, 20f, 70f, 20f, 76f, 30f, 24f, 30f);
        pen.Rect(pen.main, 28f, 30f, 44f, 52f, 6f);
        pen.Rect(pen.main, 22f, 82f, 56f, 12f, 4f);
        pen.Rect(pen.accent, 36f, 38f, 28f, 36f, 4f);
        Flame(pen, pen.main, 50f, 56f, 0.6f);
        pen.Line(pen.accent, 6f, 56f, 16f, 56f, 5f);
        pen.Line(pen.accent, 84f, 56f, 94f, 56f, 5f);
        pen.Line(pen.accent, 10f, 34f, 18f, 40f, 5f);
        pen.Line(pen.accent, 90f, 34f, 82f, 40f, 5f);
    }

    // Restless: a camp fire gone out, no rest by it
    static void Campfire(IconPen pen)
    {
        pen.Line(pen.main, 14f, 86f, 86f, 66f, 11f);
        pen.Line(pen.main, 14f, 66f, 86f, 86f, 11f);
        pen.Ring(pen.accent, 50f, 46f, 9f, 5f);
        pen.Ring(pen.accent, 40f, 26f, 8f, 5f);
        pen.Ring(pen.accent, 54f, 8f, 6f, 4f);
        pen.Line(pen.back, 6f, 94f, 94f, 58f, 5f);
    }

    // Rosary: beads hanging to a cross
    static void Rosary(IconPen pen)
    {
        for (int i = 0; i < 8; i++)
        {
            double a = System.Math.PI * (0.85 + i * 1.3 / 7);
            pen.Circle(pen.main, 50f + (float)System.Math.Cos(a) * 34f, 38f + (float)System.Math.Sin(a) * 30f, 6f);
        }
        pen.Line(pen.accent, 22f, 52f, 50f, 64f, 3f);
        pen.Line(pen.accent, 78f, 52f, 50f, 64f, 3f);
        pen.Rect(pen.main, 45f, 62f, 10f, 34f, 2f);
        pen.Rect(pen.main, 36f, 70f, 28f, 9f, 2f);
    }

    // Tithe: a bowl of offering, coins in it
    static void Offering(IconPen pen)
    {
        pen.Circle(pen.accent, 36f, 42f, 13f);
        pen.Circle(pen.accent, 62f, 38f, 13f);
        pen.Circle(pen.accent, 50f, 22f, 12f);
        pen.Ring(pen.back, 36f, 42f, 8f, 3f);
        pen.Ring(pen.back, 62f, 38f, 8f, 3f);
        pen.Ring(pen.back, 50f, 22f, 7f, 3f);
        pen.Poly(pen.main, 6f, 50f, 94f, 50f, 80f, 80f, 20f, 80f);
        pen.Rect(pen.main, 32f, 80f, 36f, 14f, 4f);
    }

    // Verdant: a flower and its leaves
    static void Flower(IconPen pen)
    {
        pen.Line(pen.main, 50f, 50f, 50f, 96f, 7f);
        Leaf(pen, pen.main, 50f, 82f, 18f, 62f, 18f);
        Leaf(pen, pen.main, 50f, 74f, 82f, 56f, 18f);
        for (int i = 0; i < 6; i++)
        {
            double a = i * System.Math.PI / 3;
            pen.Circle(pen.main, 50f + (float)System.Math.Cos(a) * 20f, 32f + (float)System.Math.Sin(a) * 20f, 13f);
        }
        pen.Circle(pen.accent, 50f, 32f, 12f);
    }

    // War Drums: a drum and its sticks
    static void Drum(IconPen pen)
    {
        pen.Rect(pen.main, 14f, 40f, 72f, 46f, 4f);
        pen.Ellipse(pen.main, 50f, 86f, 36f, 10f);
        pen.Ellipse(pen.accent, 50f, 40f, 36f, 11f);
        pen.Line(pen.accent, 18f, 52f, 34f, 82f, 4f);
        pen.Line(pen.accent, 34f, 82f, 50f, 54f, 4f);
        pen.Line(pen.accent, 50f, 54f, 66f, 84f, 4f);
        pen.Line(pen.accent, 66f, 84f, 82f, 52f, 4f);
        pen.Line(pen.main, 22f, 4f, 44f, 34f, 6f);
        pen.Line(pen.main, 78f, 4f, 56f, 34f, 6f);
        pen.Circle(pen.main, 22f, 4f + 2f, 6f);
        pen.Circle(pen.main, 78f, 4f + 2f, 6f);
    }

    // Abyssal Well: a well over a dark mana pool
    static void Well(IconPen pen)
    {
        pen.Rect(pen.main, 16f, 8f, 68f, 10f, 3f);
        pen.Rect(pen.main, 20f, 16f, 8f, 40f);
        pen.Rect(pen.main, 72f, 16f, 8f, 40f);
        pen.Line(pen.accent, 50f, 18f, 50f, 40f, 3f);
        pen.Rect(pen.accent, 44f, 40f, 12f, 12f, 2f);
        pen.Ellipse(pen.accent, 50f, 60f, 40f, 9f);
        pen.Rect(pen.main, 10f, 60f, 80f, 34f, 4f);
        pen.Line(pen.back, 10f, 76f, 90f, 76f, 3f);
        pen.Line(pen.back, 36f, 60f, 36f, 76f, 3f);
        pen.Line(pen.back, 64f, 60f, 64f, 76f, 3f);
        pen.Line(pen.back, 50f, 76f, 50f, 94f, 3f);
    }

    // Berserker's Brand: a branded rune in a flame
    static void RuneBrand(IconPen pen)
    {
        Flame(pen, pen.main, 50f, 58f, 1.9f);
        pen.Line(pen.back, 50f, 40f, 50f, 88f, 6f);
        pen.Line(pen.back, 50f, 52f, 64f, 40f, 6f);
        pen.Line(pen.back, 50f, 64f, 36f, 52f, 6f);
        pen.Line(pen.back, 50f, 74f, 64f, 62f, 6f);
    }

    // Black Codex: a book with an open eye
    static void BookEye(IconPen pen)
    {
        Book(pen);
        pen.Ellipse(pen.back, 54f, 48f, 22f, 12f);
        pen.Circle(pen.main, 54f, 48f, 9f);
        pen.Circle(pen.back, 54f, 48f, 4f);
    }

    // Blood Ledger: a book with a drop of blood
    static void BookDrop(IconPen pen)
    {
        Book(pen);
        DropShape(pen, pen.back, 54f, 46f, 0.5f);
    }

    // Bloodthirst Blade: a scythe, its blade dripping
    static void Scythe(IconPen pen)
    {
        pen.Line(pen.main, 30f, 96f, 62f, 8f, 8f);
        pen.Poly(pen.main, 60f, 8f, 66f, 14f, 38f, 18f, 10f, 34f, 22f, 16f, 42f, 6f);
        pen.Poly(pen.accent, 66f, 14f, 38f, 18f, 10f, 34f, 34f, 22f);
        pen.Line(pen.main, 30f, 70f, 46f, 76f, 6f);
        DropShape(pen, pen.accent, 14f, 52f, 0.2f);
        DropShape(pen, pen.accent, 24f, 66f, 0.16f);
    }

    // Codex of Mending: a book with a cross
    static void BookCross(IconPen pen)
    {
        Book(pen);
        pen.Rect(pen.back, 48f, 30f, 12f, 36f, 2f);
        pen.Rect(pen.back, 36f, 42f, 36f, 12f, 2f);
    }

    // Glutton's Charm: a drumstick
    static void Drumstick(IconPen pen)
    {
        pen.Ellipse(pen.main, 40f, 40f, 30f, 26f);
        pen.Poly(pen.main, 40f, 52f, 60f, 32f, 74f, 66f, 66f, 74f);
        pen.Line(pen.accent, 66f, 66f, 84f, 84f, 9f);
        pen.Circle(pen.accent, 88f, 80f, 7f);
        pen.Circle(pen.accent, 80f, 90f, 7f);
        pen.Ellipse(pen.accent, 30f, 30f, 10f, 6f);
    }

    // Hasty Grimoire: a book with wings
    static void BookWings(IconPen pen)
    {
        pen.Poly(pen.accent, 30f, 30f, 2f, 14f, 8f, 34f, 2f, 40f, 12f, 52f, 6f, 60f, 30f, 64f);
        pen.Poly(pen.accent, 70f, 30f, 98f, 14f, 92f, 34f, 98f, 40f, 88f, 52f, 94f, 60f, 70f, 64f);
        pen.Rect(pen.main, 26f, 14f, 48f, 72f, 5f);
        pen.Rect(pen.back, 26f, 14f, 8f, 72f, 3f);
        pen.Poly(pen.back, 46f, 36f, 58f, 50f, 46f, 64f);
    }

    // Hungering Mask: a mask baring its teeth
    static void MaskTeeth(IconPen pen)
    {
        pen.Poly(pen.main, 14f, 10f, 86f, 10f, 90f, 52f, 70f, 92f, 30f, 92f, 10f, 52f);
        pen.Poly(pen.back, 22f, 32f, 44f, 38f, 42f, 46f, 26f, 44f);
        pen.Poly(pen.back, 78f, 32f, 56f, 38f, 58f, 46f, 74f, 44f);
        pen.Ellipse(pen.back, 50f, 70f, 22f, 12f);
        pen.Poly(pen.main, 30f, 62f, 70f, 62f, 66f, 66f, 62f, 72f, 58f, 66f, 54f, 72f, 50f, 66f, 46f, 72f, 42f, 66f, 38f, 72f, 34f, 66f);
        pen.Poly(pen.main, 32f, 78f, 36f, 72f, 40f, 78f, 44f, 72f, 48f, 78f, 52f, 72f, 56f, 78f, 60f, 72f, 64f, 78f, 68f, 78f, 50f, 84f);
    }

    // Leech Fang: a leech, curled
    static void Leech(IconPen pen)
    {
        ArcBand(pen, pen.main, 50f, 50f, 30f, 22f, 150f, 420f);
        pen.Circle(pen.main, 50f + (float)System.Math.Cos(150 * System.Math.PI / 180) * 30f, 50f + (float)System.Math.Sin(150 * System.Math.PI / 180) * 30f, 11f);
        pen.Circle(pen.accent, 50f + (float)System.Math.Cos(60 * System.Math.PI / 180) * 30f, 50f + (float)System.Math.Sin(60 * System.Math.PI / 180) * 30f, 13f);
        pen.Circle(pen.back, 50f + (float)System.Math.Cos(60 * System.Math.PI / 180) * 30f, 50f + (float)System.Math.Sin(60 * System.Math.PI / 180) * 30f, 6f);
        for (int i = 0; i < 4; i++)
        {
            double a = (190 + i * 50) * System.Math.PI / 180;
            pen.Line(pen.back, 50f + (float)System.Math.Cos(a) * 20f, 50f + (float)System.Math.Sin(a) * 20f,
                50f + (float)System.Math.Cos(a) * 40f, 50f + (float)System.Math.Sin(a) * 40f, 3f);
        }
    }

    // Reaper's Coin: a coin stamped with a skull
    static void CoinSkull(IconPen pen)
    {
        pen.Circle(pen.main, 50f, 50f, 44f);
        pen.Ring(pen.accent, 50f, 50f, 36f, 4f);
        pen.Circle(pen.back, 50f, 44f, 17f);
        pen.Rect(pen.back, 41f, 50f, 18f, 16f, 3f);
        pen.Circle(pen.main, 44f, 44f, 5f);
        pen.Circle(pen.main, 56f, 44f, 5f);
        pen.Rect(pen.main, 48f, 58f, 4f, 8f);
    }

    // Scroll of Thrift: a scroll and a coin
    static void ScrollCoin(IconPen pen)
    {
        pen.Rect(pen.main, 14f, 10f, 56f, 76f, 4f);
        pen.Rect(pen.accent, 8f, 4f, 68f, 12f, 6f);
        pen.Rect(pen.accent, 8f, 80f, 68f, 12f, 6f);
        pen.Rect(pen.back, 24f, 28f, 36f, 4f);
        pen.Rect(pen.back, 24f, 40f, 30f, 4f);
        pen.Rect(pen.back, 24f, 52f, 34f, 4f);
        pen.Circle(pen.back, 72f, 70f, 24f);
        pen.Circle(pen.main, 72f, 70f, 20f);
        pen.Ring(pen.accent, 72f, 70f, 13f, 4f);
    }

    // Thorned Crown: a crown of thorns
    static void ThornCrown(IconPen pen)
    {
        pen.EllipseRing(pen.main, 50f, 58f, 40f, 20f, 10f);
        for (int i = 0; i < 9; i++)
        {
            double a = i * 2 * System.Math.PI / 9;
            float x = 50f + (float)System.Math.Cos(a) * 40f;
            float y = 58f + (float)System.Math.Sin(a) * 20f;
            float dx = (float)System.Math.Cos(a + 0.6);
            float dy = (float)System.Math.Sin(a + 0.6) - 0.6f;
            pen.Poly(pen.accent, x - 3f, y, x + 3f, y, x + dx * 16f, y + dy * 16f);
        }
        pen.Line(pen.accent, 16f, 56f, 84f, 60f, 3f);
    }

    // Tome of Haste: a book with chevrons
    static void BookChevrons(IconPen pen)
    {
        Book(pen);
        pen.Poly(pen.back, 36f, 30f, 46f, 30f, 60f, 48f, 46f, 66f, 36f, 66f, 50f, 48f);
        pen.Poly(pen.back, 52f, 30f, 62f, 30f, 76f, 48f, 62f, 66f, 52f, 66f, 66f, 48f);
    }

    // Wellspring Manuscript: a quill over a drop of ink
    static void Quill(IconPen pen)
    {
        pen.Poly(pen.main, 90f, 4f, 70f, 10f, 46f, 30f, 30f, 58f, 24f, 76f, 40f, 66f, 64f, 50f, 82f, 28f);
        pen.Poly(pen.accent, 90f, 4f, 50f, 40f, 30f, 66f, 26f, 72f, 40f, 66f, 64f, 50f, 82f, 28f);
        pen.Line(pen.main, 24f, 76f, 16f, 90f, 4f);
        DropShape(pen, pen.accent, 74f, 76f, 0.36f);
    }
}

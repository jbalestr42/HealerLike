// The items' pictograms, and the shapes several of them share
public static partial class IconGlyphs
{
    // Adrenaline: a syringe
    static void Syringe(IconPen pen)
    {
        pen.Line(pen.main, 28f, 72f, 12f, 88f, 4f);
        pen.Line(pen.main, 30f, 70f, 64f, 36f, 22f);
        pen.Line(pen.accent, 34f, 66f, 50f, 50f, 10f);
        pen.Line(pen.main, 54f, 22f, 78f, 46f, 7f);
        pen.Line(pen.main, 64f, 36f, 82f, 18f, 6f);
        pen.Line(pen.main, 74f, 10f, 90f, 26f, 8f);
    }

    // Arc: a bolt jumping on to three targets
    static void BoltChain3(IconPen pen)
    {
        BoltChain(pen, 3);
    }

    // The balance's Arc: on to two targets
    static void BoltChain2(IconPen pen)
    {
        BoltChain(pen, 2);
    }

    static void BoltChain(IconPen pen, int jumps)
    {
        Bolt(pen, pen.main, 34f, 50f, 0.82f);
        (float x, float y, float r)[] dots = { (76f, 24f, 9f), (88f, 52f, 8f), (76f, 80f, 7f) };
        float fromX = 50f;
        float fromY = 44f;
        for (int i = 0; i < jumps; i++)
        {
            pen.Line(pen.accent, fromX, fromY, dots[i].x, dots[i].y, 4f);
            fromX = dots[i].x;
            fromY = dots[i].y;
        }
        for (int i = 0; i < jumps; i++)
        {
            pen.Circle(pen.main, dots[i].x, dots[i].y, dots[i].r);
        }
    }

    // Archon: a crown
    static void Crown(IconPen pen)
    {
        pen.Poly(pen.main, 10f, 80f, 12f, 30f, 32f, 54f, 50f, 22f, 68f, 54f, 88f, 30f, 90f, 80f);
        pen.Rect(pen.main, 10f, 72f, 80f, 16f, 4f);
        pen.Circle(pen.main, 12f, 28f, 7f);
        pen.Circle(pen.main, 50f, 18f, 8f);
        pen.Circle(pen.main, 88f, 28f, 7f);
        pen.Circle(pen.back, 30f, 80f, 4f);
        pen.Circle(pen.back, 50f, 80f, 5f);
        pen.Circle(pen.back, 70f, 80f, 4f);
    }

    // Armor Breaker: a war hammer
    static void Hammer(IconPen pen)
    {
        pen.Rect(pen.main, 44f, 36f, 12f, 60f, 5f);
        pen.Rect(pen.main, 14f, 8f, 72f, 32f, 6f);
        pen.Rect(pen.accent, 50f, 8f, 36f, 32f, 6f);
        pen.Rect(pen.accent, 50f, 8f, 10f, 32f);
        pen.Rect(pen.back, 30f, 8f, 4f, 32f);
        pen.Rect(pen.back, 66f, 8f, 4f, 32f);
    }

    // The balance's Bounce: the bounce, weighed
    static void BounceScales(IconPen pen)
    {
        Bounce(pen);
        ScalesBadge(pen);
    }

    // The balance's Mortar Shell: the shell, weighed
    static void ShellScales(IconPen pen)
    {
        Shell(pen, pen.main, 40f);
        ScalesBadge(pen);
    }

    // Banner: a flag on its pole
    static void Banner(IconPen pen)
    {
        Flag(pen);
        pen.Circle(pen.accent, 54f, 34f, 11f);
    }

    // Beacon: a fire on a pillar
    static void Beacon(IconPen pen)
    {
        Flame(pen, pen.main, 50f, 28f, 1f);
        Flame(pen, pen.accent, 50f, 34f, 0.5f);
        pen.Poly(pen.main, 18f, 52f, 82f, 52f, 68f, 68f, 32f, 68f);
        pen.Rect(pen.main, 41f, 66f, 18f, 22f);
        pen.Rect(pen.main, 28f, 84f, 44f, 12f, 4f);
    }

    // Blight Acolyte: a hooded acolyte, a crack on its chest
    static void AcolyteBlight(IconPen pen)
    {
        Acolyte(pen);
        pen.Poly(pen.back, 48f, 66f, 56f, 66f, 50f, 76f, 57f, 82f, 49f, 92f, 51f, 82f, 44f, 76f);
    }

    // Blood Bond: two linked rings under a drop
    static void BloodBond(IconPen pen)
    {
        pen.Ring(pen.accent, 64f, 64f, 24f, 9f);
        pen.Ring(pen.main, 36f, 64f, 24f, 9f);
        DropShape(pen, pen.main, 50f, 20f, 0.4f);
    }

    // Blood Price: a dagger dripping
    static void BloodDagger(IconPen pen)
    {
        pen.Poly(pen.main, 38f, 40f, 62f, 40f, 62f, 72f, 50f, 94f, 38f, 72f);
        pen.Poly(pen.accent, 50f, 40f, 62f, 40f, 62f, 72f, 50f, 94f);
        pen.Rect(pen.main, 22f, 30f, 56f, 12f, 6f);
        pen.Rect(pen.main, 43f, 8f, 14f, 24f, 4f);
        pen.Circle(pen.main, 50f, 8f, 7f);
        DropShape(pen, pen.main, 80f, 72f, 0.3f);
        DropShape(pen, pen.main, 20f, 82f, 0.24f);
    }

    // Blood Ward: a shield with a drop
    static void ShieldDrop(IconPen pen)
    {
        Shield(pen, pen.main, pen.accent);
        DropShape(pen, pen.back, 50f, 48f, 0.5f);
    }

    // Bone Charm: a skull
    static void Skull(IconPen pen)
    {
        pen.Circle(pen.main, 50f, 42f, 36f);
        pen.Rect(pen.main, 30f, 56f, 40f, 34f, 8f);
        pen.Circle(pen.back, 35f, 46f, 11f);
        pen.Circle(pen.back, 65f, 46f, 11f);
        pen.Poly(pen.back, 50f, 58f, 44f, 70f, 56f, 70f);
        pen.Rect(pen.back, 41f, 78f, 4f, 12f);
        pen.Rect(pen.back, 55f, 78f, 4f, 12f);
    }

    // Boost Cell: a battery with a bolt
    static void Battery(IconPen pen)
    {
        pen.Rect(pen.main, 38f, 4f, 24f, 14f, 3f);
        pen.Rect(pen.main, 22f, 14f, 56f, 82f, 9f);
        Bolt(pen, pen.back, 50f, 55f, 0.55f);
    }

    // Bounce: a ball bouncing off the ground
    static void Bounce(IconPen pen)
    {
        pen.Line(pen.accent, 8f, 26f, 34f, 80f, 8f);
        pen.Line(pen.accent, 34f, 80f, 60f, 40f, 8f);
        pen.Rect(pen.main, 12f, 86f, 46f, 8f, 4f);
        pen.Circle(pen.main, 70f, 28f, 17f);
    }

    // Briar Thorns: a thorny stem
    static void Briar(IconPen pen)
    {
        pen.Line(pen.main, 26f, 94f, 40f, 66f, 9f);
        pen.Line(pen.main, 40f, 66f, 34f, 42f, 9f);
        pen.Line(pen.main, 34f, 42f, 52f, 18f, 9f);
        pen.Line(pen.main, 52f, 18f, 74f, 8f, 9f);
        pen.Poly(pen.main, 38f, 74f, 56f, 76f, 42f, 64f);
        pen.Poly(pen.main, 34f, 52f, 14f, 50f, 32f, 40f);
        pen.Poly(pen.main, 44f, 28f, 62f, 32f, 50f, 20f);
        pen.Poly(pen.main, 28f, 86f, 10f, 80f, 26f, 76f);
        pen.Ellipse(pen.accent, 74f, 52f, 16f, 9f);
        pen.Line(pen.accent, 40f, 62f, 66f, 54f, 4f);
    }

    // Carrion: a crow
    static void Crow(IconPen pen)
    {
        pen.Poly(pen.main, 70f, 50f, 98f, 74f, 72f, 72f);
        pen.Ellipse(pen.main, 56f, 56f, 28f, 20f);
        pen.Circle(pen.main, 30f, 38f, 16f);
        pen.Poly(pen.main, 18f, 32f, 2f, 44f, 20f, 48f);
        pen.Ellipse(pen.accent, 62f, 52f, 20f, 10f);
        pen.Circle(pen.back, 28f, 35f, 4f);
        pen.Line(pen.main, 50f, 72f, 46f, 94f, 5f);
        pen.Line(pen.main, 62f, 72f, 64f, 94f, 5f);
    }

    // Censer: a smoking censer on its chain
    static void Censer(IconPen pen)
    {
        pen.Line(pen.main, 50f, 4f, 50f, 34f, 4f);
        pen.Poly(pen.main, 28f, 52f, 72f, 52f, 50f, 30f);
        pen.Ellipse(pen.main, 50f, 62f, 28f, 20f);
        pen.Rect(pen.main, 38f, 78f, 24f, 14f, 4f);
        pen.Circle(pen.back, 38f, 62f, 4f);
        pen.Circle(pen.back, 50f, 64f, 4f);
        pen.Circle(pen.back, 62f, 62f, 4f);
        pen.Line(pen.accent, 22f, 46f, 12f, 30f, 5f);
        pen.Line(pen.accent, 12f, 30f, 20f, 14f, 5f);
        pen.Line(pen.accent, 78f, 46f, 88f, 30f, 5f);
        pen.Line(pen.accent, 88f, 30f, 80f, 14f, 5f);
    }

    // Charm of Mercy: a pendant with a heart
    static void Pendant(IconPen pen)
    {
        pen.Line(pen.accent, 16f, 4f, 50f, 36f, 5f);
        pen.Line(pen.accent, 84f, 4f, 50f, 36f, 5f);
        pen.Circle(pen.main, 50f, 38f, 7f);
        pen.Circle(pen.main, 50f, 68f, 30f);
        HeartAt(pen, pen.back, 50f, 70f, 1.3f);
    }

    // Conclave: two arrows turning in a circle
    static void Cycle(IconPen pen)
    {
        ArcArrow(pen, pen.main, 200f, 330f);
        ArcArrow(pen, pen.accent, 20f, 150f);
    }

    // Current Health Damage: a heart with a bite out of it
    static void HeartBitten(IconPen pen)
    {
        Heart(pen, pen.main);
        pen.Poly(pen.back, 50f, 52f, 104f, 6f, 104f, 46f);
        pen.Circle(pen.main, 84f, 70f, 5f);
        pen.Circle(pen.main, 94f, 58f, 3f);
    }

    // Cursed Idol: a stone idol with glowing eyes
    static void Idol(IconPen pen)
    {
        pen.Rect(pen.main, 26f, 6f, 48f, 60f, 10f);
        pen.Rect(pen.main, 18f, 64f, 64f, 30f, 6f);
        pen.Rect(pen.back, 32f, 24f, 14f, 10f, 3f);
        pen.Rect(pen.back, 54f, 24f, 14f, 10f, 3f);
        pen.Circle(pen.accent, 39f, 29f, 3f);
        pen.Circle(pen.accent, 61f, 29f, 3f);
        pen.Rect(pen.back, 40f, 46f, 20f, 6f, 3f);
        pen.Poly(pen.back, 70f, 6f, 74f, 6f, 66f, 22f, 72f, 30f, 64f, 40f, 68f, 30f, 62f, 22f);
    }

    // Echo: a sound and its echoes
    static void Echo(IconPen pen)
    {
        pen.Circle(pen.main, 18f, 50f, 11f);
        ArcBand(pen, pen.main, 18f, 50f, 30f, 9f, -55f, 55f);
        ArcBand(pen, pen.main, 18f, 50f, 52f, 9f, -48f, 48f);
        ArcBand(pen, pen.accent, 18f, 50f, 74f, 9f, -40f, 40f);
    }

    // Executioner: an executioner's axe
    static void Axe(IconPen pen)
    {
        pen.Rect(pen.main, 50f, 4f, 11f, 92f, 5f);
        pen.Poly(pen.main, 52f, 14f, 22f, 4f, 8f, 26f, 6f, 46f, 14f, 68f, 52f, 54f);
        pen.Poly(pen.accent, 22f, 4f, 8f, 26f, 6f, 46f, 14f, 68f, 22f, 62f, 16f, 46f, 18f, 26f, 28f, 10f);
        pen.Poly(pen.main, 60f, 22f, 82f, 30f, 60f, 40f);
    }

    // Executioner's Edge: a sword and a critical spark
    static void SwordSpark(IconPen pen)
    {
        pen.Line(pen.main, 24f, 76f, 70f, 30f, 15f);
        pen.Poly(pen.main, 64f, 24f, 88f, 12f, 76f, 36f);
        pen.Line(pen.accent, 32f, 68f, 70f, 30f, 4f);
        pen.Line(pen.main, 12f, 60f, 40f, 88f, 9f);
        pen.Line(pen.main, 22f, 78f, 8f, 92f, 8f);
        Sparkle(pen, pen.main, 84f, 66f, 16f, 5f);
        Sparkle(pen, pen.accent, 30f, 18f, 12f, 4f);
    }

    // Explode On Hit: a burst
    static void Burst(IconPen pen)
    {
        Star(pen, pen.main, 50f, 50f, 48f, 26f, 10);
        Star(pen, pen.accent, 50f, 50f, 28f, 14f, 10);
    }

    // Faithful Guard: a shield under a halo
    static void ShieldHalo(IconPen pen)
    {
        pen.EllipseRing(pen.main, 50f, 12f, 26f, 8f, 6f);
        Shield(pen, pen.main, pen.accent, 50f, 60f, 0.78f);
    }

    // Frenzy: three claw slashes
    static void Claws(IconPen pen)
    {
        for (int i = 0; i < 3; i++)
        {
            float o = i * 24f;
            pen.Poly(i == 1 ? pen.main : pen.accent, 10f + o, 12f, 22f + o, 8f, 42f + o, 88f, 34f + o, 94f);
        }
    }

    // Frost Acolyte: a hooded acolyte, a flake on its chest
    static void AcolyteFrost(IconPen pen)
    {
        Acolyte(pen);
        pen.Line(pen.back, 50f, 68f, 50f, 88f, 3.5f);
        pen.Line(pen.back, 41f, 73f, 59f, 83f, 3.5f);
        pen.Line(pen.back, 41f, 83f, 59f, 73f, 3.5f);
    }

    // Hex Acolyte: a hooded acolyte, an eye on its chest
    static void AcolyteHex(IconPen pen)
    {
        Acolyte(pen);
        pen.Ellipse(pen.back, 50f, 78f, 11f, 6f);
        pen.Circle(pen.accent, 50f, 78f, 3.5f);
    }

    // Spark Acolyte: a hooded acolyte, a bolt on its chest
    static void AcolyteSpark(IconPen pen)
    {
        Acolyte(pen);
        Bolt(pen, pen.back, 50f, 78f, 0.24f);
    }

    // Frost: ice crystals
    static void IceShard(IconPen pen)
    {
        pen.Poly(pen.main, 22f, 36f, 32f, 52f, 30f, 94f, 18f, 94f, 10f, 52f);
        pen.Poly(pen.main, 80f, 44f, 90f, 58f, 84f, 94f, 72f, 94f, 70f, 58f);
        pen.Poly(pen.main, 50f, 4f, 66f, 30f, 60f, 94f, 40f, 94f, 34f, 30f);
        pen.Poly(pen.accent, 50f, 4f, 66f, 30f, 60f, 94f, 50f, 94f);
    }

    // Glass Cannon: a cracked cannon
    static void Cannon(IconPen pen)
    {
        pen.Line(pen.main, 26f, 64f, 76f, 28f, 24f);
        pen.Line(pen.main, 74f, 14f, 92f, 38f, 10f);
        pen.Poly(pen.back, 48f, 36f, 54f, 36f, 52f, 46f, 60f, 52f, 52f, 62f, 54f, 52f, 46f, 46f);
        pen.Circle(pen.back, 34f, 74f, 24f);
        pen.Circle(pen.main, 34f, 74f, 20f);
        pen.Circle(pen.back, 34f, 74f, 6f);
    }

    // Gratitude: a heart lending a sword
    static void HeartSword(IconPen pen)
    {
        HeartAt(pen, pen.main, 36f, 62f, 1.6f);
        pen.Line(pen.back, 54f, 46f, 86f, 14f, 16f);
        pen.Line(pen.back, 46f, 34f, 66f, 54f, 14f);
        pen.Line(pen.main, 56f, 44f, 86f, 14f, 9f);
        pen.Line(pen.main, 48f, 36f, 64f, 52f, 7f);
        pen.Line(pen.main, 56f, 44f, 46f, 54f, 6f);
    }

    // Growing Seed: a sprout
    static void Sprout(IconPen pen)
    {
        pen.Ellipse(pen.main, 50f, 90f, 36f, 8f);
        pen.Line(pen.main, 50f, 88f, 50f, 40f, 7f);
        Leaf(pen, pen.main, 50f, 56f, 10f, 34f, 22f);
        Leaf(pen, pen.accent, 50f, 44f, 90f, 18f, 24f);
    }

    // Hair Trigger: a stopwatch
    static void Stopwatch(IconPen pen)
    {
        pen.Rect(pen.main, 42f, 4f, 16f, 12f, 3f);
        pen.Line(pen.main, 78f, 22f, 86f, 14f, 8f);
        pen.Circle(pen.main, 50f, 56f, 40f);
        pen.Circle(pen.back, 50f, 56f, 31f);
        ArcBand(pen, pen.accent, 50f, 56f, 18f, 18f, -90f, 30f);
        pen.Line(pen.main, 50f, 56f, 50f, 32f, 6f);
        pen.Line(pen.main, 50f, 56f, 66f, 66f, 5f);
        pen.Circle(pen.main, 50f, 56f, 5f);
    }

    // Halo: a halo over two wings
    static void Wings(IconPen pen)
    {
        pen.EllipseRing(pen.main, 50f, 14f, 22f, 7f, 6f);
        pen.Poly(pen.main, 46f, 40f, 4f, 26f, 8f, 50f, 2f, 62f, 14f, 72f, 10f, 84f, 46f, 72f);
        pen.Poly(pen.main, 54f, 40f, 96f, 26f, 92f, 50f, 98f, 62f, 86f, 72f, 90f, 84f, 54f, 72f);
        pen.Line(pen.back, 14f, 50f, 40f, 52f, 3f);
        pen.Line(pen.back, 18f, 64f, 40f, 62f, 3f);
        pen.Line(pen.back, 86f, 50f, 60f, 52f, 3f);
        pen.Line(pen.back, 82f, 64f, 60f, 62f, 3f);
        pen.Circle(pen.accent, 50f, 56f, 8f);
    }

    // Hardened Foes: a knight's helm
    static void Helmet(IconPen pen)
    {
        pen.Ellipse(pen.accent, 50f, 10f, 8f, 10f);
        pen.Circle(pen.main, 50f, 44f, 32f);
        pen.Rect(pen.main, 18f, 44f, 64f, 50f, 8f);
        pen.Rect(pen.back, 26f, 46f, 48f, 8f, 4f);
        pen.Rect(pen.back, 47f, 54f, 6f, 30f, 2f);
    }

    // Heart Stone: a cut heart
    static void HeartGem(IconPen pen)
    {
        Heart(pen, pen.main);
        pen.Poly(pen.accent, 50f, 46f, 92f, 26f, 90f, 46f, 50f, 86f);
        pen.Line(pen.back, 8f, 30f, 50f, 46f, 3f);
        pen.Line(pen.back, 92f, 30f, 50f, 46f, 3f);
        pen.Line(pen.back, 50f, 46f, 50f, 86f, 3f);
        pen.Line(pen.back, 28f, 12f, 50f, 46f, 3f);
        pen.Line(pen.back, 72f, 12f, 50f, 46f, 3f);
    }

    // Holy Water: a flask with a cross
    static void FlaskCross(IconPen pen)
    {
        Flask(pen);
        pen.Rect(pen.back, 46f, 50f, 8f, 30f, 2f);
        pen.Rect(pen.back, 36f, 58f, 28f, 8f, 2f);
    }

    // Hunter's Mark: a bow and its arrow
    static void Bow(IconPen pen)
    {
        ArcBand(pen, pen.main, 10f, 50f, 48f, 9f, -58f, 58f);
        pen.Line(pen.accent, 35f, 10f, 35f, 90f, 3f);
        Arrow(pen, pen.main, 8f, 50f, 96f, 50f);
    }

    // Increase Damage Per Hit: bars growing
    static void Bars(IconPen pen)
    {
        pen.Rect(pen.accent, 8f, 64f, 24f, 32f, 4f);
        pen.Rect(pen.main, 38f, 42f, 24f, 54f, 4f);
        pen.Rect(pen.main, 68f, 16f, 24f, 80f, 4f);
        pen.Poly(pen.main, 8f, 50f, 8f, 36f, 22f, 36f);
        pen.Line(pen.main, 10f, 48f, 34f, 22f, 5f);
    }

    // Increase Damage With Projectile Distance: an arrow far on its way
    static void ArrowDistance(IconPen pen)
    {
        pen.Line(pen.accent, 4f, 30f, 30f, 30f, 6f);
        pen.Line(pen.accent, 4f, 70f, 30f, 70f, 6f);
        pen.Line(pen.accent, 14f, 50f, 34f, 50f, 6f);
        Arrow(pen, pen.main, 40f, 50f, 98f, 50f);
        pen.Rect(pen.main, 4f, 86f, 92f, 5f, 2f);
        pen.Rect(pen.main, 4f, 80f, 5f, 16f, 2f);
        pen.Rect(pen.main, 91f, 80f, 5f, 16f, 2f);
    }

    // Iron Plating: a riveted plate
    static void Plate(IconPen pen)
    {
        pen.Rect(pen.main, 10f, 12f, 80f, 76f, 10f);
        pen.Poly(pen.accent, 90f, 22f, 90f, 78f, 80f, 88f, 30f, 88f);
        pen.Circle(pen.back, 22f, 24f, 5f);
        pen.Circle(pen.back, 78f, 24f, 5f);
        pen.Circle(pen.back, 22f, 76f, 5f);
        pen.Circle(pen.back, 78f, 76f, 5f);
    }

    // Lone Wolf: a wolf's head
    static void Wolf(IconPen pen)
    {
        pen.Poly(pen.main, 14f, 4f, 42f, 30f, 16f, 46f);
        pen.Poly(pen.main, 86f, 4f, 58f, 30f, 84f, 46f);
        pen.Poly(pen.accent, 18f, 14f, 32f, 28f, 20f, 36f);
        pen.Poly(pen.accent, 82f, 14f, 68f, 28f, 80f, 36f);
        pen.Poly(pen.main, 12f, 30f, 88f, 30f, 82f, 60f, 50f, 96f, 18f, 60f);
        pen.Poly(pen.back, 28f, 46f, 44f, 52f, 30f, 55f);
        pen.Poly(pen.back, 72f, 46f, 56f, 52f, 70f, 55f);
        pen.Ellipse(pen.back, 50f, 82f, 8f, 5f);
    }

    // Lucky Coin: a coin with a star
    static void Coin(IconPen pen)
    {
        pen.Circle(pen.main, 50f, 50f, 44f);
        pen.Ring(pen.accent, 50f, 50f, 36f, 4f);
        Star(pen, pen.back, 50f, 52f, 24f, 10f, 5);
    }

    // Martyr's Heart: a heart shining on its neighbours
    static void HeartRays(IconPen pen)
    {
        for (int i = 0; i < 8; i++)
        {
            double a = i * System.Math.PI / 4;
            float dx = (float)System.Math.Cos(a);
            float dy = (float)System.Math.Sin(a);
            pen.Line(pen.accent, 50f + dx * 38f, 50f + dy * 38f, 50f + dx * 48f, 50f + dy * 48f, 6f);
        }
        HeartAt(pen, pen.main, 50f, 50f, 1.75f);
    }

    // Missing Health Damage: an emptied heart
    static void HeartEmpty(IconPen pen)
    {
        Heart(pen, pen.main);
        HeartAt(pen, pen.back, 50f, 48f, 1.9f);
        pen.Line(pen.main, 50f, 30f, 50f, 58f, 8f);
        pen.Poly(pen.main, 36f, 54f, 64f, 54f, 50f, 72f);
    }

    // Mortar Shell: a shell bursting
    static void ShellBurst(IconPen pen)
    {
        Star(pen, pen.accent, 50f, 44f, 50f, 30f, 12);
        Shell(pen, pen.main, 50f);
    }

    // Multiple Shoot: two arrows
    static void Arrows2(IconPen pen)
    {
        Arrow(pen, pen.main, 8f, 62f, 76f, 8f);
        Arrow(pen, pen.main, 24f, 92f, 92f, 38f);
    }

    // Volley: arrows falling
    static void ArrowsRain(IconPen pen)
    {
        Arrow(pen, pen.accent, 18f, 4f, 18f, 76f);
        Arrow(pen, pen.main, 50f, 10f, 50f, 96f);
        Arrow(pen, pen.accent, 82f, 4f, 82f, 76f);
    }

    // Pact: a scroll sealed in blood
    static void Scroll(IconPen pen)
    {
        pen.Rect(pen.main, 20f, 12f, 60f, 76f, 2f);
        pen.Rect(pen.accent, 12f, 6f, 76f, 14f, 7f);
        pen.Rect(pen.accent, 12f, 80f, 76f, 14f, 7f);
        pen.Rect(pen.back, 30f, 28f, 40f, 5f, 2f);
        pen.Rect(pen.back, 30f, 40f, 32f, 5f, 2f);
        pen.Rect(pen.back, 30f, 52f, 40f, 5f, 2f);
        DropShape(pen, pen.back, 62f, 68f, 0.26f);
    }

    // Phalanx: three shields side by side
    static void Shields3(IconPen pen)
    {
        Shield(pen, pen.main, pen.accent, 22f, 60f, 0.5f);
        Shield(pen, pen.main, pen.accent, 78f, 60f, 0.5f);
        Shield(pen, pen.back, pen.back, 50f, 48f, 0.74f);
        Shield(pen, pen.main, pen.accent, 50f, 48f, 0.64f);
    }

    // Phylactery: a jar holding a soul
    static void Phylactery(IconPen pen)
    {
        pen.Circle(pen.main, 50f, 10f, 6f);
        pen.Rect(pen.main, 30f, 14f, 40f, 12f, 4f);
        pen.Rect(pen.main, 22f, 28f, 56f, 68f, 14f);
        pen.Rect(pen.back, 30f, 40f, 40f, 48f, 10f);
        Flame(pen, pen.accent, 50f, 66f, 0.7f);
    }

    // Plague: a plague doctor's beaked mask
    static void PlagueMask(IconPen pen)
    {
        pen.Rect(pen.main, 22f, 2f, 40f, 16f, 4f);
        pen.Rect(pen.main, 6f, 16f, 72f, 9f, 4f);
        pen.Poly(pen.main, 52f, 40f, 98f, 88f, 46f, 70f);
        pen.Circle(pen.main, 40f, 48f, 26f);
        pen.Circle(pen.back, 30f, 44f, 8f);
        pen.Circle(pen.back, 52f, 44f, 7f);
        pen.Circle(pen.accent, 30f, 44f, 3f);
        pen.Circle(pen.accent, 52f, 44f, 3f);
    }

    // Poison: a bubbling vial
    static void PoisonVial(IconPen pen)
    {
        Flask(pen);
        pen.Circle(pen.back, 40f, 70f, 7f);
        pen.Circle(pen.back, 60f, 62f, 5f);
        pen.Circle(pen.back, 56f, 80f, 4f);
        DropShape(pen, pen.main, 86f, 80f, 0.22f);
    }

    // Punching Bag Regen: a punching bag mending
    static void PunchingBag(IconPen pen)
    {
        pen.Line(pen.main, 50f, 2f, 50f, 14f, 4f);
        pen.Rect(pen.main, 26f, 12f, 48f, 84f, 18f);
        pen.Rect(pen.back, 26f, 26f, 48f, 4f);
        pen.Rect(pen.back, 26f, 80f, 48f, 4f);
        pen.Rect(pen.accent, 45f, 40f, 10f, 30f, 2f);
        pen.Rect(pen.accent, 35f, 50f, 30f, 10f, 2f);
    }

    // Radiant: a sun
    static void Sun(IconPen pen)
    {
        for (int i = 0; i < 12; i++)
        {
            double a = i * System.Math.PI / 6;
            float dx = (float)System.Math.Cos(a);
            float dy = (float)System.Math.Sin(a);
            float length = i % 2 == 0 ? 48f : 40f;
            pen.Line(i % 2 == 0 ? pen.main : pen.accent, 50f + dx * 32f, 50f + dy * 32f, 50f + dx * length, 50f + dy * length, 7f);
        }
        pen.Circle(pen.main, 50f, 50f, 24f);
    }

    // Rage: a blazing fire
    static void Fire(IconPen pen)
    {
        Flame(pen, pen.main, 22f, 70f, 0.7f);
        Flame(pen, pen.main, 78f, 70f, 0.7f);
        Flame(pen, pen.main, 50f, 52f, 1.6f);
        Flame(pen, pen.accent, 50f, 68f, 0.8f);
    }

    // Regen Hp: a leaf and a cross
    static void Herb(IconPen pen)
    {
        Leaf(pen, pen.main, 14f, 94f, 90f, 22f, 42f);
        pen.Line(pen.back, 18f, 90f, 74f, 38f, 4f);
        pen.Rect(pen.accent, 14f, 6f, 10f, 32f, 2f);
        pen.Rect(pen.accent, 3f, 17f, 32f, 10f, 2f);
    }

    // Sap: a tree weeping sap
    static void Tree(IconPen pen)
    {
        pen.Rect(pen.main, 40f, 50f, 20f, 40f, 4f);
        pen.Rect(pen.main, 24f, 86f, 52f, 10f, 4f);
        pen.Circle(pen.main, 50f, 34f, 30f);
        pen.Circle(pen.main, 26f, 46f, 16f);
        pen.Circle(pen.main, 74f, 46f, 16f);
        DropShape(pen, pen.accent, 50f, 72f, 0.24f);
    }

    // Second Wind: gusts of wind
    static void Wind(IconPen pen)
    {
        pen.Line(pen.main, 6f, 30f, 60f, 30f, 8f);
        ArcBand(pen, pen.main, 60f, 18f, 12f, 8f, -180f, 90f);
        pen.Line(pen.accent, 6f, 52f, 78f, 52f, 8f);
        ArcBand(pen, pen.accent, 78f, 64f, 12f, 8f, -90f, 180f);
        pen.Line(pen.main, 16f, 76f, 54f, 76f, 8f);
    }

    // Self Destruct: a lit bomb
    static void Bomb(IconPen pen)
    {
        pen.Line(pen.main, 58f, 34f, 70f, 22f, 16f);
        pen.Line(pen.main, 70f, 22f, 82f, 14f, 4f);
        pen.Circle(pen.main, 42f, 60f, 36f);
        pen.Circle(pen.accent, 30f, 48f, 8f);
        Star(pen, pen.accent, 88f, 10f, 12f, 5f, 6);
    }

    // Sharpened Blades: two crossed swords
    static void CrossedSwords(IconPen pen)
    {
        DiagonalSword(pen, 14f, 86f, 82f, 18f);
        DiagonalSword(pen, 86f, 86f, 18f, 18f);
    }

    // Whetstone: a stone sharpening a blade
    static void Whetstone(IconPen pen)
    {
        pen.Poly(pen.main, 4f, 74f, 64f, 52f, 96f, 66f, 36f, 92f);
        pen.Poly(pen.accent, 4f, 74f, 64f, 52f, 70f, 56f, 10f, 78f);
        pen.Line(pen.main, 18f, 54f, 74f, 14f, 10f);
        pen.Poly(pen.main, 70f, 8f, 94f, 2f, 80f, 22f);
        Sparkle(pen, pen.main, 30f, 22f, 12f, 4f);
        Sparkle(pen, pen.accent, 88f, 40f, 9f, 3f);
    }

    // Siphon: a mana crystal drained
    static void CrystalDrain(IconPen pen)
    {
        pen.Poly(pen.main, 50f, 2f, 76f, 30f, 50f, 64f, 24f, 30f);
        pen.Poly(pen.accent, 50f, 2f, 76f, 30f, 50f, 64f);
        pen.Line(pen.main, 50f, 70f, 50f, 84f, 8f);
        pen.Poly(pen.main, 36f, 80f, 64f, 80f, 50f, 98f);
        pen.Line(pen.accent, 22f, 66f, 30f, 76f, 5f);
        pen.Line(pen.accent, 78f, 66f, 70f, 76f, 5f);
    }

    // Slow: a snail
    static void Snail(IconPen pen)
    {
        pen.Rect(pen.main, 4f, 76f, 88f, 16f, 8f);
        pen.Line(pen.main, 12f, 72f, 6f, 46f, 4f);
        pen.Line(pen.main, 20f, 72f, 24f, 46f, 4f);
        pen.Circle(pen.main, 6f, 46f, 4f);
        pen.Circle(pen.main, 24f, 46f, 4f);
        pen.Circle(pen.main, 14f, 76f, 10f);
        pen.Circle(pen.main, 60f, 50f, 32f);
        pen.Ring(pen.back, 60f, 50f, 20f, 5f);
        pen.Circle(pen.accent, 60f, 50f, 8f);
    }

    // Spark: a bolt jumping on to one target
    static void BoltChain1(IconPen pen)
    {
        BoltChain(pen, 1);
    }

    // Strangle: a noose
    static void Noose(IconPen pen)
    {
        pen.Line(pen.main, 50f, 2f, 50f, 34f, 7f);
        pen.Ring(pen.main, 50f, 66f, 28f, 10f);
        pen.Rect(pen.main, 40f, 26f, 20f, 22f, 5f);
        pen.Rect(pen.back, 40f, 32f, 20f, 3f);
        pen.Rect(pen.back, 40f, 40f, 20f, 3f);
        pen.Circle(pen.accent, 50f, 66f, 10f);
    }

    // Swift Grace: a feather
    static void Feather(IconPen pen)
    {
        Leaf(pen, pen.main, 18f, 86f, 88f, 8f, 34f);
        pen.Poly(pen.accent, 53f, 47f, 88f, 8f, 70f, 46f, 60f, 54f);
        pen.Line(pen.back, 20f, 84f, 80f, 16f, 3f);
        pen.Line(pen.back, 40f, 62f, 30f, 52f, 3f);
        pen.Line(pen.back, 56f, 44f, 46f, 34f, 3f);
        pen.Line(pen.main, 6f, 98f, 22f, 82f, 5f);
    }

    // Taunt Totem: a totem pole of shouting faces
    static void Totem(IconPen pen)
    {
        pen.Poly(pen.accent, 30f, 12f, 4f, 6f, 30f, 32f);
        pen.Poly(pen.accent, 70f, 12f, 96f, 6f, 70f, 32f);
        pen.Rect(pen.main, 28f, 4f, 44f, 30f, 6f);
        pen.Rect(pen.main, 24f, 36f, 52f, 30f, 6f);
        pen.Rect(pen.main, 28f, 68f, 44f, 30f, 6f);
        foreach (float top in new[] { 4f, 36f, 68f })
        {
            pen.Rect(pen.back, 36f, top + 7f, 9f, 6f, 2f);
            pen.Rect(pen.back, 55f, top + 7f, 9f, 6f, 2f);
            pen.Ellipse(pen.back, 50f, top + 21f, 8f, 5f);
        }
    }

    // Thorns of Life: a heart among thorns
    static void HeartThorns(IconPen pen)
    {
        Star(pen, pen.accent, 50f, 50f, 50f, 30f, 12);
        HeartAt(pen, pen.main, 50f, 50f, 1.75f);
    }

    // Titan Fury: a horned helm, its eyes glowing
    static void HornedHelm(IconPen pen)
    {
        pen.Poly(pen.main, 26f, 42f, 6f, 4f, 2f, 28f, 16f, 50f);
        pen.Poly(pen.main, 74f, 42f, 94f, 4f, 98f, 28f, 84f, 50f);
        pen.Circle(pen.main, 50f, 50f, 30f);
        pen.Rect(pen.main, 20f, 50f, 60f, 44f, 8f);
        pen.Rect(pen.back, 28f, 54f, 44f, 10f, 5f);
        pen.Circle(pen.accent, 38f, 59f, 4f);
        pen.Circle(pen.accent, 62f, 59f, 4f);
        pen.Rect(pen.back, 47f, 64f, 6f, 24f, 2f);
    }

    // Tower Shield: a tall shield
    static void TowerShield(IconPen pen)
    {
        pen.Rect(pen.main, 20f, 4f, 60f, 92f, 10f);
        pen.Rect(pen.accent, 50f, 4f, 30f, 92f, 10f);
        pen.Rect(pen.accent, 50f, 4f, 12f, 92f);
        pen.Rect(pen.back, 20f, 26f, 60f, 4f);
        pen.Rect(pen.back, 20f, 70f, 60f, 4f);
        pen.Circle(pen.back, 50f, 50f, 9f);
    }

    // Trinity: three linked orbs
    static void Trinity(IconPen pen)
    {
        pen.Line(pen.accent, 50f, 16f, 16f, 78f, 7f);
        pen.Line(pen.accent, 50f, 16f, 84f, 78f, 7f);
        pen.Line(pen.accent, 16f, 78f, 84f, 78f, 7f);
        pen.Circle(pen.main, 50f, 18f, 16f);
        pen.Circle(pen.main, 16f, 78f, 16f);
        pen.Circle(pen.main, 84f, 78f, 16f);
        pen.Circle(pen.back, 50f, 18f, 6f);
        pen.Circle(pen.back, 16f, 78f, 6f);
        pen.Circle(pen.back, 84f, 78f, 6f);
    }

    // Twin Blade: twin daggers
    static void TwinBlades(IconPen pen)
    {
        Dagger(pen, pen.accent, 30f);
        Dagger(pen, pen.main, 70f);
    }

    // Vengeance: a grave sending on haste
    static void Tombstone(IconPen pen)
    {
        pen.Circle(pen.main, 38f, 40f, 26f);
        pen.Rect(pen.main, 12f, 40f, 52f, 48f);
        pen.Rect(pen.main, 6f, 84f, 64f, 12f, 3f);
        pen.Rect(pen.back, 34f, 26f, 8f, 34f, 2f);
        pen.Rect(pen.back, 24f, 36f, 28f, 8f, 2f);
        pen.Poly(pen.accent, 70f, 30f, 82f, 30f, 98f, 52f, 82f, 74f, 70f, 74f, 86f, 52f);
    }

    // Venom: two fangs dripping
    static void Fangs(IconPen pen)
    {
        pen.Rect(pen.main, 10f, 4f, 80f, 18f, 9f);
        pen.Poly(pen.main, 18f, 14f, 44f, 14f, 32f, 70f);
        pen.Poly(pen.main, 56f, 14f, 82f, 14f, 68f, 70f);
        pen.Poly(pen.accent, 32f, 14f, 44f, 14f, 32f, 70f);
        pen.Poly(pen.accent, 70f, 14f, 82f, 14f, 68f, 70f);
        DropShape(pen, pen.main, 68f, 88f, 0.22f);
        DropShape(pen, pen.main, 32f, 88f, 0.18f);
    }

    // War Banner: a flag with chevrons
    static void WarBanner(IconPen pen)
    {
        Flag(pen);
        pen.Poly(pen.back, 32f, 20f, 42f, 20f, 54f, 34f, 42f, 48f, 32f, 48f, 44f, 34f);
        pen.Poly(pen.back, 52f, 20f, 62f, 20f, 74f, 34f, 62f, 48f, 52f, 48f, 64f, 34f);
    }

    // Zeal: a raised fist under a halo
    static void Fist(IconPen pen)
    {
        pen.EllipseRing(pen.main, 50f, 10f, 22f, 6f, 5f);
        pen.Rect(pen.main, 34f, 66f, 32f, 30f, 4f);
        pen.Rect(pen.main, 24f, 28f, 52f, 44f, 10f);
        pen.Rect(pen.back, 36f, 28f, 3f, 22f);
        pen.Rect(pen.back, 49f, 28f, 3f, 22f);
        pen.Rect(pen.back, 62f, 28f, 3f, 22f);
        pen.Rect(pen.accent, 18f, 46f, 36f, 12f, 6f);
    }

    // A flask, round, its neck up
    static void Flask(IconPen pen)
    {
        pen.Rect(pen.main, 34f, 6f, 32f, 10f, 4f);
        pen.Rect(pen.main, 41f, 14f, 18f, 28f);
        pen.Circle(pen.main, 50f, 64f, 32f);
    }

    // An arrow from its tail (x0, y0) to its point (x1, y1)
    static void Arrow(IconPen pen, IconColor color, float x0, float y0, float x1, float y1)
    {
        float length = (float)System.Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        float dx = (x1 - x0) / length;
        float dy = (y1 - y0) / length;
        pen.Line(color, x0, y0, x1 - dx * 12f, y1 - dy * 12f, 6f);
        pen.Poly(color, x1, y1, x1 - dx * 20f - dy * 11f, y1 - dy * 20f + dx * 11f, x1 - dx * 20f + dy * 11f, y1 - dy * 20f - dx * 11f);
        pen.Line(color, x0 + dx * 4f, y0 + dy * 4f, x0 - dx * 4f - dy * 9f, y0 - dy * 4f + dx * 9f, 4f);
        pen.Line(color, x0 + dx * 4f, y0 + dy * 4f, x0 - dx * 4f + dy * 9f, y0 - dy * 4f - dx * 9f, 4f);
    }

    // A sword from its pommel (x0, y0) to its point (x1, y1), outlined in the frame's colour to stand over another
    static void DiagonalSword(IconPen pen, float x0, float y0, float x1, float y1)
    {
        float length = (float)System.Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        float dx = (x1 - x0) / length;
        float dy = (y1 - y0) / length;
        float gx = x0 + dx * 20f;
        float gy = y0 + dy * 20f;
        pen.Line(pen.back, gx, gy, x1, y1, 16f);
        pen.Line(pen.main, gx, gy, x1, y1, 10f);
        pen.Line(pen.back, gx - dy * 14f, gy + dx * 14f, gx + dy * 14f, gy - dx * 14f, 12f);
        pen.Line(pen.main, gx - dy * 13f, gy + dx * 13f, gx + dy * 13f, gy - dx * 13f, 7f);
        pen.Line(pen.main, x0, y0, gx, gy, 7f);
    }

    // A dagger pointing up, centred on x
    static void Dagger(IconPen pen, IconColor color, float cx)
    {
        pen.Poly(color, cx, 2f, cx + 10f, 16f, cx + 10f, 60f, cx - 10f, 60f, cx - 10f, 16f);
        pen.Rect(color, cx - 18f, 58f, 36f, 9f, 4f);
        pen.Rect(color, cx - 5f, 66f, 10f, 20f, 3f);
        pen.Circle(color, cx, 90f, 7f);
    }

    // A leaf from its stem (x0, y0) to its tip (x1, y1), at its widest in the middle
    static void Leaf(IconPen pen, IconColor color, float x0, float y0, float x1, float y1, float width)
    {
        float length = (float)System.Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        float nx = -(y1 - y0) / length;
        float ny = (x1 - x0) / length;
        pen.Curve(color, t =>
        {
            float along = t < 0.5f ? t * 2f : 2f - t * 2f;
            float side = (t < 0.5f ? 1f : -1f) * (float)System.Math.Sin(along * System.Math.PI) * width / 2f;
            return (x0 + (x1 - x0) * along + nx * side, y0 + (y1 - y0) * along + ny * side);
        });
    }

    // A lightning bolt centred on (cx, cy)
    static void Bolt(IconPen pen, IconColor color, float cx, float cy, float scale)
    {
        float X(float x) => cx + (x - 50f) * scale;
        float Y(float y) => cy + (y - 50f) * scale;
        pen.Poly(color, X(60f), Y(2f), X(20f), Y(56f), X(46f), Y(56f), X(36f), Y(98f), X(80f), Y(40f), X(54f), Y(40f), X(68f), Y(2f));
    }

    // A drop centred on (cx, cy), its tip up
    static void DropShape(IconPen pen, IconColor color, float cx, float cy, float scale)
    {
        pen.Poly(color, cx, cy - 46f * scale, cx + 26f * scale, cy, cx - 26f * scale, cy);
        pen.Circle(color, cx, cy + 12f * scale, 28f * scale);
    }

    // A heart centred on (cx, cy), 32 units wide at a scale of 1
    static void HeartAt(IconPen pen, IconColor color, float cx, float cy, float scale)
    {
        pen.Curve(color, t =>
        {
            double a = t * 2 * System.Math.PI;
            double x = 16 * System.Math.Pow(System.Math.Sin(a), 3);
            double y = -(13 * System.Math.Cos(a) - 5 * System.Math.Cos(2 * a) - 2 * System.Math.Cos(3 * a) - System.Math.Cos(4 * a));
            return (cx + (float)x * scale, cy + (float)y * scale);
        });
    }

    // A flame centred on (cx, cy)
    static void Flame(IconPen pen, IconColor color, float cx, float cy, float scale)
    {
        pen.Poly(color, cx, cy - 26f * scale, cx + 17f * scale, cy, cx - 17f * scale, cy);
        pen.Circle(color, cx, cy + 6f * scale, 17f * scale);
    }

    // A mortar shell, standing, centred on x
    static void Shell(IconPen pen, IconColor color, float cx)
    {
        pen.Ellipse(color, cx, 38f, 20f, 32f);
        pen.Rect(color, cx - 20f, 38f, 40f, 44f);
        pen.Rect(pen.accent, cx - 20f, 60f, 40f, 9f);
        pen.Rect(color, cx - 25f, 80f, 50f, 14f, 4f);
    }

    // A flag on its pole, flying right
    static void Flag(IconPen pen)
    {
        pen.Rect(pen.main, 14f, 6f, 9f, 90f, 4f);
        pen.Circle(pen.main, 18.5f, 7f, 7f);
        pen.Poly(pen.main, 23f, 12f, 90f, 12f, 76f, 34f, 90f, 56f, 23f, 56f);
    }

    // A hooded acolyte, room on its chest for its sign
    static void Acolyte(IconPen pen)
    {
        pen.Poly(pen.main, 24f, 40f, 76f, 40f, 94f, 98f, 6f, 98f);
        pen.Ellipse(pen.main, 50f, 36f, 28f, 32f);
        pen.Ellipse(pen.back, 50f, 42f, 15f, 18f);
        pen.Circle(pen.accent, 44f, 42f, 3f);
        pen.Circle(pen.accent, 56f, 42f, 3f);
        pen.Circle(pen.accent, 50f, 78f, 15f);
    }

    // A band of a ring, from one angle to another (degrees, clockwise from the right)
    static void ArcBand(IconPen pen, IconColor color, float cx, float cy, float radius, float thickness, float from, float to)
    {
        const int steps = 40;
        var xy = new float[(steps + 1) * 4];
        float outer = radius + thickness / 2f;
        float inner = radius - thickness / 2f;
        for (int i = 0; i <= steps; i++)
        {
            double a = (from + (to - from) * i / steps) * System.Math.PI / 180;
            xy[i * 2] = cx + (float)System.Math.Cos(a) * outer;
            xy[i * 2 + 1] = cy + (float)System.Math.Sin(a) * outer;
            int back = (steps + 1) * 2 + (steps - i) * 2;
            xy[back] = cx + (float)System.Math.Cos(a) * inner;
            xy[back + 1] = cy + (float)System.Math.Sin(a) * inner;
        }
        pen.Poly(color, xy);
    }

    // A band of the box's circle ending in an arrowhead at its "to" angle
    static void ArcArrow(IconPen pen, IconColor color, float from, float to)
    {
        ArcBand(pen, color, 50f, 50f, 34f, 12f, from, to);
        double a = to * System.Math.PI / 180;
        float x = 50f + (float)System.Math.Cos(a) * 34f;
        float y = 50f + (float)System.Math.Sin(a) * 34f;
        float tx = -(float)System.Math.Sin(a);
        float ty = (float)System.Math.Cos(a);
        float nx = (float)System.Math.Cos(a);
        float ny = (float)System.Math.Sin(a);
        pen.Poly(color, x + nx * 16f, y + ny * 16f, x + tx * 18f, y + ty * 18f, x - nx * 16f, y - ny * 16f);
    }

    // A star of points, alternating between an outer and an inner radius
    static void Star(IconPen pen, IconColor color, float cx, float cy, float outer, float inner, int points)
    {
        var xy = new float[points * 4];
        for (int i = 0; i < points * 2; i++)
        {
            double a = i * System.Math.PI / points - System.Math.PI / 2;
            float radius = i % 2 == 0 ? outer : inner;
            xy[i * 2] = cx + (float)System.Math.Cos(a) * radius;
            xy[i * 2 + 1] = cy + (float)System.Math.Sin(a) * radius;
        }
        pen.Poly(color, xy);
    }

    // A small pair of scales in the corner, telling the balance's copy of an item
    static void ScalesBadge(IconPen pen)
    {
        pen.Circle(pen.back.Darken(0.45f), 82f, 82f, 19f);
        pen.Circle(pen.main, 82f, 82f, 16f);
        pen.Line(pen.back, 82f, 72f, 82f, 92f, 3f);
        pen.Line(pen.back, 70f, 76f, 94f, 76f, 3f);
        pen.Poly(pen.back, 70f, 78f, 76f, 87f, 64f, 87f);
        pen.Poly(pen.back, 94f, 78f, 100f, 87f, 88f, 87f);
        pen.Rect(pen.back, 75f, 91f, 14f, 3f);
    }
}

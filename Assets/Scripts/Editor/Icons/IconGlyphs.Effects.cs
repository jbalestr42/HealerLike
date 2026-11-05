// The pictograms of the effects that come from no item, spell or status (the bosses' phases), see IconCatalog
public static partial class IconGlyphs
{
    // Arbiter's Balance: scales on a pillar
    static void Scales(IconPen pen)
    {
        pen.Rect(pen.main, 45f, 14f, 10f, 70f, 3f);
        pen.Rect(pen.main, 28f, 82f, 44f, 10f, 4f);
        pen.Line(pen.main, 12f, 26f, 88f, 26f, 8f);
        pen.Circle(pen.accent, 50f, 16f, 8f);
        foreach (float x in new[] { 22f, 78f })
        {
            pen.Line(pen.main, x, 26f, x - 12f, 56f, 4f);
            pen.Line(pen.main, x, 26f, x + 12f, 56f, 4f);
            pen.Poly(pen.accent, x - 18f, 56f, x + 18f, 56f, x + 10f, 68f, x - 10f, 68f);
        }
    }

    // Arbiter's Judgment: a sword falling, point down
    static void SwordDown(IconPen pen)
    {
        pen.Poly(pen.main, 50f, 98f, 64f, 84f, 64f, 36f, 36f, 36f, 36f, 84f);
        pen.Poly(pen.accent, 50f, 98f, 64f, 84f, 64f, 36f, 50f, 36f);
        pen.Rect(pen.main, 22f, 27f, 56f, 12f, 6f);
        pen.Rect(pen.main, 43f, 12f, 14f, 18f);
        pen.Circle(pen.main, 50f, 10f, 8f);
    }

    // Arbiter's Verdict: a lightning bolt
    static void Bolt(IconPen pen)
    {
        pen.Poly(pen.main, 58f, 2f, 22f, 56f, 46f, 56f, 38f, 98f, 78f, 40f, 54f, 40f, 66f, 2f);
        pen.Poly(pen.accent, 58f, 2f, 66f, 2f, 54f, 40f, 46f, 40f);
    }

    // Ravager's Slam: a fist striking the ground
    static void GroundSlam(IconPen pen)
    {
        pen.Rect(pen.main, 30f, 10f, 40f, 46f, 10f);
        pen.Rect(pen.accent, 30f, 40f, 40f, 16f, 6f);
        pen.Rect(pen.main, 6f, 82f, 88f, 10f, 4f);
        pen.Poly(pen.main, 50f, 62f, 56f, 76f, 44f, 76f);
        pen.Line(pen.main, 24f, 64f, 14f, 74f, 6f);
        pen.Line(pen.main, 76f, 64f, 86f, 74f, 6f);
    }

    // Ravager's Strikes: three blows marked on the ground
    static void Strikes(IconPen pen)
    {
        foreach (float x in new[] { 22f, 50f, 78f })
        {
            pen.Ring(pen.main, x, 70f, 14f, 6f);
            pen.Poly(pen.accent, x, 64f, x - 8f, 22f, x + 8f, 22f);
        }
    }

    // Hit armor (on the HUD): a shield blocking a hit
    static void ShieldBlock(IconPen pen)
    {
        Shield(pen, pen.main, pen.accent, 50f, 54f, 0.84f);
        pen.Poly(pen.back, 40f, 30f, 56f, 46f, 48f, 50f, 62f, 66f, 44f, 54f, 50f, 50f);
    }
}

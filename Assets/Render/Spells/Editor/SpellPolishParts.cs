using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Spells.Editor
{
    // Small, shared authoring functions. Runtime motion reads the saved composition, never these labels.
    public static class SpellPolishParts
    {
        public static LookPart Part(string id, Primitive primitive, ShapeProfile shape, Vector3 position,
            Vector3 size, Vector3 rotation = default, ColourRole colour = ColourRole.Accent)
        {
            return new LookPart { id = id, primitive = primitive, shape = shape, role = PartRole.Body,
                colour = colour, position = position, size = size, euler = rotation, glow = .12f };
        }

        public static LookPart Ring(string id, float diameter, float height, float tube = .08f,
            ColourRole colour = ColourRole.Accent, Vector3 rotation = default)
        {
            return Part(id, Primitive.Torus, ShapeProfile.Ring(tube), new Vector3(0, height, 0),
                new Vector3(diameter, .16f, diameter), rotation, colour);
        }

        public static Vector3 Radial(int index, int count, float radius, float height, float offset = 0)
        {
            float angle = (index * 360f / count + offset) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
        }

        public static LookPart[] Petals(string id, int count, float radius, float height, Vector3 size,
            bool mineral = false, float lean = 0, float angleOffset = 0)
        {
            var parts = new LookPart[count];
            for (int i = 0; i < count; i++)
            {
                int slot = count == 6 ? (i % 3) * 2 + i / 3 : i;
                parts[i] = Part(id + i, mineral ? Primitive.Pyramid : Primitive.Leaf,
                    mineral ? ShapeProfile.Shard(.72f, .04f, .35f) : ShapeProfile.Leaf(.12f, .85f, .25f),
                    Radial(slot, count, radius, height, angleOffset), size,
                    new Vector3(lean, slot * 360f / count + angleOffset, 0));
            }
            return parts;
        }

        public static LookPart[] Motes(string id, int count, float radius, float height, float size, bool drops)
        {
            var parts = new LookPart[count];
            for (int i = 0; i < count; i++)
                parts[i] = Part(id + i, drops ? Primitive.Cone : Primitive.Sphere,
                    ShapeProfile.Bulb(drops ? .8f : 1f, drops ? .72f : .1f),
                    Radial(i, count, radius * (1 + .12f * (i % 2)), height + .12f * (i % 3)),
                    new Vector3(size, size * (drops ? 1.7f : 1.2f), size),
                    Vector3.zero);
            return parts;
        }

        public static LookPart[] Heal()
        {
            var parts = new List<LookPart>();
            // Three readable leaves form the smallest heal; larger heals add a constellation of pearls.
            parts.AddRange(Petals("Healing leaf ", 3, 1.65f, -.12f,
                new Vector3(.65f, 1.15f, .24f), false, 18));
            parts.AddRange(Motes("Healing pearl ", 5, 1.85f, -.05f, .38f, false));
            return parts.ToArray();
        }

        public static LookPart[] Stalks()
        {
            var parts = new List<LookPart>();
            LookPart[] crowns = Petals("Renewal petal ", 6, 1.55f, 1.2f, new Vector3(.6f, .85f, .25f));
            for (int i = 0; i < crowns.Length; i++)
            {
                parts.Add(crowns[i]);
                LookPart stem = Part("Renewal stem " + i, Primitive.CylinderSegment,
                    ShapeProfile.Segment(.2f, .25f), crowns[i].position * .5f,
                    new Vector3(.075f, 1f, .075f), colour: ColourRole.Accent);
                stem.role = PartRole.Stem;
                parts.Add(stem);
            }
            return parts.ToArray();
        }

        public static LookPart[] Beam()
        {
            var parts = new List<LookPart>();
            for (int i = 0; i < 12; i++)
            {
                LookPart segment = Part("Ribbon segment " + i, Primitive.CylinderSegment,
                    ShapeProfile.Segment(0, .3f), new Vector3(i / 12f, 0, 0), new Vector3(.045f, 1, .045f));
                segment.role = PartRole.Stem;
                parts.Add(segment);
            }
            for (int i = 0; i < 6; i++)
                parts.Add(Part("Travelling pearl " + i, Primitive.Sphere, ShapeProfile.Bulb(),
                    new Vector3(i / 6f, 0, 0), Vector3.one * .14f));
            return parts.ToArray();
        }

        public static LookPart[] Burst()
        {
            var parts = new List<LookPart>();
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                parts.Add(Part("Impact ray " + i, Primitive.Pyramid, ShapeProfile.Shard(.72f, 0, .3f),
                    new Vector3(Mathf.Sin(angle) * .6f, Mathf.Cos(angle) * .6f, 0),
                    new Vector3(.36f, 1.6f, .13f), new Vector3(0, 0, -i * 60f)));
            }
            parts.Add(Ring("Impact halo", 2.3f, 0, .065f, rotation: new Vector3(90, 0, 0)));
            parts.Add(Part("Impact heart", Primitive.Sphere, ShapeProfile.Bulb(), Vector3.zero,
                new Vector3(.5f, .5f, .2f), colour: ColourRole.MushroomCapPale));
            return parts.ToArray();
        }

        public static LookPart[] Press()
        {
            var parts = new LookPart[5];
            for (int i = 0; i < parts.Length; i++)
                parts[i] = Part("Pressure thorn " + i, Primitive.Cone, ShapeProfile.Shard(.9f, 0),
                    Radial(i, 5, .7f, .3f), new Vector3(.35f, .75f, .35f), new Vector3(0, 0, 180));
            for (int i = 0; i < parts.Length; i++) parts[i].glow = .8f;
            return parts;
        }

        public static LookPart[] Zone(bool hostile)
        {
            var parts = new List<LookPart> { Ring("Boundary", 2f, .16f, .09f),
                Ring("Inner boundary", 1.82f, .13f, .075f) };
            parts.AddRange(Petals("Boundary marker ", 8, .95f, .18f,
                new Vector3(.12f, hostile ? .22f : .17f, .06f), hostile, hostile ? 30 : 90));
            return parts.ToArray();
        }

        // Stone magic is broken and stacked: chipped slabs, blunt wedges, straight shards and flat capstones.
        // Never a sphere, a bead chain or a smooth curve, and never a colour of its own: roles only.
        public static ShapeProfile Slab(float fracture = .45f)
        {
            return ShapeProfile.Block(.08f, .12f, .18f, fracture, .15f);
        }

        public static LookPart FacetedRing(string id, float diameter, float height, float tube = .08f,
            ColourRole colour = ColourRole.Accent, Vector3 rotation = default)
        {
            LookPart ring = Ring(id, diameter, height, tube, colour, rotation);
            ring.shape = ShapeProfile.Ring(tube, true);
            return ring;
        }

        public static LookPart[] Chips(string id, int count, float radius, float height, float size)
        {
            var parts = new LookPart[count];
            for (int i = 0; i < count; i++)
                parts[i] = Part(id + i, Primitive.Boulder, Slab(.6f),
                    Radial(i, count, radius, height + .1f * (i % 2)),
                    new Vector3(size * 1.3f, size * .7f, size), new Vector3(0, i * 47f, 8f * (i % 3 - 1)));
            return parts;
        }

        // Where plant Burst sprays pointed rays, stone Burst sprays straight fractured shards of uneven length
        // around a chipped core
        public static LookPart[] StoneBurst()
        {
            var parts = new List<LookPart>();
            float[] lengths = { 1.7f, 1.2f, 1.55f, 1.05f, 1.6f, 1.3f, 1.15f };
            for (int i = 0; i < lengths.Length; i++)
            {
                float turn = i * 360f / lengths.Length + (i % 2 == 0 ? 6f : -9f);
                float angle = turn * Mathf.Deg2Rad;
                float reach = .45f + .1f * lengths[i];
                parts.Add(Part("Stone shard " + i, Primitive.Pyramid, ShapeProfile.Shard(.8f, 0, .55f),
                    new Vector3(Mathf.Sin(angle) * reach, Mathf.Cos(angle) * reach, 0),
                    new Vector3(.3f + .05f * (i % 3), lengths[i], .2f), new Vector3(0, 0, -turn)));
            }
            parts.Add(Part("Stone core", Primitive.Boulder, Slab(.7f), Vector3.zero,
                new Vector3(.62f, .5f, .3f), new Vector3(0, 0, 17f), ColourRole.MushroomCapPale));
            return parts.ToArray();
        }

        // Where plant Rise lifts leaves and pearls, stone Rise lifts three stacks of flat slabs: the three base
        // slabs always show and a larger heal adds a course to every stack
        public static LookPart[] StoneRise()
        {
            var parts = new List<LookPart>();
            int[] courses = { 3, 3, 2 };
            for (int course = 0; course < courses.Length; course++)
                for (int stack = 0; stack < courses[course]; stack++)
                {
                    Vector3 foot = Radial(stack, 3, 1.6f, -.25f, 18f);
                    float shrink = 1f - .16f * course;
                    parts.Add(Part("Rising slab " + (course * 3 + stack), Primitive.Boulder, Slab(),
                        foot + Vector3.up * (.3f * course), new Vector3(.62f * shrink, .2f, .5f * shrink),
                        new Vector3(4f * (course - 1), stack * 120f + course * 23f, 6f * (stack - 1))));
                }
            return parts.ToArray();
        }

        // Where plant Press bears down with thorns, stone Press bears down with one heavy capstone set on blunt
        // wedges; each stack adds a wedge under its rim
        public static LookPart[] StonePress()
        {
            var parts = new LookPart[5];
            parts[0] = Part("Capstone", Primitive.Boulder, ShapeProfile.Block(.1f, .05f, .12f, .35f, .1f),
                new Vector3(0, .62f, 0), new Vector3(1.7f, .32f, 1.45f), new Vector3(0, 21f, 3f));
            for (int i = 1; i < parts.Length; i++)
                parts[i] = Part("Bearing wedge " + i, Primitive.Pyramid, ShapeProfile.Shard(.45f, 0, .5f),
                    Radial(i - 1, 4, .55f, .3f, 45f), new Vector3(.36f, .5f, .3f), new Vector3(0, i * 90f + 45f, 180));
            for (int i = 0; i < parts.Length; i++) parts[i].glow = .8f;
            return parts;
        }
    }
}

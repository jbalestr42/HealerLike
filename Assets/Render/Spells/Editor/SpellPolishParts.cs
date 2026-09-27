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
    }
}

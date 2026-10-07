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
                colour = colour, position = position, size = size, euler = rotation, glow = 0.12f };
        }

        public static LookPart Ring(string id, float diameter, float height, float tube = 0.08f,
            ColourRole colour = ColourRole.Accent, Vector3 rotation = default)
        {
            return Part(id, Primitive.Torus, ShapeProfile.Ring(tube), new Vector3(0, height, 0),
                new Vector3(diameter, 0.16f, diameter), rotation, colour);
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
                    mineral ? ShapeProfile.Shard(0.72f, 0.04f, 0.35f) : ShapeProfile.Leaf(0.12f, 0.85f, 0.25f),
                    Radial(slot, count, radius, height, angleOffset), size,
                    new Vector3(lean, slot * 360f / count + angleOffset, 0));
            }
            return parts;
        }

        public static LookPart[] Motes(string id, int count, float radius, float height, float size, bool drops)
        {
            var parts = new LookPart[count];
            for (int i = 0; i < count; i++)
            {
                parts[i] = Part(id + i, drops ? Primitive.Cone : Primitive.Sphere,
                    ShapeProfile.Bulb(drops ? 0.8f : 1f, drops ? 0.72f : 0.1f),
                    Radial(i, count, radius * (1 + 0.12f * (i % 2)), height + 0.12f * (i % 3)),
                    new Vector3(size, size * (drops ? 1.7f : 1.2f), size),
                    Vector3.zero);
            }

            return parts;
        }

        public static LookPart[] Heal()
        {
            var parts = new List<LookPart>();
            // Three readable leaves form the smallest heal; larger heals add a constellation of pearls.
            parts.AddRange(Petals("Healing leaf ", 3, 1.65f, -0.12f,
                new Vector3(0.65f, 1.15f, 0.24f), false, 18));
            parts.AddRange(Motes("Healing pearl ", 5, 1.85f, -0.05f, 0.38f, false));
            return parts.ToArray();
        }

        public static LookPart[] Stalks()
        {
            var parts = new List<LookPart>();
            LookPart[] crowns = Petals("Renewal petal ", 6, 1.55f, 1.2f, new Vector3(0.6f, 0.85f, 0.25f));
            for (int i = 0; i < crowns.Length; i++)
            {
                parts.Add(crowns[i]);
                LookPart stem = Part("Renewal stem " + i, Primitive.CylinderSegment,
                    ShapeProfile.Segment(0.2f, 0.25f), crowns[i].position * 0.5f,
                    new Vector3(0.075f, 1f, 0.075f), colour: ColourRole.Accent);
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
                    ShapeProfile.Segment(0, 0.3f), new Vector3(i / 12f, 0, 0), new Vector3(0.045f, 1, 0.045f));
                segment.role = PartRole.Stem;
                parts.Add(segment);
            }
            for (int i = 0; i < 6; i++)
            {
                parts.Add(Part("Travelling pearl " + i, Primitive.Sphere, ShapeProfile.Bulb(),
                    new Vector3(i / 6f, 0, 0), Vector3.one * 0.14f));
            }

            return parts.ToArray();
        }

        public static LookPart[] Burst()
        {
            var parts = new List<LookPart>();
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                parts.Add(Part("Impact ray " + i, Primitive.Pyramid, ShapeProfile.Shard(0.72f, 0, 0.3f),
                    new Vector3(Mathf.Sin(angle) * 0.6f, Mathf.Cos(angle) * 0.6f, 0),
                    new Vector3(0.36f, 1.6f, 0.13f), new Vector3(0, 0, -i * 60f)));
            }
            parts.Add(Ring("Impact halo", 2.3f, 0, 0.065f, rotation: new Vector3(90, 0, 0)));
            parts.Add(Part("Impact heart", Primitive.Sphere, ShapeProfile.Bulb(), Vector3.zero,
                new Vector3(0.5f, 0.5f, 0.2f), colour: ColourRole.MushroomCapPale));
            return parts.ToArray();
        }

        public static LookPart[] Press()
        {
            var parts = new LookPart[5];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Part("Pressure thorn " + i, Primitive.Cone, ShapeProfile.Shard(0.9f, 0),
                    Radial(i, 5, 0.7f, 0.3f), new Vector3(0.35f, 0.75f, 0.35f), new Vector3(0, 0, 180));
            }

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].glow = 0.8f;
            }

            return parts;
        }

        public static LookPart[] Zone(bool hostile)
        {
            var parts = new List<LookPart> { Ring("Boundary", 2f, 0.16f, 0.09f),
                Ring("Inner boundary", 1.82f, 0.13f, 0.075f) };
            parts.AddRange(Petals("Boundary marker ", 8, 0.95f, 0.18f,
                new Vector3(0.12f, hostile ? 0.22f : 0.17f, 0.06f), hostile, hostile ? 30 : 90));
            return parts.ToArray();
        }

        // Boon kinds: what separates boons that agree on every other channel is the buff that builds them. Plant
        // kit only: smooth bulbs, teardrop seeds, tapering leaf blades, capsule segments, bead chains
        public static ShapeProfile Seed() { return ShapeProfile.Bulb(0.8f, 0.72f); }

        // Projectile: one leaf-bladed dart standing on its own axis, a teardrop seed for a point and two
        // fletching leaves; every part sits on that axis so the spin turns it whole
        public static LookPart[] Dart()
        {
            var parts = new List<LookPart>();
            // In front of the body and to one side, so the body never hides it from the board camera
            Vector3 axis = new Vector3(1.35f, 0, -0.7f);
            parts.Add(Part("Dart blade", Primitive.Leaf, ShapeProfile.Leaf(0.04f, 0.5f), axis + Vector3.up * 0.2f,
                new Vector3(0.5f, 2f, 0.14f)));
            parts.Add(Part("Dart seed", Primitive.Cone, Seed(), axis + Vector3.up * 1.4f,
                new Vector3(0.38f, 0.6f, 0.38f), colour: ColourRole.MushroomCapPale));
            for (int i = 0; i < 2; i++)
            {
                parts.Add(Part("Dart fletching " + i, Primitive.Leaf, ShapeProfile.Leaf(0.2f, 0.7f),
                    axis + Vector3.down * 0.75f, new Vector3(0.36f, 0.75f, 0.12f), new Vector3(0, 90f * i, i == 0 ? 38f : -38f)));
            }

            return parts.ToArray();
        }

        // Volume: a dense clump of many small seeds, each pointing out of the clump
        public static LookPart[] Seeds()
        {
            var parts = new List<LookPart>();
            int count = 13;
            for (int i = 0; i < count; i++)
            {
                // Fibonacci sphere, a tight even clump without a ring reading
                float y = 1f - 2f * (i + 0.5f) / count;
                float radius = Mathf.Sqrt(1f - y * y);
                float angle = i * 137.5f * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                parts.Add(Part("Clustered seed " + i, Primitive.Cone, Seed(), direction * 0.48f + Vector3.up * 0.35f,
                    new Vector3(0.2f, 0.34f, 0.2f), Quaternion.FromToRotation(Vector3.up, direction).eulerAngles));
            }
            return parts.ToArray();
        }

        // Rate: a tight stack of thin rings, one beat per ring
        public static LookPart[] Cadence()
        {
            var parts = new LookPart[5];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Ring("Cadence ring " + i, 2.5f - 0.12f * (i % 2), -0.35f + 0.2f * i, 0.07f);
            }

            return parts;
        }

        // Conditional: two curved leaf brackets facing each other across the body, a bead chain between their
        // feet, that open and close
        public static LookPart[] Brackets()
        {
            var parts = new List<LookPart>();
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? 1f : -1f;
                parts.Add(Part("Bracket leaf " + i, Primitive.Leaf, ShapeProfile.Leaf(0.65f, 0.6f, 0.4f),
                    new Vector3(side * 1.3f, 0.1f, -0.2f), new Vector3(0.8f, 2.1f, 0.16f), new Vector3(0, side * 25f, 0)));
            }
            for (int i = 0; i < 3; i++)
            {
                parts.Add(Part("Bracket bead " + i, Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(-0.5f + 0.5f * i, -0.8f, 0),
                    Vector3.one * 0.18f));
            }

            return parts.ToArray();
        }

        // Positional: a low ring of seeds lying on the ground round the feet, four leaf ticks pointing out to the
        // neighbouring cells
        public static LookPart[] Footring()
        {
            var parts = new List<LookPart>();
            for (int i = 0; i < 8; i++)
            {
                float turn = i * 45f + 22.5f;
                // Tipped out from the feet and raised over the blades, so the grass does not swallow them
                parts.Add(Part("Ground seed " + i, Primitive.Cone, Seed(), Radial(i, 8, 1.5f, 0.3f, 22.5f),
                    new Vector3(0.3f, 0.55f, 0.3f), new Vector3(55, turn, 0)));
            }
            for (int i = 0; i < 4; i++)
            {
                parts.Add(Part("Cell tick " + i, Primitive.Leaf, ShapeProfile.Leaf(0.05f, 0.6f), Radial(i, 4, 1.95f, 0.25f),
                    new Vector3(0.3f, 0.7f, 0.1f), new Vector3(70, i * 90f, 0)));
            }

            return parts.ToArray();
        }

        // Flat: one broad flat leaf plate held above the head on a capsule stem with a spherical knuckle
        public static LookPart[] Canopy()
        {
            return new[] {
                Part("Canopy leaf", Primitive.Leaf, ShapeProfile.Leaf(0.1f, 1.1f, 0.35f), new Vector3(0, 0.55f, 0),
                    new Vector3(1.5f, 1.8f, 0.12f), new Vector3(90, 0, 0)),
                Part("Canopy stem", Primitive.CylinderSegment, ShapeProfile.Segment(0.2f, 0.3f), new Vector3(0, 0.15f, 0),
                    new Vector3(0.12f, 0.5f, 0.12f)),
                Part("Canopy knuckle", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(0, 0.42f, 0),
                    Vector3.one * 0.2f)
            };
        }

        // Reactive: a coiled tendril at the shoulder with a bead caught in it and three thorns that snap out of
        // the coil, in front and to one side so the body never hides it
        public static LookPart[] Spark()
        {
            var parts = new List<LookPart>();
            Vector3 coil = new Vector3(-1.35f, 0.5f, -0.8f);
            parts.Add(Part("Spark coil", Primitive.Torus, ShapeProfile.Ring(0.16f), coil, new Vector3(1.3f, 0.2f, 1.3f),
                new Vector3(70, 0, 0)));
            parts.Add(Part("Spark bead", Primitive.Sphere, ShapeProfile.Bulb(), coil, Vector3.one * 0.45f));
            for (int i = 0; i < 3; i++)
            {
                float turn = -45f + 45f * i;
                Vector3 direction = Quaternion.Euler(0, 0, turn) * Vector3.up;
                parts.Add(Part("Spark thorn " + i, Primitive.Cone, Seed(), coil + direction * 1.05f,
                    new Vector3(0.34f, 0.8f, 0.34f), new Vector3(0, 0, turn)));
            }
            return parts.ToArray();
        }

        // Echo: one leaf blade struck and two copies of it trailing behind, each smaller and further back
        public static LookPart[] Echo()
        {
            var parts = new LookPart[3];
            for (int i = 0; i < parts.Length; i++)
            {
                float shrink = 1f - 0.18f * i;
                parts[i] = Part("Echo blade " + i, Primitive.Leaf, ShapeProfile.Leaf(0.06f, 0.7f),
                    new Vector3(1.2f + 0.6f * i, 0.35f + 0.15f * i, -0.8f + 0.25f * i), new Vector3(0.75f, 2.2f, 0.16f) * shrink,
                    new Vector3(0, 0, -28f));
            }
            return parts;
        }

        // Link: two vine rings hooked through each other over the head, a bead on each, one life held by two
        public static LookPart[] Tether()
        {
            return new[] {
                Part("Tether ring 0", Primitive.Torus, ShapeProfile.Ring(0.1f), new Vector3(-0.42f, 0.35f, 0),
                    new Vector3(1.15f, 0.16f, 1.15f), new Vector3(90, 0, 0)),
                Part("Tether ring 1", Primitive.Torus, ShapeProfile.Ring(0.1f), new Vector3(0.42f, 0.35f, 0),
                    new Vector3(1.15f, 0.16f, 1.15f), new Vector3(0, 0, 90)),
                Part("Tether bead 0", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(-0.98f, 0.35f, 0),
                    Vector3.one * 0.24f, colour: ColourRole.MushroomCapPale),
                Part("Tether bead 1", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(0.98f, 0.35f, 0),
                    Vector3.one * 0.24f, colour: ColourRole.MushroomCapPale)
            };
        }

        // Summon: a seedling breaking out of a mound of earth beside the body, two seed leaves on a short stem
        public static LookPart[] Sprout()
        {
            Vector3 at = new Vector3(1.35f, -0.7f, -0.85f);
            return new[] {
                Part("Sprout mound", Primitive.Sphere, ShapeProfile.Bulb(), at + Vector3.up * 0.15f,
                    new Vector3(1.2f, 0.5f, 1.2f)),
                Part("Sprout stem", Primitive.CylinderSegment, ShapeProfile.Segment(0.16f, 0.78f), at + Vector3.up * 0.8f,
                    new Vector3(0.22f, 1.2f, 0.22f)),
                Part("Sprout leaf 0", Primitive.Leaf, ShapeProfile.Leaf(0.3f, 0.8f, 0.3f), at + new Vector3(-0.5f, 1.5f, 0),
                    new Vector3(0.75f, 1.25f, 0.14f), new Vector3(0, 0, 55)),
                Part("Sprout leaf 1", Primitive.Leaf, ShapeProfile.Leaf(0.3f, 0.8f, 0.3f), at + new Vector3(0.5f, 1.5f, 0),
                    new Vector3(0.75f, 1.25f, 0.14f), new Vector3(0, 0, -55))
            };
        }

        // Growth: the creature growth language (GrowthStoneParts.Growth and Tip) as a spell, segments with a joint
        // on each and a bud on top, listed from the ground up so each stack shows one more of them
        // Clear of the blades and of the arm, leaning away from the camera so the segments climb the screen instead
        // of stacking into one disc under a camera that looks down
        static readonly Vector3 StemFoot = new Vector3(-1.95f, 0.35f, -1.35f);
        static readonly Vector3 StemLean = new Vector3(0, 0.7f, 0.3f);
        static Vector3 StemTilt { get { return new Vector3(Mathf.Atan2(StemLean.z, StemLean.y) * Mathf.Rad2Deg, 0, 0); } }
        static Vector3 StemFrom(int i) { return StemFoot + new Vector3(0.1f * (i % 2 == 0 ? 1 : -1), 0, 0) + StemLean * i; }

        public static LookPart[] Stem()
        {
            var parts = new List<LookPart>();
            for (int i = 0; i < 4; i++)
            {
                parts.Add(Part("Growth segment " + i, Primitive.CylinderSegment, ShapeProfile.Segment(0.16f, 0.78f),
                    StemFrom(i) + StemLean * 0.5f, new Vector3(0.5f, 0.76f, 0.5f), StemTilt));
                parts.Add(Part("Growth joint " + i, Primitive.Sphere, ShapeProfile.Bulb(), StemFrom(i) + StemLean,
                    Vector3.one * 0.5f * 0.72f));
            }
            parts.Add(Part("Growth bud", Primitive.Sphere, ShapeProfile.Bulb(0.9f, 0.25f), StemFoot + StemLean * 4.5f,
                new Vector3(0.6f, 0.8f, 0.6f), StemTilt));
            return parts.ToArray();
        }

        // Stone magic is broken and stacked: chipped slabs, blunt wedges, straight shards and flat capstones.
        // Never a sphere, a bead chain or a smooth curve, and never a colour of its own: roles only.
        public static ShapeProfile Slab(float fracture = 0.45f)
        {
            return ShapeProfile.Block(0.08f, 0.12f, 0.12f, fracture, 0.15f);
        }

        public static LookPart FacetedRing(string id, float diameter, float height, float tube = 0.08f,
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
            {
                parts[i] = Part(id + i, Primitive.Boulder, Slab(0.6f),
                    Radial(i, count, radius, height + 0.1f * (i % 2)),
                    new Vector3(size * 1.3f, size * 0.7f, size), new Vector3(0, i * 47f, 8f * (i % 3 - 1)));
            }

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
                float reach = 0.45f + 0.1f * lengths[i];
                parts.Add(Part("Stone shard " + i, Primitive.Pyramid, ShapeProfile.Shard(0.8f, 0, 0.55f),
                    new Vector3(Mathf.Sin(angle) * reach, Mathf.Cos(angle) * reach, 0),
                    new Vector3(0.3f + 0.05f * (i % 3), lengths[i], 0.2f), new Vector3(0, 0, -turn)));
            }
            parts.Add(Part("Stone core", Primitive.Boulder, Slab(0.7f), Vector3.zero,
                new Vector3(0.62f, 0.5f, 0.3f), new Vector3(0, 0, 17f), ColourRole.MushroomCapPale));
            return parts.ToArray();
        }

        // Where plant Rise lifts leaves and pearls, stone Rise lifts three stacks of flat slabs: the three base
        // slabs always show and a larger heal adds a course to every stack
        public static LookPart[] StoneRise()
        {
            var parts = new List<LookPart>();
            int[] courses = { 3, 3, 2 };
            for (int course = 0; course < courses.Length; course++)
            {
                for (int stack = 0; stack < courses[course]; stack++)
                {
                    Vector3 foot = Radial(stack, 3, 1.6f, -0.25f, 18f);
                    float shrink = 1f - 0.16f * course;
                    parts.Add(Part("Rising slab " + (course * 3 + stack), Primitive.Boulder, Slab(),
                        foot + Vector3.up * (0.3f * course), new Vector3(0.62f * shrink, 0.2f, 0.5f * shrink),
                        new Vector3(4f * (course - 1), stack * 120f + course * 23f, 6f * (stack - 1))));
                }
            }

            return parts.ToArray();
        }

        // Where plant Press bears down with thorns, stone Press bears down with one heavy capstone set on blunt
        // wedges; each stack adds a wedge under its rim
        public static LookPart[] StonePress()
        {
            var parts = new LookPart[5];
            parts[0] = Part("Capstone", Primitive.Boulder, ShapeProfile.Block(0.1f, 0.05f, 0.12f, 0.35f, 0.1f),
                new Vector3(0, 0.62f, 0), new Vector3(1.7f, 0.32f, 1.45f), new Vector3(0, 21f, 3f));
            for (int i = 1; i < parts.Length; i++)
            {
                parts[i] = Part("Bearing wedge " + i, Primitive.Pyramid, ShapeProfile.Shard(0.45f, 0, 0.5f),
                    Radial(i - 1, 4, 0.55f, 0.3f, 45f), new Vector3(0.36f, 0.5f, 0.3f), new Vector3(0, i * 90f + 45f, 180));
            }

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].glow = 0.8f;
            }

            return parts;
        }

        // The five event kinds in Stone, on the Plant kinds' places so socket, motion and head clearance hold

        // Reactive: a faceted ring at the shoulder round a chipped core, three straight shards snapping out of it
        public static LookPart[] StoneSpark()
        {
            var parts = new List<LookPart>();
            Vector3 coil = new Vector3(-1.35f, 0.5f, -0.8f);
            parts.Add(Part("Spark ring", Primitive.Torus, ShapeProfile.Ring(0.16f, true), coil, new Vector3(1.3f, 0.2f, 1.3f),
                new Vector3(70, 0, 0)));
            parts.Add(Part("Spark core", Primitive.Boulder, Slab(0.7f), coil, new Vector3(0.45f, 0.38f, 0.3f),
                new Vector3(0, 0, 17f)));
            for (int i = 0; i < 3; i++)
            {
                float turn = -45f + 45f * i;
                Vector3 direction = Quaternion.Euler(0, 0, turn) * Vector3.up;
                parts.Add(Part("Spark shard " + i, Primitive.Pyramid, ShapeProfile.Shard(0.8f, 0, 0.45f),
                    coil + direction * 1.05f, new Vector3(0.34f, 0.8f, 0.26f), new Vector3(0, 0, turn)));
            }
            return parts.ToArray();
        }

        // Echo: one straight shard struck and two copies of it trailing behind, each smaller and further back
        public static LookPart[] StoneEcho()
        {
            var parts = new LookPart[3];
            for (int i = 0; i < parts.Length; i++)
            {
                float shrink = 1f - 0.18f * i;
                parts[i] = Part("Echo shard " + i, Primitive.Pyramid, ShapeProfile.Shard(0.75f, 0, 0.35f),
                    new Vector3(1.2f + 0.6f * i, 0.35f + 0.15f * i, -0.8f + 0.25f * i), new Vector3(0.6f, 2.1f, 0.22f) * shrink,
                    new Vector3(0, 0, -28f));
            }
            return parts;
        }

        // Link: two faceted rings hooked through each other over the head, a chip on each
        public static LookPart[] StoneTether()
        {
            return new[] {
                Part("Tether ring 0", Primitive.Torus, ShapeProfile.Ring(0.1f, true), new Vector3(-0.42f, 0.35f, 0),
                    new Vector3(1.15f, 0.16f, 1.15f), new Vector3(90, 0, 0)),
                Part("Tether ring 1", Primitive.Torus, ShapeProfile.Ring(0.1f, true), new Vector3(0.42f, 0.35f, 0),
                    new Vector3(1.15f, 0.16f, 1.15f), new Vector3(0, 0, 90)),
                Part("Tether chip 0", Primitive.Boulder, Slab(0.6f), new Vector3(-0.98f, 0.35f, 0),
                    new Vector3(0.3f, 0.2f, 0.26f), new Vector3(0, 30f, 8f)),
                Part("Tether chip 1", Primitive.Boulder, Slab(0.6f), new Vector3(0.98f, 0.35f, 0),
                    new Vector3(0.3f, 0.2f, 0.26f), new Vector3(0, -40f, -8f))
            };
        }

        // Summon: a wedge breaking up through a cracked slab beside the body, two shards split off it like seed leaves
        public static LookPart[] StoneSprout()
        {
            Vector3 at = new Vector3(1.35f, -0.7f, -0.85f);
            return new[] {
                Part("Sprout slab", Primitive.Boulder, Slab(0.6f), at + Vector3.up * 0.15f, new Vector3(1.2f, 0.4f, 1.2f),
                    new Vector3(0, 23f, 4f)),
                Part("Sprout wedge", Primitive.Pyramid, ShapeProfile.Shard(0.5f, 0, 0.4f), at + Vector3.up * 0.8f,
                    new Vector3(0.26f, 1.2f, 0.26f)),
                Part("Sprout shard 0", Primitive.Pyramid, ShapeProfile.Shard(0.7f, 0, 0.35f), at + new Vector3(-0.5f, 1.5f, 0),
                    new Vector3(0.6f, 1.1f, 0.2f), new Vector3(0, 0, 55)),
                Part("Sprout shard 1", Primitive.Pyramid, ShapeProfile.Shard(0.7f, 0, 0.35f), at + new Vector3(0.5f, 1.5f, 0),
                    new Vector3(0.6f, 1.1f, 0.2f), new Vector3(0, 0, -55))
            };
        }

        // Growth: the Plant stem's path in chipped courses, a flat chip for each joint and the creature language's
        // stone bud (a shard) on top, listed from the ground up so each stack shows one more of them
        public static LookPart[] StoneStem()
        {
            var parts = new List<LookPart>();
            for (int i = 0; i < 4; i++)
            {
                parts.Add(Part("Growth course " + i, Primitive.Boulder, Slab(0.45f), StemFrom(i) + StemLean * 0.5f,
                    new Vector3(0.5f, 0.76f, 0.5f), StemTilt + new Vector3(0, 23f * i, 0)));
                parts.Add(Part("Growth chip " + i, Primitive.Boulder, Slab(0.6f), StemFrom(i) + StemLean,
                    new Vector3(0.46f, 0.14f, 0.46f), StemTilt + new Vector3(0, 40f + 23f * i, 0)));
            }
            parts.Add(Part("Growth bud", Primitive.Pyramid, ShapeProfile.Shard(0.62f, 0f), StemFoot + StemLean * 4.5f,
                new Vector3(0.6f, 0.8f, 0.6f), StemTilt));
            return parts.ToArray();
        }
    }
}

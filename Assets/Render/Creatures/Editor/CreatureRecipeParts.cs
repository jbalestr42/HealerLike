using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // The part lists of the authored creature recipes
    public static class CreatureRecipeParts
    {
        public static List<CreaturePart> Base()
        {
            return new List<CreaturePart>
            {
                CreatureRecipeAuthoring.Part("Stem", Primitive.Capsule, new Vector3(0f, 0.25f, 0f), new Vector3(0.11f, 0.5f, 0.11f), CreatureRecipeAuthoring.stem,
                    parent: -1, role: PartRole.Stem)
            };
        }

        public static List<CreaturePart> Healer()
        {
            List<CreaturePart> parts = Base();
            parts.Add(CreatureRecipeAuthoring.Part("Bulb", Primitive.Cone, new Vector3(0f, 0.48f, 0f), new Vector3(0.5f, 0.62f, 0.45f),
                CreatureRecipeAuthoring.body, new Vector3(0f, 0f, 180f)));
            parts.Add(CreatureRecipeAuthoring.Part("Hip", Primitive.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.38f, 0.30f, 0.38f),
                CreatureRecipeAuthoring.body));
            parts.Add(CreatureRecipeAuthoring.Part("Crown", Primitive.Torus, new Vector3(0f, 0.86f, 0f), new Vector3(0.60f, 0.3f, 0.48f),
                CreatureRecipeAuthoring.bud, glow: 0.4f, role: PartRole.Crown));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 0.24f, 0.69f, Mathf.Sin(angle) * 0.24f);
                Vector3 euler = new Vector3(0f, -angle * Mathf.Rad2Deg, -25f);
                Vector3 size = new Vector3(0.13f, 0.23f, 0.13f);
                parts.Add(CreatureRecipeAuthoring.Part("Bud" + i, Primitive.Sphere, position, size, CreatureRecipeAuthoring.bud, euler, glow: 1.6f, role: PartRole.Tip));
            }

            return parts;
        }

        public static List<CreaturePart> Rosette()
        {
            List<CreaturePart> parts = new List<CreaturePart>
            {
                CreatureRecipeAuthoring.Part("Rosette", Primitive.Sphere, Vector3.up * 0.15f, new Vector3(0.4f, 0.3f, 0.4f), CreatureRecipeAuthoring.body,
                    parent: -1)
            };
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector3 position = new Vector3(0.26f * Mathf.Cos(angle), 0.28f, 0.26f * Mathf.Sin(angle));
                Vector3 facing = new Vector3(Mathf.Cos(angle), 0.65f, Mathf.Sin(angle));
                Vector3 euler = Quaternion.FromToRotation(Vector3.up, facing).eulerAngles;
                Vector3 size = new Vector3(0.2f, 0.72f, 0.1f);
                parts.Add(CreatureRecipeAuthoring.Part("Bud" + i, Primitive.Leaf, position, size, CreatureRecipeAuthoring.body, euler, glow: 0.7f));
            }

            return parts;
        }

        public static List<CreaturePart> Fern()
        {
            List<CreaturePart> parts = Base();
            Vector3 previous = Vector3.zero;
            for (int i = 0; i < 10; i++)
            {
                float angle = i * 0.53f;
                float radius = 0.31f * (1f - i * 0.055f);
                float height = 0.13f + i * 0.075f;
                Vector3 point = new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius * 0.35f);
                Vector3 delta = point - previous;
                Vector3 stemSize = new Vector3(0.065f, delta.magnitude + 0.035f, 0.065f);
                Vector3 stemEuler = Quaternion.FromToRotation(Vector3.up, delta).eulerAngles;
                Vector3 middle = (point + previous) * 0.5f;
                parts.Add(CreatureRecipeAuthoring.Part("FernStem" + i, Primitive.Capsule, middle, stemSize, CreatureRecipeAuthoring.stem, stemEuler));
                Vector3 frondSize = new Vector3(0.28f * (1f - i * 0.04f), 0.07f, 0.15f);
                Vector3 frondEuler = new Vector3(0f, i * 29f, i % 2 == 0 ? 32f : -32f);
                Color frondColour = i % 2 == 0 ? CreatureRecipeAuthoring.body : CreatureRecipeAuthoring.bud;
                parts.Add(CreatureRecipeAuthoring.Part("Frond" + i, Primitive.Sphere, point, frondSize, frondColour, frondEuler,
                    glow: i > 6 ? 0.45f : 0f));
                previous = point;
            }

            return parts;
        }

        public static List<CreaturePart> Arch()
        {
            List<CreaturePart> parts = Base();
            parts[0] = CreatureRecipeAuthoring.Part("ArchFoot", Primitive.Capsule, new Vector3(-0.29f, 0.2f, 0f),
                new Vector3(0.10f, 0.4f, 0.1f), CreatureRecipeAuthoring.stem, parent: -1);
            Vector3 previous = new Vector3(0f, 0.12f, 0f);
            for (int i = 1; i <= 8; i++)
            {
                float angle = Mathf.PI - i * Mathf.PI / 8f;
                Vector3 point = new Vector3(0.29f + Mathf.Cos(angle) * 0.29f, 0.12f + Mathf.Sin(angle) * 0.75f, 0f);
                Vector3 delta = point - previous;
                Vector3 size = new Vector3(0.09f, delta.magnitude + 0.04f, 0.09f);
                Vector3 euler = Quaternion.FromToRotation(Vector3.up, delta).eulerAngles;
                parts.Add(CreatureRecipeAuthoring.Part("Arch" + i, Primitive.Capsule, (point + previous) * 0.5f, size, CreatureRecipeAuthoring.body, euler));
                previous = point;
            }

            for (int i = 0; i < 3; i++)
            {
                float x = 0.13f + i * 0.16f;
                float y = i == 1 ? 0.58f : 0.41f;
                parts.Add(CreatureRecipeAuthoring.Part("PodStem" + i, Primitive.CylinderSegment, new Vector3(x, y + 0.12f, 0f),
                    new Vector3(0.028f, 0.25f, 0.028f), CreatureRecipeAuthoring.stem));
                parts.Add(CreatureRecipeAuthoring.Part("Pod" + i, Primitive.Sphere, new Vector3(x, y - 0.06f, 0f),
                    new Vector3(0.16f, 0.28f, 0.18f), CreatureRecipeAuthoring.bud, glow: 0.6f));
            }

            return parts;
        }

        public static List<CreaturePart> Stack()
        {
            List<CreaturePart> parts = new List<CreaturePart>
            {
                CreatureRecipeAuthoring.Part("ConicalRoot", Primitive.Cone, new Vector3(0f, 0.2f, 0f), new Vector3(0.30f, 0.4f, 0.30f),
                    CreatureRecipeAuthoring.stem, parent: -1)
            };
            parts.Add(CreatureRecipeAuthoring.Part("BottomSphere", Primitive.Sphere, new Vector3(0f, 0.34f, 0f), Vector3.one * 0.43f,
                CreatureRecipeAuthoring.body));
            parts.Add(CreatureRecipeAuthoring.Part("MiddleSphere", Primitive.Sphere, new Vector3(0.045f, 0.69f, 0f), Vector3.one * 0.32f,
                CreatureRecipeAuthoring.bud));
            parts.Add(CreatureRecipeAuthoring.Part("TopSphere", Primitive.Sphere, new Vector3(-0.02f, 0.95f, 0f), Vector3.one * 0.22f,
                CreatureRecipeAuthoring.body, glow: 0.7f));
            parts.Add(CreatureRecipeAuthoring.Part("Ring", Primitive.Torus, new Vector3(0f, 0.56f, 0f), new Vector3(0.48f, 0.26f, 0.48f),
                CreatureRecipeAuthoring.stem));
            return parts;
        }
    }
}

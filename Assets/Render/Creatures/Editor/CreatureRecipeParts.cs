using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // The part list of the authored healer recipe
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
    }
}

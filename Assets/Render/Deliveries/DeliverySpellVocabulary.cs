using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // Explicit asset authoring, never a runtime shape switch or a creature-specific exception.
    public static class DeliverySpellVocabulary
    {
        public static void Apply(DeliveryVocabulary vocabulary)
        {
            DeliveryPresentationPresets.Apply(vocabulary);
            vocabulary.bulletSize = 0.38f;
            vocabulary.paths = new Dictionary<DeliveryPathKind, DeliveryPathLook>
            {
                [DeliveryPathKind.Chain] = new DeliveryPathLook { width = 0.095f, deviation = 0.22f,
                    segmentsPerLeg = 11, pulseFrequency = 10f, releaseSeconds = 0.22f, coreWidth = 0.3f },
                [DeliveryPathKind.Beam] = new DeliveryPathLook { width = 0.14f, deviation = 0.025f,
                    segmentsPerLeg = 14, pulseFrequency = 3f, releaseSeconds = 0.25f, coreWidth = 0.4f }
            };
            vocabulary.tips[DeliveryStyle.Direct] = new[]
            {
                Part("Spell seed", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(0.8f, 0.8f, 1.7f))
            };
            vocabulary.tips[DeliveryStyle.Rigid] = new[]
            {
                Part("Spell lance", Primitive.Cone, ShapeProfile.Shard(), new Vector3(0.55f, 2.4f, 0.55f),
                    rotation: new Vector3(90, 0, 0))
            };
            vocabulary.tips[DeliveryStyle.Arc] = new[]
            {
                Part("Pod stem", Primitive.CylinderSegment, ShapeProfile.Segment(0.25f),
                    new Vector3(0.22f, 1.5f, 0.22f), new Vector3(0, 0, -0.45f), new Vector3(90, 0, 0)),
                Part("Arc pod", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(1.35f, 1.2f, 1.3f))
            };
            vocabulary.tips[DeliveryStyle.Swarm] = new[]
            {
                Part("Swarm seed", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(0.65f, 0.65f, 1.5f)),
                Part("Left seed", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(0.4f, 0.4f, 1f),
                    new Vector3(-0.65f, 0.2f, -0.7f)),
                Part("Right seed", Primitive.Sphere, ShapeProfile.Bulb(), new Vector3(0.4f, 0.4f, 1f),
                    new Vector3(0.65f, -0.2f, -0.7f))
            };
            vocabulary.tips[DeliveryStyle.Bounce] = new[]
            {
                Part("Returning ring", Primitive.Sphere, ShapeProfile.Ring(0.2f), new Vector3(1.5f, 0.3f, 1.5f),
                    rotation: new Vector3(90, 0, 0))
            };
            vocabulary.tips[DeliveryStyle.Thrown] = new[]
            {
                Part("Thrown spell shard", Primitive.Cone, ShapeProfile.Shard(), new Vector3(1.2f, 1.8f, 0.8f),
                    rotation: new Vector3(90, 0, 0))
            };
        }

        static LookPart Part(string id, Primitive primitive, ShapeProfile shape, Vector3 size,
            Vector3 position = default, Vector3 rotation = default)
        {
            return new LookPart { id = id, primitive = primitive, shape = shape, size = size,
                position = position, euler = rotation, role = PartRole.Tip, colour = ColourRole.Accent };
        }
    }
}

using System.Collections.Generic;

namespace HealerLike.Render.Deliveries
{
    // Explicit authoring pass; never runs when a shot spawns and never touches gameplay data.
    public static class DeliveryPresentationPresets
    {
        public static void Apply(DeliveryVocabulary vocabulary)
        {
            vocabulary.bulletSize = 0.28f;
            vocabulary.presentation = new Dictionary<DeliveryStyle, DeliveryPresentation>
            {
                [DeliveryStyle.Direct] = Look(1.2f, 0.08f, 2f, 0.16f, 0.32f),
                [DeliveryStyle.Arc] = Look(1.18f, 0.1f, 1.6f, 0.12f, 0.22f),
                [DeliveryStyle.Rigid] = Look(1.15f, 0.035f, 2.5f, 0.09f, 0.2f),
                [DeliveryStyle.Swarm] = Look(1.22f, 0.12f, 2.8f, 0.13f, 0.26f),
                [DeliveryStyle.Bounce] = Look(1.25f, 0.1f, 2.2f, 0.2f, 0.35f),
                [DeliveryStyle.ChainSync] = Look(1.2f, 0.06f, 3f, 0.08f, 0.2f),
                [DeliveryStyle.Thrown] = Look(1f, 0f, 0f, 0f, 0f)
            };
        }

        static DeliveryPresentation Look(float size, float pulse, float frequency, float trail, float width)
        {
            return new DeliveryPresentation { size = size, pulseAmount = pulse, pulseFrequency = frequency,
                trailSeconds = trail, trailWidth = width };
        }
    }
}

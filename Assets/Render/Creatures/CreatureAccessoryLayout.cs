using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Accessory attachment and clearance share one owner, independent of head/body composition.
    public static class CreatureAccessoryLayout
    {
        public static bool Add(
            PartList parts,
            UnitChannels channels,
            LookVocabulary vocabulary,
            UnitSockets sockets,
            int seed
        )
        {
            bool isPlant = channels.side == LookSide.Plant;
            float scale = sockets.scale;
            float headScale = sockets.headScale;
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            Color stemColour = vocabulary.Colour(ColourRole.Stem, channels.accent, channels.side);
            if (channels.accessory != AccessoryKind.None)
            {
                parts.accessoryStart = parts.count;
                LookVocabulary.AccessoryEntry accessory = vocabulary.accessories[channels.accessory];
                Vector3 socket = sockets.At(accessory.socket);
                LookPart[] accessoryParts = isPlant ? accessory.plant : accessory.stone;
                if (
                    !FragmentPlacement.Add(
                        parts,
                        vocabulary,
                        channels,
                        accessoryParts,
                        socket,
                        scale,
                        CountBand.One,
                        seed
                    )
                )
                {
                    return false;
                }

                if (channels.accessory == AccessoryKind.MiniHead)
                {
                    LookVocabulary.HeadEntry mini = vocabulary.heads[channels.accessoryHead];
                    LookPart[] miniParts = isPlant ? mini.plant : mini.stone;
                    Vector3 miniAt = socket + accessory.miniHeadAt * scale;
                    float miniScale = accessory.miniHeadScale * headScale;
                    if (
                        !FragmentPlacement.Add(
                            parts,
                            vocabulary,
                            channels,
                            miniParts,
                            miniAt,
                            miniScale,
                            CountBand.One,
                            seed
                        )
                    )
                    {
                        return false;
                    }
                }

                if (vocabulary.layoutSettings.extendAccessorySupports && !accessory.isCentered)
                {
                    ExtendAccessory(parts, channels, vocabulary, socket, scale, stem, stemColour, seed);
                }
            }

            return true;
        }

        static void ExtendAccessory(
            PartList parts,
            UnitChannels channels,
            LookVocabulary vocabulary,
            Vector3 socket,
            float scale,
            LookVocabulary.StemEntry stem,
            Color colour,
            int seed
        )
        {
            float unit = vocabulary.Unit(channels.side);
            float needed = LookComposer.AccessoryClearance(channels.side, vocabulary);
            if (LookMeasure.OutlineReach(parts, unit) >= needed)
            {
                return;
            }

            float bodyRight = float.MinValue;
            float accessoryRight = float.MinValue;
            for (int i = 0; i < parts.count; i++)
            {
                LookPart part = parts.Source(i);
                Quaternion rotation = Quaternion.Euler(part.euler);
                Vector3 half = part.size * 0.5f;
                Vector3 direction = Quaternion.Inverse(rotation) * Vector3.right;
                if (i < parts.accessoryStart)
                {
                    bodyRight = Mathf.Max(bodyRight, part.position.x + LookMeasure.Extent(half, direction, part.shape));
                }
                else
                {
                    // OutlineReach samples the six face centres, so use the rightmost of those same samples.
                    float face = Mathf.Max(
                        Mathf.Abs(half.x * direction.x),
                        Mathf.Abs(half.y * direction.y),
                        Mathf.Abs(half.z * direction.z)
                    );
                    accessoryRight = Mathf.Max(accessoryRight, part.position.x + face);
                }
            }

            if (!float.IsFinite(bodyRight) || !float.IsFinite(accessoryRight))
            {
                return;
            }

            float shift = Mathf.Max(0f, bodyRight + needed / unit - accessoryRight + 0.001f);
            Vector3 offset = Vector3.right * shift;
            parts.Translate(parts.accessoryStart, offset);
            bool plant = channels.side == LookSide.Plant;
            ShapeProfile profile = plant ? stem.plantShape : stem.stoneLimbShape;
            Vector3 size = new Vector3(
                vocabulary.layoutSettings.branchThickness * scale,
                shift + vocabulary.layoutSettings.branchThickness * scale,
                vocabulary.layoutSettings.branchThickness * scale
            );
            parts.Add(
                "AccessorySupport",
                plant ? Primitive.Capsule : Primitive.Stone,
                socket + offset * 0.5f,
                size,
                colour,
                new Vector3(0f, 0f, -90f),
                0f,
                PartRole.Accessory,
                LookComposer.Variant(seed, parts.count),
                profile
            );
        }
    }
}

using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Assemble the selected anatomy in body units; LookComposer owns the resulting recipe.
    public static class CreatureLayout
    {
        public static PartList Build(
            UnitChannels channels,
            LookVocabulary vocabulary,
            int copies,
            int seed,
            out UnitSockets sockets
        )
        {
            PartList parts = new PartList(vocabulary.Unit(channels.side));
            sockets = UnitSockets.Place(channels, vocabulary);
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            LookVocabulary.HeadEntry head = vocabulary.heads[channels.head];
            bool isPlant = channels.side == LookSide.Plant;
            float scale = sockets.scale;
            float headScale = sockets.headScale;
            Color stemColour = vocabulary.Colour(ColourRole.Stem, channels.accent, channels.side);
            if (isPlant)
            {
                if (
                    !FragmentPlacement.Add(
                        parts,
                        vocabulary,
                        channels,
                        body.plant,
                        sockets.body,
                        1f,
                        CountBand.One,
                        seed
                    )
                )
                {
                    return null;
                }

                if (head.plantStem == null)
                {
                    parts.Link(
                        "Stem",
                        sockets.stemFoot,
                        sockets.neck,
                        stem.thickness,
                        stemColour,
                        PartRole.Stem,
                        stem.plantShape
                    );
                }
                else
                {
                    PlantStem(
                        parts,
                        sockets,
                        stem,
                        head.plantStem,
                        vocabulary.Colour(ColourRole.Body, channels.accent, channels.side)
                    );
                }
            }
            else
            {
                if (
                    !FragmentPlacement.Add(
                        parts,
                        vocabulary,
                        channels,
                        body.stone,
                        sockets.body,
                        vocabulary.stoneScale,
                        CountBand.One,
                        seed
                    )
                )
                {
                    return null;
                }

                Limbs(
                    parts,
                    stem.limbLength * sockets.stemScale,
                    sockets.bodyRadius,
                    scale,
                    stemColour,
                    seed,
                    vocabulary.layoutSettings,
                    stem.stoneLimbShape
                );
            }

            LookPart[] headParts = isPlant ? head.plant : head.stone;
            if (head.carriesCount)
            {
                parts.headStarts.Add(parts.count);
                if (
                    !FragmentPlacement.Add(
                        parts,
                        vocabulary,
                        channels,
                        headParts,
                        sockets.neck,
                        headScale,
                        channels.count,
                        seed
                    )
                )
                {
                    return null;
                }
            }
            else if (copies == 1)
            {
                parts.headStarts.Add(parts.count);
                if (
                    !FragmentPlacement.Add(
                        parts,
                        vocabulary,
                        channels,
                        headParts,
                        sockets.neck,
                        headScale,
                        CountBand.One,
                        seed
                    )
                )
                {
                    return null;
                }
            }
            else
            {
                // Three or five smaller heads on a branching neck, spread so two neighbours never touch on screen;
                // a stone carries them side by side
                if (
                    !FragmentPlacement.TryResolve(
                        headParts,
                        CountBand.One,
                        seed,
                        parts.count + 1,
                        out LookPart[] measuredHead,
                        out string attachmentError
                    )
                )
                {
                    Debug.LogError("[LookComposer] " + attachmentError);
                    return null;
                }

                HeadFan fan = HeadFan.Shape(
                    measuredHead,
                    copies,
                    isPlant,
                    vocabulary.layoutSettings,
                    isPlant ? stem.plantShape : stem.stoneLimbShape
                );
                for (int i = 0; i < copies; i++)
                {
                    parts.headStarts.Add(parts.count);
                    Vector3 end = fan.Branch(parts, sockets.neck, i, headScale, stemColour);
                    if (
                        !FragmentPlacement.Add(
                            parts,
                            vocabulary,
                            channels,
                            headParts,
                            end,
                            fan.copyScale * headScale,
                            CountBand.One,
                            seed
                        )
                    )
                    {
                        return null;
                    }
                }
            }

            if (!CreatureAccessoryLayout.Add(parts, channels, vocabulary, sockets, seed))
            {
                return null;
            }

            return parts;
        }

        static void PlantStem(
            PartList parts,
            UnitSockets sockets,
            LookVocabulary.StemEntry cadence,
            LookVocabulary.PlantStemEntry growth,
            Color colour
        )
        {
            float width = cadence.thickness * growth.thicknessScale;
            Vector3 start = sockets.stemFoot;
            for (int i = 0; i < growth.segments; i++)
            {
                float t = (i + 1f) / growth.segments;
                Vector3 end =
                    Vector3.Lerp(sockets.stemFoot, sockets.neck, t)
                    + Vector3.right * (growth.bow * Mathf.Sin(Mathf.PI * t));
                parts.Link("StemGrowth", start, end, width, colour, PartRole.Stem, growth.segmentShape);
                if (i + 1 < growth.segments)
                {
                    parts.Add(
                        "StemJoint",
                        Primitive.Sphere,
                        end,
                        Vector3.one * (width * growth.jointScale),
                        colour,
                        Vector3.zero,
                        0f,
                        PartRole.Stem,
                        shape: growth.jointShape
                    );
                }

                start = end;
            }
        }

        // Keep the whole accessory readable beyond a broad crown, with an attached support back to its socket.
        // A fragment's parts around a socket, those its count band allows; a stone part takes a variant from the seed
        // Exactly two mineral legs; the cadence band supplies their profile and length.
        static void Limbs(
            PartList parts,
            float limb,
            float bodyRadius,
            float scale,
            Color colour,
            int seed,
            LookVocabulary.LayoutEntry layout,
            ShapeProfile shape
        )
        {
            float height = limb + bodyRadius * layout.limbBodyOverlap;
            for (int i = -1; i <= 1; i += 2)
            {
                float proportion = 1f + i * layout.limbAsymmetry;
                Vector3 foot = new Vector3(layout.limbSpread * i * scale, height * proportion * 0.5f, layout.limbDepth);
                Vector3 size = new Vector3(layout.limbWidth * scale, height, layout.limbThickness * scale) * proportion;
                parts.Add(
                    "Limb",
                    Primitive.Stone,
                    foot,
                    size,
                    colour,
                    new Vector3(0f, layout.limbSplay * i, 0f),
                    0f,
                    PartRole.Limb,
                    LookComposer.Variant(seed, parts.count),
                    shape
                );
            }
        }
    }
}

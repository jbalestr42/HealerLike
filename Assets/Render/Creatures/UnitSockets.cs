using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // Where the composer puts a unit's body, in its side's body units from the foot, and where its head, stem,
    // accessory and effects attach
    public struct UnitSockets
    {
        float _shoulderOffset;

        public Vector3 foot;
        public Vector3 body;
        public float bodyRadius;
        public Vector3 hip;
        public Vector3 neck;
        // Where a plant's stem leaves its body
        public Vector3 stemFoot;
        // Mass scale, times the stone scale on stones
        public float scale;

        public static UnitSockets Place(UnitChannels channels, LookVocabulary vocabulary)
        {
            LookVocabulary.BodyEntry body = vocabulary.bodies[channels.mass];
            LookVocabulary.StemEntry stem = vocabulary.stems[channels.stem];
            bool isPlant = channels.side == LookSide.Plant;
            LookPart[] bodyParts = isPlant ? body.plant : body.stone;
            float stoneScale = isPlant ? 1f : vocabulary.stoneScale;
            LookVocabulary.LayoutEntry layout = vocabulary.Layout;
            UnitSockets sockets = new UnitSockets { _shoulderOffset = layout.shoulderOffset };
            sockets.scale = body.scale * stoneScale;
            sockets.bodyRadius = bodyParts[0].size.x * 0.5f * stoneScale;
            if (isPlant)
            {
                sockets.body = Vector3.up * (layout.plantBodySink * sockets.bodyRadius + body.bodyLift);
                sockets.neck = sockets.body + Vector3.up * (sockets.bodyRadius * layout.plantStemFoot + stem.length);
                sockets.stemFoot = sockets.body + Vector3.up * (sockets.bodyRadius * layout.plantStemFoot);
            }
            else
            {
                // Stones stand on boulder limbs, the stem band is the limb length
                sockets.body = Vector3.up * (stem.limbLength * sockets.scale + sockets.bodyRadius * layout.stoneBodyLift
                    + body.bodyLift * stoneScale);
                sockets.neck = sockets.body + Vector3.up * (sockets.bodyRadius * layout.stoneNeck);
            }

            sockets.hip = sockets.body;
            return sockets;
        }

        // Where an accessory socket sits, every one on the unit's right so the accessory stands on lit grass
        public Vector3 At(AccessorySocket socket)
        {
            switch (socket)
            {
                case AccessorySocket.NeckOrbit:
                case AccessorySocket.Crook:
                    return neck;
                case AccessorySocket.Shoulder:
                    return body + new Vector3(bodyRadius * _shoulderOffset, bodyRadius * _shoulderOffset, 0f);
                case AccessorySocket.Flank:
                    return hip + Vector3.right * bodyRadius;
                default:
                    return hip;
            }
        }
    }
}

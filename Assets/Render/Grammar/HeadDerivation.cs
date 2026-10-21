using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Grammar
{
    // The silhouette half of a unit's reading: the head, the accessory and the accent its skills draw.
    // LookDerivation reads the rest and calls this for those three.
    public static class HeadDerivation
    {
        public static HeadKind Head(ASkillFactory skill)
        {
            return SkillDescriptionReader.Read(skill, null).head;
        }

        // One slot: a second skill or delivery, then a baked behaviour, then the first passive, then an on-hit effect
        public static AccessoryKind Accessory(EntityData data)
        {
            ASkillFactory primary = LookDerivation.Primary(data);
            SkillDescription description = SkillDescriptionReader.Read(primary, data);
            return Accessory(data, description.head, description.accent, out HeadKind accessoryHead);
        }

        public static HeadKind AccessoryHead(EntityData data)
        {
            TryMiniHead(data, PrimaryHead(LookDerivation.Primary(data)), out HeadKind head);
            return head;
        }

        // The family the primary delivers, drawn on the tips and the projectile
        public static EffectFamily Accent(ASkillFactory skill)
        {
            return SkillDescriptionReader.Read(skill, null).accent;
        }

        // The accessory and its small head, read once from the head and the accent already derived
        public static AccessoryKind Accessory(EntityData data, HeadKind head, EffectFamily accent,
            out HeadKind accessoryHead)
        {
            if (TryMiniHead(data, head, out accessoryHead))
            {
                return AccessoryKind.MiniHead;
            }

            return FallbackAccessory(data, accent);
        }

        // The non-head accessory, also exposed to the atlas when a speed edit removes a mini head.
        public static AccessoryKind FallbackAccessory(EntityData data, EffectFamily accent)
        {

            if (data == null)
            {
                return AccessoryKind.None;
            }

            foreach (GameObject prefab in SkillWalker.Prefabs(LookDerivation.Primary(data)))
            {
                if (prefab.GetComponent<BackstabProjectileBehaviour>() != null)
                {
                    return AccessoryKind.Hook;
                }

                if (prefab.GetComponent<IncreaseDamageOnDistanceProjectileBehaviour>() != null)
                {
                    return AccessoryKind.Antenna;
                }
            }

            List<ABuffHandlerFactory> passives = ItemWalker.Buffs(data);
            if (passives.Count > 0)
            {
                AccessoryKind passive = PassiveAccessory(passives[0]);
                if (passive != AccessoryKind.None)
                {
                    return passive;
                }
            }

            foreach (ABuffHandlerFactory handler in ItemWalker.OnHitEffects(data))
            {
                EffectFamily family = EffectDerivation.Family(handler, false);
                if (family == accent)
                {
                    continue;
                }

                if (family == EffectFamily.Rot)
                {
                    return AccessoryKind.DripBeads;
                }

                if (family == EffectFamily.Bane)
                {
                    return AccessoryKind.ShardBarbs;
                }

                if (family == EffectFamily.Boon)
                {
                    return AccessoryKind.SmallTorus;
                }
            }
            return AccessoryKind.None;
        }

        // The head of the primary skill, Bud for a unit without one
        static HeadKind PrimaryHead(ASkillFactory primary)
        {
            if (primary == null)
            {
                return HeadKind.Bud;
            }
            return Head(primary);
        }

        // A second skill, else a second delivery inside the primary, drawn as a small head
        static bool TryMiniHead(EntityData data, HeadKind main, out HeadKind head)
        {
            head = HeadKind.Bud;
            ASkillFactory secondary = LookDerivation.Secondary(data);
            if (secondary != null)
            {
                head = Head(secondary);
                return true;
            }

            ASkillFactory primary = LookDerivation.Primary(data);
            if (primary == null)
            {
                return false;
            }

            foreach (GameObject prefab in SkillWalker.Prefabs(primary))
            {
                HeadKind other = LookDerivation.DeliveryHead(prefab);
                if (other != main)
                {
                    head = other;
                    return true;
                }
            }
            return false;
        }

        static AccessoryKind PassiveAccessory(ABuffHandlerFactory handler)
        {
            IReadOnlyList<ABuffFactory> buffs = EffectDerivation.Buffs(handler);
            if (buffs.Count == 0)
            {
                return AccessoryKind.None;
            }

            foreach (ABuffFactory buff in buffs)
            {
                if (buff is CurrentWaveModifierFactory wave && wave.data != null)
                {
                    return AccessoryKind.TierRings;
                }
            }

            EffectFamily family = EffectDerivation.Family(handler, true);
            if (family == EffectFamily.Renew)
            {
                return AccessoryKind.StalkBeads;
            }

            foreach (ABuffFactory buff in buffs)
            {
                if (EffectDerivation.TryModifier(buff, out AttributeType type, out float delta))
                {
                    if (family == EffectFamily.Bane)
                    {
                        return AccessoryKind.ConeCrown;
                    }
                    return AccessoryKind.SmallTorus;
                }
            }
            return AccessoryKind.None;
        }

    }
}

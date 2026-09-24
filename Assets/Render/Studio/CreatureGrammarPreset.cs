using System;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio
{
    // The inputs of the look grammar, kept editable: nine channels set by hand or read from an entity, and the
    // vocabulary they compose against. A composed recipe is a separate bake the caller owns.
    [CreateAssetMenu(menuName = "Custom/Data/Render/Creature Grammar Preset", fileName = "CreatureGrammar")]
    public class CreatureGrammarPreset : ScriptableObject
    {
        public string displayName = "Untitled grammar";
        [TextArea]
        public string description;
        public LookVocabulary vocabulary;
        public LookSide side = LookSide.Plant;
        public HeadKind head = HeadKind.Bud;
        public CountBand count = CountBand.One;
        public StemBand stem = StemBand.Steady;
        public MassBand mass = MassBand.Light;
        public ReachBand reach = ReachBand.Mid;
        public AccessoryKind accessory = AccessoryKind.None;
        public HeadKind accessoryHead = HeadKind.Bud;
        public EffectFamily accent = EffectFamily.Heal;
        public bool deriveFromEntity;
        public EntityData sourceEntity;
        public Entity.EntityType sourceSide = Entity.EntityType.Player;

        // The entity's channels while deriving from it, else the channels set by hand
        public UnitChannels Channels()
        {
            UnitChannels channels;
            string error;
            if (TryChannels(out channels, out error))
            {
                return channels;
            }
            return ManualChannels();
        }

        public bool TryChannels(out UnitChannels channels, out string error)
        {
            channels = ManualChannels();
            error = null;
            if (!deriveFromEntity)
            {
                return true;
            }

            if (sourceEntity == null)
            {
                error = "Choose an EntityData source or switch to manual channels.";
                return false;
            }

            if (LookDerivation.Primary(sourceEntity) == null)
            {
                error = "The source entity needs a primary skill for production head derivation.";
                return false;
            }

            if (!Enum.IsDefined(typeof(Entity.EntityType), sourceSide))
            {
                error = "Choose a valid entity side.";
                return false;
            }

            channels = LookDerivation.Channels(sourceEntity, sourceSide);
            return true;
        }

        // Copies the entity's channels into the manual fields and stops deriving from it
        public bool ReadFromEntity()
        {
            if (sourceEntity == null || LookDerivation.Primary(sourceEntity) == null)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(Entity.EntityType), sourceSide))
            {
                return false;
            }

            UnitChannels channels = LookDerivation.Channels(sourceEntity, sourceSide);

            side = channels.side;
            head = channels.head;
            count = channels.count;
            stem = channels.stem;
            mass = channels.mass;
            reach = channels.reach;
            accessory = channels.accessory;
            accessoryHead = channels.accessoryHead;
            accent = channels.accent;

            deriveFromEntity = false;
            return true;
        }

        // Null while the validator reports a problem, so the composer never reads a broken vocabulary
        public CreatureRecipe Compose()
        {
            if (CreatureGrammarValidator.Validate(this).Length != 0)
            {
                return null;
            }

            CreatureRecipe result = LookComposer.Compose(Channels(), vocabulary);
            if (result != null)
            {
                result.name = displayName;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    result.name = name;
                }
                result.hideFlags = HideFlags.None;
            }
            return result;
        }

        UnitChannels ManualChannels()
        {
            UnitChannels channels = new UnitChannels();

            channels.side = side;
            channels.head = head;
            channels.count = count;
            channels.stem = stem;
            channels.mass = mass;
            channels.reach = reach;
            channels.accessory = accessory;
            channels.accessoryHead = accessoryHead;
            channels.accent = accent;
            return channels;
        }
    }
}

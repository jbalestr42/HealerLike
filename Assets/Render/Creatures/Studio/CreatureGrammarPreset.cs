using System;
using System.Collections.Generic;
using HealerLike.Render.Grammar;
using UnityEngine;

namespace HealerLike.Render.Creatures.Studio
{
    /// <summary>Persistent inputs to the production look grammar. A composed recipe is an independent, caller-owned bake.</summary>
    [CreateAssetMenu(menuName = "Custom/Data/Render/Creature Grammar Preset", fileName = "CreatureGrammar")]
    public sealed class CreatureGrammarPreset : ScriptableObject
    {
        public string displayName = "Untitled grammar";
        [TextArea] public string description;
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

        public UnitChannels Channels()
        {
            return TryChannels(out UnitChannels channels, out _) ? channels : ManualChannels();
        }

        /// <summary>Copies the current entity-derived channels into editable fields and detaches the live derivation.</summary>
        public bool ReadFromEntity()
        {
            if (sourceEntity == null || LookDerivation.Primary(sourceEntity) == null ||
                !Enum.IsDefined(typeof(Entity.EntityType), sourceSide)) return false;
            UnitChannels channels;
            try { channels = LookDerivation.Channels(sourceEntity, sourceSide); }
            catch (Exception) { return false; }
            side = channels.side; head = channels.head; count = channels.count; stem = channels.stem;
            mass = channels.mass; reach = channels.reach; accessory = channels.accessory;
            accessoryHead = channels.accessoryHead; accent = channels.accent;
            deriveFromEntity = false;
            return true;
        }

        public CreatureRecipe Compose()
        {
            if (Validate().Length != 0) return null;
            CreatureRecipe result = LookComposer.Compose(Channels(), vocabulary);
            if (result != null)
            {
                result.name = string.IsNullOrWhiteSpace(displayName) ? name : displayName;
                result.hideFlags = HideFlags.None;
            }
            return result;
        }

        /// <summary>Blocking prerequisites. Informational production constraints are returned separately by Diagnostics.</summary>
        public string[] Validate()
        {
            var errors = new List<string>();
            if (!TryChannels(out UnitChannels c, out string channelError)) errors.Add(channelError);
            if (!Defined(c.side) || !Defined(c.head) || !Defined(c.count) || !Defined(c.stem) || !Defined(c.mass) ||
                !Defined(c.reach) || !Defined(c.accessory) || !Defined(c.accessoryHead) || !Defined(c.accent))
                errors.Add("Choose valid grammar channel values.");
            if (vocabulary == null) { errors.Add("Choose a look vocabulary."); return errors.ToArray(); }
            if (vocabulary.palette == null) errors.Add("The vocabulary needs a palette.");
            if (vocabulary.bodies == null || !vocabulary.bodies.TryGetValue(c.mass, out var body) || body == null)
                errors.Add("The vocabulary is missing the selected mass/body entry.");
            if (vocabulary.heads == null || !vocabulary.heads.TryGetValue(c.head, out var headEntry) || headEntry == null)
                errors.Add("The vocabulary is missing the selected head entry.");
            if (vocabulary.stems == null || !vocabulary.stems.TryGetValue(c.stem, out var stemEntry) || stemEntry == null)
                errors.Add("The vocabulary is missing the selected stem entry.");
            if (c.side == LookSide.Plant && vocabulary.roots == null) errors.Add("The vocabulary root table is missing.");
            if (c.accessory != AccessoryKind.None && (vocabulary.accessories == null ||
                !vocabulary.accessories.TryGetValue(c.accessory, out var accessoryEntry) || accessoryEntry == null))
                errors.Add("The vocabulary is missing the selected accessory entry.");
            if (c.accessory == AccessoryKind.MiniHead && (vocabulary.heads == null ||
                !vocabulary.heads.TryGetValue(c.accessoryHead, out var mini) || mini == null))
                errors.Add("The vocabulary is missing the selected miniature head entry.");
            if (!Positive(vocabulary.bodyUnit) || !Positive(c.side == LookSide.Plant ? vocabulary.plantScale : vocabulary.stoneScale))
                errors.Add("Body unit and the selected side scale must be finite and positive.");
            if (vocabulary.maxParts < 1 || vocabulary.maxParts > CreatureValidator.MaxParts)
                errors.Add("Vocabulary maxParts must be between 1 and " + CreatureValidator.MaxParts + ".");
            if (errors.Count != 0) return errors.ToArray();

            bool plant = c.side == LookSide.Plant;
            var b = vocabulary.bodies[c.mass];
            var h = vocabulary.heads[c.head];
            var s = vocabulary.stems[c.stem];
            CheckParts(plant ? b.plant : b.stone, "body", errors);
            CheckParts(plant ? h.plant : h.stone, "head", errors);
            if (!Positive(b.scale) || (plant ? !Positive(s.length) || !Positive(s.thickness) : !Positive(s.limbLength)))
                errors.Add("Body scale and stem dimensions must be finite and positive.");
            if (c.accessory != AccessoryKind.None)
            {
                var a = vocabulary.accessories[c.accessory];
                CheckParts(plant ? a.plant : a.stone, "accessory", errors);
                if (!Defined(a.socket) || (c.accessory == AccessoryKind.MiniHead && (!Finite(a.miniHeadAt) || !Positive(a.miniHeadScale))))
                    errors.Add("Accessory socket and miniature head settings are invalid.");
                if (c.accessory == AccessoryKind.MiniHead)
                {
                    var m = vocabulary.heads[c.accessoryHead];
                    CheckParts(plant ? m.plant : m.stone, "miniature head", errors);
                }
            }
            if (plant)
            {
                bool usesPinnedReach = vocabulary.isReachPinned || !vocabulary.roots.ContainsKey(c.reach);
                if (usesPinnedReach && !Positive(vocabulary.pinnedReach)) errors.Add("Pinned reach must be finite and positive.");
                if (!usesPinnedReach && (vocabulary.roots[c.reach] == null || !Positive(vocabulary.roots[c.reach].reach)))
                    errors.Add("The selected root entry requires finite positive reach.");
                // Mid reach is also consumed by LookComposer.Roots to choose the root segment count.
                if (vocabulary.roots.TryGetValue(ReachBand.Mid, out var mid) && (mid == null || !Positive(mid.reach)))
                    errors.Add("The middle root entry requires finite positive reach.");
                float unit = vocabulary.Unit(c.side);
                if (vocabulary.rootCount < 4 || vocabulary.rootCount > 14 || vocabulary.armCount < 0 ||
                    vocabulary.armCount > CreatureRig.MaxArms || !Positive(vocabulary.rootHip) ||
                    !Positive(vocabulary.rootKnee) || !Positive(vocabulary.rootThickness))
                    errors.Add("Plant roots or arm count are outside the renderer's supported ranges.");
                if (errors.Count == 0 && (vocabulary.Reach(c.reach) + vocabulary.rootThickness * .5f) * unit > CreatureValidator.MaxRootReach)
                    errors.Add("The selected root reach and thickness exceed the renderer's maximum root extent.");
            }
            foreach (ColourRole role in Enum.GetValues(typeof(ColourRole)))
                if (!Finite(vocabulary.Colour(role, c.accent, c.side))) { errors.Add("Palette colours must be finite."); break; }
            if (errors.Count != 0) return errors.ToArray();
            if (PartCount(c, 1) > CreatureValidator.MaxParts)
                errors.Add("Even one head copy exceeds the runtime limit of " + CreatureValidator.MaxParts + " parts.");
            if (c.accessory != AccessoryKind.None)
            {
                float minimum = plant ? LookComposer.PlantAccessoryReach : LookComposer.StoneAccessoryReach;
                // Compose reduces fanned head copies to fit maxParts before measuring the accessory silhouette.
                UnitChannels finalChannels = c;
                if (!h.carriesCount)
                {
                    int copies = LookComposer.Copies(c.count);
                    while (PartCount(c, copies) > vocabulary.maxParts && copies > 1) copies = copies > 3 ? 3 : 1;
                    finalChannels.count = copies == 1 ? CountBand.One : copies == 3 ? CountBand.Few : CountBand.Many;
                }
                if (LookComposer.AccessoryReach(finalChannels, vocabulary) < minimum)
                    errors.Add("The accessory does not extend far enough beyond the body/head silhouette for the production grammar.");
            }
            return errors.ToArray();
        }

        public string[] Diagnostics()
        {
            var notes = new List<string>();
            if (deriveFromEntity) notes.Add("Channels are read from the entity's actual skill, item and attribute data; bake or read into manual fields to detach.");
            if (vocabulary != null)
            {
                notes.Add("Production maxParts: " + vocabulary.maxParts + ". The composer reduces multiple head copies if they exceed this budget.");
                if (vocabulary.isReachPinned)
                    notes.Add("Reach is pinned by this vocabulary to " + vocabulary.pinnedReach.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " body units; changing Reach does not change root extent.");
                if (Channels().side == LookSide.Stone) notes.Add("Stone recipes stand on limbs; they have no plant roots or liana arms.");
            }
            return notes.ToArray();
        }

        UnitChannels ManualChannels() => new UnitChannels { side = side, head = head, count = count, stem = stem,
            mass = mass, reach = reach, accessory = accessory, accessoryHead = accessoryHead, accent = accent };
        bool TryChannels(out UnitChannels channels, out string error)
        {
            channels = ManualChannels(); error = null;
            if (!deriveFromEntity) return true;
            if (sourceEntity == null) { error = "Choose an EntityData source or switch to manual channels."; return false; }
            if (LookDerivation.Primary(sourceEntity) == null) { error = "The source entity needs a primary skill for production head derivation."; return false; }
            if (!Defined(sourceSide)) { error = "Choose a valid entity side."; return false; }
            try { channels = LookDerivation.Channels(sourceEntity, sourceSide); return true; }
            catch (Exception exception) { error = "Unable to derive channels from this entity: " + exception.Message; return false; }
        }
        int PartCount(UnitChannels c, int copies)
        {
            bool plant = c.side == LookSide.Plant;
            var body = vocabulary.bodies[c.mass]; var headEntry = vocabulary.heads[c.head];
            int value = Visible(plant ? body.plant : body.stone, CountBand.One) + (plant ? 1 : 2);
            var parts = plant ? headEntry.plant : headEntry.stone;
            value += headEntry.carriesCount ? Visible(parts, c.count) : Visible(parts, CountBand.One) * copies + (plant && copies > 1 ? copies : 0);
            if (c.accessory != AccessoryKind.None)
            {
                var a = vocabulary.accessories[c.accessory]; value += Visible(plant ? a.plant : a.stone, CountBand.One);
                if (c.accessory == AccessoryKind.MiniHead)
                { var m = vocabulary.heads[c.accessoryHead]; value += Visible(plant ? m.plant : m.stone, CountBand.One); }
            }
            return value;
        }
        static int Visible(LookPart[] parts, CountBand band) { int result = 0; foreach (var p in parts) if (p.minCount <= band) result++; return result; }
        static void CheckParts(LookPart[] parts, string label, List<string> errors)
        {
            if (parts == null || parts.Length == 0) { errors.Add("The selected " + label + " fragment has no parts."); return; }
            if (parts.Length > 256) { errors.Add("The selected " + label + " fragment exceeds 256 source parts."); return; }
            foreach (var p in parts)
                if (string.IsNullOrEmpty(p.id) || !Defined(p.primitive) || !Defined(p.role) || !Defined(p.colour) ||
                    !Defined(p.minCount) || !Finite(p.position) || !Finite(p.euler) || !Positive(p.size.x) ||
                    !Positive(p.size.y) || !Positive(p.size.z) || !Finite(p.glow) || p.glow < 0f)
                { errors.Add("The selected " + label + " fragment contains invalid part data."); break; }
        }
        static bool Defined<T>(T value) where T : struct => Enum.IsDefined(typeof(T), value);
        static bool Positive(float value) => float.IsFinite(value) && value > 0f;
        static bool Finite(float value) => float.IsFinite(value);
        static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        static bool Finite(Color v) => Finite(v.r) && Finite(v.g) && Finite(v.b) && Finite(v.a);
    }
}

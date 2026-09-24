using System;
using System.Collections.Generic;
using HealerLike.Render.Grammar;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures.Editor.Studio
{
    /// <summary>Edits recipe structure while retaining earlier-parent ordering. Callers own undo and dirty state.</summary>
    public static class CreatureStudioAuthoring
    {
        public static readonly string[] SampleNames = { "Healer", "Sprout", "Stone Sentinel" };
        const string DataFolder = "Assets/Render/Creatures/Data/";

        public static CreatureRecipe BuildSample(int index)
        {
            if (index < 0 || index >= SampleNames.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (index == 0) return Clone(AssetDatabase.LoadAssetAtPath<CreatureRecipe>(DataFolder + "Healer.asset"));
            LookVocabulary vocabulary = AssetDatabase.LoadAssetAtPath<LookVocabulary>(DataFolder + "LookVocabulary.asset");
            if (vocabulary == null) return null;
            bool stone = index == 2;
            CreatureRecipe result = LookComposer.Compose(new UnitChannels
            {
                side = stone ? LookSide.Stone : LookSide.Plant,
                head = stone ? HeadKind.Ward : HeadKind.Bud, count = CountBand.One,
                stem = stone ? StemBand.Steady : StemBand.Quick,
                mass = stone ? MassBand.Heavy : MassBand.Light,
                reach = stone ? ReachBand.Mid : ReachBand.Short,
                accessory = AccessoryKind.None, accent = stone ? EffectFamily.Boon : EffectFamily.Heal
            }, vocabulary);
            if (result != null) { result.name = SampleNames[index]; result.hideFlags = HideFlags.None; }
            return result;
        }

        public static CreatureRecipe Clone(CreatureRecipe source)
        {
            if (source == null) return null;
            CreatureRecipe result = ScriptableObject.CreateInstance<CreatureRecipe>();
            result.name = source.name;
            result.parts = source.parts == null ? Array.Empty<CreaturePart>() : (CreaturePart[])source.parts.Clone();
            result.arms = source.arms == null ? Array.Empty<ArmDefinition>() : (ArmDefinition[])source.arms.Clone();
            for (int i = 0; i < result.arms.Length; i++)
                result.arms[i].restJoints = source.arms[i].restJoints == null
                    ? Array.Empty<Vector3>() : (Vector3[])source.arms[i].restJoints.Clone();
            result.sourceLocal = source.sourceLocal == null ? Array.Empty<Vector3>() : (Vector3[])source.sourceLocal.Clone();
            result.roots = source.roots; result.idle = source.idle; result.neckLocal = source.neckLocal;
            result.wiltColour = source.wiltColour; result.stoneOchre = source.stoneOchre;
            return result;
        }

        public static int AddPart(CreatureRecipe recipe, int parent = 0, Primitive primitive = Primitive.Sphere)
        {
            if (recipe == null || !Enum.IsDefined(typeof(Primitive), primitive)) return -1;
            int length = recipe.parts?.Length ?? 0;
            if (length >= CreatureValidator.MaxParts || (length > 0 &&
                (!CreatureValidator.TryValidate(recipe, out _) || parent < 0 || parent >= length))) return -1;
            CreaturePart part = new CreaturePart
            {
                id = UniqueId(recipe, primitive.ToString()), parent = length == 0 ? -1 : parent,
                primitive = primitive, dimensions = Vector3.one * 0.15f, colour = length == 0 ? Color.white : recipe.parts[parent].colour,
                role = length == 0 ? PartRole.Body : PartRole.Accessory, localPosition = Vector3.up * 0.2f
            };
            Array.Resize(ref recipe.parts, length + 1);
            recipe.parts[length] = part;
            return length;
        }

        /// <summary>Duplicates one part, appending after all potential parents. Children and arms are not duplicated.</summary>
        public static int DuplicatePart(CreatureRecipe recipe, int index)
        {
            if (!CanEdit(recipe, index) || recipe.parts.Length >= CreatureValidator.MaxParts) return -1;
            int length = recipe.parts.Length;
            CreaturePart copy = recipe.parts[index];
            copy.id = UniqueId(recipe, copy.id + " Copy");
            if (index == 0)
            {
                copy.parent = 0;
                copy.localPosition = Vector3.zero;
                copy.localEuler = Vector3.zero;
            }
            copy.localPosition += Vector3.right * 0.1f;
            Array.Resize(ref recipe.parts, length + 1);
            recipe.parts[length] = copy;
            return length;
        }

        /// <summary>Removes the nonroot subtree and its arms. Paired arm sockets stay aligned.</summary>
        public static bool RemovePart(CreatureRecipe recipe, int index)
        {
            if (index <= 0 || !CanEdit(recipe, index)) return false;
            int length = recipe.parts.Length;
            var removed = new bool[length];
            var map = new int[length];
            var parts = new List<CreaturePart>();
            for (int i = 0; i < length; i++)
            {
                removed[i] = i == index || (i > 0 && removed[recipe.parts[i].parent]);
                map[i] = removed[i] ? -1 : parts.Count;
                if (removed[i]) continue;
                CreaturePart part = recipe.parts[i];
                part.parent = i == 0 ? -1 : map[part.parent];
                parts.Add(part);
            }
            var arms = new List<ArmDefinition>();
            var sources = new List<Vector3>();
            for (int i = 0; i < recipe.arms.Length; i++)
            {
                ArmDefinition arm = recipe.arms[i];
                if (removed[arm.bodyPart]) continue;
                arm.bodyPart = map[arm.bodyPart]; arms.Add(arm); sources.Add(recipe.sourceLocal[i]);
            }
            // Preserve sockets that are not arm roots (e.g. a stone's spell source).
            for (int i = recipe.arms.Length; i < recipe.sourceLocal.Length; i++) sources.Add(recipe.sourceLocal[i]);
            recipe.parts = parts.ToArray(); recipe.arms = arms.ToArray(); recipe.sourceLocal = sources.ToArray();
            return true;
        }

        public static int AddArm(CreatureRecipe recipe, int bodyPart)
        {
            if (!CanEdit(recipe, bodyPart) || recipe.arms.Length >= HealerLike.Render.Deliveries.ArmPool.MaxArms) return -1;
            int index = recipe.arms.Length;
            var arm = new ArmDefinition
            {
                bodyPart = bodyPart, rootLocal = Vector3.up * 0.1f,
                segmentCount = 3, segmentLength = 0.15f, radius = 0.018f,
                bendPole = Vector3.forward, colour = recipe.roots.colour,
                tipColour = AuthoredAccent(recipe, bodyPart)
            };
            Array.Resize(ref recipe.arms, index + 1);
            recipe.arms[index] = arm;
            RebuildArmRestPose(recipe, index);
            Vector3 source = arm.rootLocal;
            for (int partIndex = bodyPart; partIndex >= 0; partIndex = recipe.parts[partIndex].parent)
            {
                CreaturePart part = recipe.parts[partIndex];
                source = part.localPosition + Quaternion.Euler(part.localEuler) * source;
            }
            var sources = new List<Vector3>(recipe.sourceLocal);
            sources.Insert(index, source);
            recipe.sourceLocal = sources.ToArray();
            return index;
        }

        public static bool RemoveArm(CreatureRecipe recipe, int index)
        {
            if (recipe == null || recipe.arms == null || index < 0 || index >= recipe.arms.Length ||
                recipe.sourceLocal == null || recipe.sourceLocal.Length < recipe.arms.Length) return false;
            var arms = new List<ArmDefinition>(recipe.arms);
            var sources = new List<Vector3>(recipe.sourceLocal);
            arms.RemoveAt(index); sources.RemoveAt(index);
            recipe.arms = arms.ToArray(); recipe.sourceLocal = sources.ToArray();
            return true;
        }

        /// <summary>Repairs the rest chain after count/length edits. Other arm authoring fields are preserved.</summary>
        public static bool RebuildArmRestPose(CreatureRecipe recipe, int index)
        {
            if (recipe == null || recipe.arms == null || index < 0 || index >= recipe.arms.Length) return false;
            ArmDefinition arm = recipe.arms[index];
            arm.segmentCount = Mathf.Clamp(arm.segmentCount, 2, 128);
            arm.segmentLength = float.IsFinite(arm.segmentLength) ? Mathf.Clamp(arm.segmentLength, 0.001f, 1f) : 0.15f;
            arm.restJoints = new Vector3[arm.segmentCount + 1];
            for (int i = 1; i < arm.restJoints.Length; i++) arm.restJoints[i] = Vector3.up * (arm.segmentLength * i);
            recipe.arms[index] = arm;
            return true;
        }

        public static string[] Validate(CreatureRecipe recipe)
        {
            var warnings = new List<string>();
            if (!CreatureValidator.TryValidate(recipe, out string error)) warnings.Add(error);
            if (recipe == null) return warnings.ToArray();
            if (!Finite(recipe.neckLocal)) warnings.Add("Neck coordinates must be finite.");
            if (!Finite(recipe.wiltColour) || !Finite(recipe.stoneOchre)) warnings.Add("Wilt and stone colours must be finite.");
            if (recipe.parts != null)
                foreach (CreaturePart part in recipe.parts)
                    if (!Enum.IsDefined(typeof(PartRole), part.role)) { warnings.Add("Every part needs a valid role."); break; }
            if (recipe.arms != null)
                foreach (ArmDefinition arm in recipe.arms)
                    if (!Finite(arm.tipColour)) { warnings.Add("Arm tip colours must be finite."); break; }
            return warnings.ToArray();
        }

        static Color AuthoredAccent(CreatureRecipe recipe, int bodyPart)
        {
            foreach (var arm in recipe.arms) if (arm.tipColour.a > 0f) return arm.tipColour;
            foreach (var part in recipe.parts) if (part.role == PartRole.Tip || part.role == PartRole.Head) return part.colour;
            return recipe.parts[bodyPart].colour;
        }

        static bool CanEdit(CreatureRecipe recipe, int index) => recipe != null && recipe.parts != null &&
            index >= 0 && index < recipe.parts.Length && CreatureValidator.TryValidate(recipe, out _);
        static string UniqueId(CreatureRecipe recipe, string basis)
        {
            var used = new HashSet<string>();
            if (recipe.parts != null) foreach (CreaturePart part in recipe.parts) used.Add(part.id);
            string candidate = basis;
            for (int suffix = 2; used.Contains(candidate); suffix++) candidate = basis + " " + suffix;
            return candidate;
        }
        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        static bool Finite(Color value) => float.IsFinite(value.r) && float.IsFinite(value.g) && float.IsFinite(value.b) && float.IsFinite(value.a);
    }
}

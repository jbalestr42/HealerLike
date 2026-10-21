using System;
using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;

namespace HealerLike.Render.Studio.Editor
{
    // Structural edits to a creature recipe that keep it valid for the rig: a parent always comes before its
    // children, and each arm keeps its source socket at the same index. The caller records undo and dirties.
    public static class CreatureRecipeEdits
    {
        // A new part under parent, the root when the recipe is empty; its index, or -1 when refused
        public static int AddPart(CreatureRecipe recipe, int parent, Primitive primitive)
        {
            if (recipe == null || !Enum.IsDefined(typeof(Primitive), primitive))
            {
                return -1;
            }

            int length = 0;
            if (recipe.parts != null)
            {
                length = recipe.parts.Length;
            }

            if (length >= CreatureValidator.MaxParts)
            {
                return -1;
            }

            if (length > 0 && (!IsValid(recipe) || parent < 0 || parent >= length))
            {
                return -1;
            }

            CreaturePart part = new CreaturePart();
            part.id = UniqueId(recipe, primitive.ToString());
            part.primitive = primitive;
            part.dimensions = Vector3.one * 0.15f;
            part.localPosition = Vector3.up * 0.2f;
            part.parent = parent;
            part.colour = Color.white;
            part.role = PartRole.Accessory;
            if (length == 0)
            {
                part.parent = -1;
                part.role = PartRole.Body;
            }
            else
            {
                part.colour = recipe.parts[parent].colour;
            }

            Array.Resize(ref recipe.parts, length + 1);
            recipe.parts[length] = part;
            return length;
        }

        // A copy of one part appended at the end, after every possible parent; its children and arms stay behind
        public static int DuplicatePart(CreatureRecipe recipe, int index)
        {
            if (!CanEdit(recipe, index) || recipe.parts.Length >= CreatureValidator.MaxParts)
            {
                return -1;
            }

            int length = recipe.parts.Length;
            CreaturePart copy = recipe.parts[index];
            copy.id = UniqueId(recipe, copy.id + " Copy");
            // The root's copy hangs under the root instead of becoming a second root
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

        // Removes a part other than the root with its subtree and their arms, keeping the other arm sockets paired
        public static bool RemovePart(CreatureRecipe recipe, int index)
        {
            if (index <= 0 || !CanEdit(recipe, index))
            {
                return false;
            }

            int length = recipe.parts.Length;
            bool[] isRemoved = new bool[length];
            int[] map = new int[length];
            List<CreaturePart> parts = new List<CreaturePart>();
            for (int i = 0; i < length; i++)
            {
                isRemoved[i] = i == index || (i > 0 && isRemoved[recipe.parts[i].parent]);
                map[i] = -1;
                if (isRemoved[i])
                {
                    continue;
                }

                map[i] = parts.Count;
                CreaturePart part = recipe.parts[i];
                part.parent = -1;
                if (i > 0)
                {
                    part.parent = map[recipe.parts[i].parent];
                }
                parts.Add(part);
            }

            List<ArmDefinition> arms = new List<ArmDefinition>();
            List<Vector3> sources = new List<Vector3>();
            for (int i = 0; i < recipe.arms.Length; i++)
            {
                ArmDefinition arm = recipe.arms[i];
                if (isRemoved[arm.bodyPart])
                {
                    continue;
                }

                arm.bodyPart = map[arm.bodyPart];
                arms.Add(arm);
                sources.Add(recipe.sourceLocal[i]);
            }

            // Sockets past the arms are not arm roots, a stone's spell source for one
            for (int i = recipe.arms.Length; i < recipe.sourceLocal.Length; i++)
            {
                sources.Add(recipe.sourceLocal[i]);
            }

            recipe.parts = parts.ToArray();
            recipe.arms = arms.ToArray();
            recipe.sourceLocal = sources.ToArray();
            return true;
        }

        // A three-link arm on the part, its source socket inserted at the arm's index; -1 past the rig's arm limit
        public static int AddArm(CreatureRecipe recipe, int bodyPart)
        {
            if (!CanEdit(recipe, bodyPart) || recipe.arms.Length >= ArmPool.MaxArms)
            {
                return -1;
            }

            int index = recipe.arms.Length;
            ArmDefinition arm = new ArmDefinition();
            arm.bodyPart = bodyPart;
            arm.rootLocal = Vector3.up * 0.1f;
            arm.segmentCount = 3;
            arm.segmentLength = 0.15f;
            arm.radius = 0.018f;
            arm.bendPole = Vector3.forward;
            arm.colour = recipe.roots.colour;
            arm.tipColour = AuthoredAccent(recipe, bodyPart);
            Array.Resize(ref recipe.arms, index + 1);
            recipe.arms[index] = arm;
            RebuildArmRestPose(recipe, index);

            // The arm's root in recipe space, up the part chain
            Vector3 source = arm.rootLocal;
            for (int partIndex = bodyPart; partIndex >= 0; partIndex = recipe.parts[partIndex].parent)
            {
                CreaturePart part = recipe.parts[partIndex];
                source = part.localPosition + Quaternion.Euler(part.localEuler) * source;
            }

            List<Vector3> sources = new List<Vector3>(recipe.sourceLocal);
            sources.Insert(index, source);
            recipe.sourceLocal = sources.ToArray();
            return index;
        }

        public static bool RemoveArm(CreatureRecipe recipe, int index)
        {
            if (recipe == null || recipe.arms == null || index < 0 || index >= recipe.arms.Length)
            {
                return false;
            }

            if (recipe.sourceLocal == null || recipe.sourceLocal.Length < recipe.arms.Length)
            {
                return false;
            }

            List<ArmDefinition> arms = new List<ArmDefinition>(recipe.arms);
            List<Vector3> sources = new List<Vector3>(recipe.sourceLocal);
            arms.RemoveAt(index);
            sources.RemoveAt(index);
            recipe.arms = arms.ToArray();
            recipe.sourceLocal = sources.ToArray();
            return true;
        }

        // A straight rest chain again after the count or length changed, the arm's other fields kept
        public static bool RebuildArmRestPose(CreatureRecipe recipe, int index)
        {
            if (recipe == null || recipe.arms == null || index < 0 || index >= recipe.arms.Length)
            {
                return false;
            }

            ArmDefinition arm = recipe.arms[index];
            arm.segmentCount = Mathf.Clamp(arm.segmentCount, 2, 128);
            arm.segmentLength = Mathf.Clamp(RenderMath.FiniteOr(arm.segmentLength, 0.15f), 0.001f, 1f);

            arm.restJoints = new Vector3[arm.segmentCount + 1];
            for (int i = 1; i < arm.restJoints.Length; i++)
            {
                arm.restJoints[i] = Vector3.up * (arm.segmentLength * i);
            }

            recipe.arms[index] = arm;
            return true;
        }

        // The first authored arm tip, else the first tip or head part's colour, else the body part's own
        static Color AuthoredAccent(CreatureRecipe recipe, int bodyPart)
        {
            foreach (ArmDefinition arm in recipe.arms)
            {
                if (arm.tipColour.a > 0f)
                {
                    return arm.tipColour;
                }
            }

            foreach (CreaturePart part in recipe.parts)
            {
                if (part.role == PartRole.Tip || part.role == PartRole.Head)
                {
                    return part.colour;
                }
            }
            return recipe.parts[bodyPart].colour;
        }

        static bool CanEdit(CreatureRecipe recipe, int index)
        {
            if (recipe == null || recipe.parts == null || index < 0 || index >= recipe.parts.Length)
            {
                return false;
            }
            return IsValid(recipe);
        }

        static bool IsValid(CreatureRecipe recipe)
        {
            string error;
            return CreatureValidator.TryValidate(recipe, out error);
        }

        static string UniqueId(CreatureRecipe recipe, string basis)
        {
            HashSet<string> used = new HashSet<string>();
            if (recipe.parts != null)
            {
                foreach (CreaturePart part in recipe.parts)
                {
                    used.Add(part.id);
                }
            }

            string candidate = basis;
            for (int suffix = 2; used.Contains(candidate); suffix++)
            {
                candidate = basis + " " + suffix;
            }
            return candidate;
        }
    }
}

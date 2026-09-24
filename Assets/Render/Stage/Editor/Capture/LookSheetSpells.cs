using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // The roster's spells as the sheet applies them: a status through the sink's SetStatus, or an outcome
    // through its ShowImpact. The game's character skill handlers are used where they exist, the rest live
    // in memory
    public static class LookSheetSpells
    {
        public static readonly string[] Spells =
        {
            "Heal", "Heal group", "Quicken", "Blight", "Stonefall", "Weaken", "Focus", "Renew", "Barkskin", "Sanctuary",
            "Mark of ruin", "Overgrowth", "Lifebloom", "Sprout", "Transfusion"
        };

        // Cast again on a stone, the side their family is meant for
        public static readonly string[] OnStone = { "Weaken", "Mark of ruin", "Blight" };

        // A factory or an engine change the game does not have yet; the sheet draws the parts and stars the label
        public static readonly string[] StandIns = { "Overgrowth", "Lifebloom", "Sprout", "Transfusion" };

        static readonly string quickenPath =
            "Assets/Data/CharacterSkills/MultiTargetBuffAttackRate/BuffHandlerFactory.asset";
        static readonly string blightPath =
            "Assets/Data/CharacterSkills/PoisonSingleTarget/PoisonSingleTarget_BuffHandlerFactory.asset";
        static readonly string weakenPath =
            "Assets/Data/CharacterSkills/MultiTargetReduceDamage/BuffHandlerFactory.asset";
        static readonly string bouncePath = "Assets/Data/EntityItems/BounceItem/BounceProjectileBehaviourFactory.asset";

        public static bool IsStandIn(string spell)
        {
            return Array.IndexOf(StandIns, spell) >= 0;
        }

        public static string Label(string spell, bool isOnStone)
        {
            string label = spell.ToUpperInvariant() + (IsStandIn(spell) ? "*" : "");
            return isOnStone ? label + " ON STONE" : label;
        }

        // The status a spell leaves, null for the spells that only show an outcome
        public static ABuffHandlerFactory Handler(string spell, List<Object> created)
        {
            switch (spell)
            {
                case "Quicken":
                    return RenderAssets.Load<ABuffHandlerFactory>(quickenPath);
                case "Blight":
                    return RenderAssets.Load<ABuffHandlerFactory>(blightPath);
                case "Weaken":
                    return RenderAssets.Load<ABuffHandlerFactory>(weakenPath);
                case "Focus":
                    // SingleTargetBuffAttackRate with the sign the roster fixes
                    return LookSheetData.Handler(DurationType.Duration, 2f, 0f, created,
                        LookSheetData.Modifier(AttributeType.AttackRate, AttributeModifierType.Multiply, -0.5f,
                            created));
                case "Renew":
                    return LookSheetData.Handler(DurationType.Duration, 6f, 1f, created,
                        LookSheetData.Consume(-4f, created));
                case "Barkskin":
                    return LookSheetData.Handler(DurationType.Instant, 0f, 0f, created,
                        LookSheetData.Modifier(AttributeType.HitArmor, AttributeModifierType.Add, 3f, created));
                case "Sanctuary":
                    InvincibilityBuffFactory invincibility =
                        LookSheetData.Track(ScriptableObject.CreateInstance<InvincibilityBuffFactory>(), created);
                    invincibility.data = new InvincibilityBuffData();
                    return LookSheetData.Handler(DurationType.Duration, 2f, 0f, created, invincibility);
                case "Mark of ruin":
                    return LookSheetData.Handler(DurationType.Duration, 5f, 0f, created,
                        LookSheetData.Modifier(AttributeType.Vulnerability, AttributeModifierType.Add, 0.3f, created));
                case "Overgrowth":
                    // Stand-in: the Bounce item's behaviour lent for 8 s, where the equip item buff would lend the item
                    ProjectileBehaviourBuffFactory lend =
                        LookSheetData.Track(ScriptableObject.CreateInstance<ProjectileBehaviourBuffFactory>(), created);
                    lend.data = new ProjectileBehaviourBuffData
                    {
                        projectileBehaviour = RenderAssets.Load<AProjectileBehaviourFactory>(bouncePath)
                    };
                    return LookSheetData.Handler(DurationType.Duration, 8f, 0f, created, lend);
                default:
                    return null;
            }
        }

        // Stacks drawn with the status: one per HitArmor charge for Barkskin
        public static int Stacks(string spell)
        {
            return spell == "Barkskin" ? 3 : 1;
        }

        // The health outcome a spell shows on its target, 0 when it shows none; negative is damage
        public static float Impact(string spell)
        {
            switch (spell)
            {
                case "Heal":
                    // HealPower 20 times 1.5
                    return 30f;
                case "Heal group":
                    return 16f;
                case "Stonefall":
                    return -60f;
                case "Lifebloom":
                    // Stand-in: a large heal, as the missing health of a hurt ally would scale it
                    return 60f;
                case "Transfusion":
                    return 20f;
                default:
                    return 0f;
            }
        }

        // The sapling a Sprout would place, drawn through the unit grammar
        public static EntityData Sapling(List<Object> created)
        {
            EntityData sapling = LookSheetData.Entity("Sapling", created);
            sapling.attributes[AttributeType.HealthMax] = 50f;
            sapling.skillFactories.Add(LookSheetData.Shoot(LookSheetData.Prefab("BulletSpeed"), 1, created));
            return sapling;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // The stone emissions, each a recipe of fragments spawned through the effects' pool
    public static class StoneEmitters
    {
        // Fragments each recipe spawns; the dust and collapse counts sit on StoneEffects
        public static readonly int HitChips = 3;
        public static readonly int CriticalHitChips = 5;
        public static readonly int SplitPieces = 3;
        public static readonly int MinThrownChips = 3;

        // A thrown contact draws from MinThrownChips to MinThrownChips + thrownChipSpread - 1 chips
        static readonly uint thrownChipSpread = 3;

        public static void Dust(StoneEffects effects, Vector3 position, uint seed)
        {
            if (effects == null || !effects.CanEmit())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            for (int i = 0; i < StoneEffects.DustPuffs; i++)
            {
                Vector3 scale = Vector3.one * random.Range(0.09f, 0.17f);
                float x = random.Range(-0.24f, 0.24f);
                float y = random.Range(0.25f, 0.5f);
                float z = random.Range(-0.24f, 0.24f);
                Vector3 velocity = new Vector3(x, y, z);
                effects.SpawnDust(position, scale, velocity, 0.65f, random.Next());
            }
        }

        // Stone chips thrown off the face that was hit; the damage readout itself is the spell composer's
        public static void Hit(StoneEffects effects, StoneImpact impact, bool critical, uint seed)
        {
            if (effects == null || !effects.CanEmit())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            int chips = critical ? CriticalHitChips : HitChips;
            Vector3 normal = impact.normal.sqrMagnitude > 0f ? impact.normal.normalized : Vector3.up;
            for (int i = 0; i < chips; i++)
            {
                float size = random.Range(0.025f, 0.07f);
                Material material = effects.stoneMaterial;
                if (i % 3 == 0)
                {
                    material = effects.coralMaterial;
                }
                // Keep the random draws in order: direction, speed, then life
                Vector3 velocity = Direction(ref random, normal) * random.Range(0.6f, 1.4f);
                float life = random.Range(0.35f, 0.55f);
                Vector3 position = impact.point + normal * 0.005f;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
                effects.Spawn(effects.debrisMesh, material, position, rotation, Vector3.one * size, velocity, life,
                    impact.point.y - 1f, false, random.Next());
            }
        }

        public static void ThrownContact(StoneEffects effects, Vector3 contact, uint seed)
        {
            if (effects == null || !effects.CanEmit())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            int count = MinThrownChips + (int)(random.Next() % thrownChipSpread);
            for (int i = 0; i < count; i++)
            {
                Quaternion rotation = Quaternion.Euler(random.Range(0f, 180f), random.Range(0f, 360f), 0f);
                Vector3 scale = Vector3.one * random.Range(0.055f, 0.11f);
                Vector3 velocity = Direction(ref random, Vector3.up) * random.Range(0.6f, 1.3f);
                effects.Spawn(effects.debrisMesh, effects.stoneMaterial, contact, rotation, scale, velocity, 0.45f,
                    contact.y, false, random.Next());
            }
        }

        // A shed part falls, then splits into pieces when it lands
        public static void DetachedPart(StoneEffects effects, Mesh mesh, Material material, Matrix4x4 pose,
            Vector3 stoneVelocity, float groundY, uint seed)
        {
            if (effects == null || !effects.CanEmit())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            // A copy belongs to the effects owner, so releasing the enemy's cache lease cannot invalidate it
            Mesh copy = Object.Instantiate(mesh);
            copy.name = "DetachedStone";
            Vector3 direction = new Vector3(random.Range(-1f, 1f), 0f, random.Range(-1f, 1f)).normalized;
            Material partMaterial = material != null ? material : effects.stoneMaterial;
            Vector3 velocity = stoneVelocity + direction * random.Range(0.6f, 1.2f) + Vector3.up * 0.2f;
            effects.Spawn(copy, partMaterial, pose.GetColumn(3), pose.rotation, pose.lossyScale, velocity, 0.25f,
                groundY, false, seed, copy, true);
        }

        public static void Split(StoneEffects effects, Vector3 position, float ground, uint seed)
        {
            if (effects == null || !effects.CanEmit())
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            for (int j = 0; j < SplitPieces; j++)
            {
                Vector3 scale = Vector3.one * random.Range(0.04f, 0.07f);
                Vector3 velocity = new Vector3(random.Range(-0.3f, 0.3f), 0.3f, random.Range(-0.3f, 0.3f));
                effects.Spawn(effects.debrisMesh, effects.stoneMaterial, position, Quaternion.identity, scale,
                    velocity, 0.25f, ground, true, random.Next());
            }
        }

        // Breaks the parts still standing into debris and dust, the caller hides them
        public static void Collapse(StoneEffects effects, IReadOnlyList<Transform> parts, Vector3 stoneVelocity,
            float groundY, uint seed)
        {
            if (effects == null || !effects.CanEmit() || parts == null)
            {
                return;
            }

            List<Transform> standing = new List<Transform>();
            Vector3 centre = Vector3.zero;
            foreach (Transform part in parts)
            {
                if (part != null && part.gameObject.activeSelf)
                {
                    standing.Add(part);
                    centre += part.position;
                }
            }

            if (standing.Count == 0)
            {
                return;
            }

            StoneRandom random = new StoneRandom(seed);
            int count = StoneEffects.CollapseDebris;
            for (int i = 0; i < count; i++)
            {
                Transform part = standing[i % standing.Count];
                float angle = i * Mathf.PI * 2f / count;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * random.Range(1.2f, 1.8f);
                Bounds bounds = part.GetComponent<Renderer>().bounds;
                float offsetX = random.Range(-0.8f, 0.8f);
                float offsetY = random.Range(-0.8f, 0.8f);
                float offsetZ = random.Range(-0.8f, 0.8f);
                Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);
                Vector3 position = bounds.center + Vector3.Scale(bounds.extents, offset);
                Material material = i % 4 == 0 ? effects.coralMaterial : effects.stoneMaterial;
                Vector3 scale = Vector3.one * random.Range(0.06f, 0.16f);
                Vector3 velocity = stoneVelocity + outward + Vector3.up * random.Range(0.7f, 1.5f);
                effects.Spawn(effects.debrisMesh, material, position, part.rotation, scale, velocity, 0.8f, groundY,
                    true, random.Next());
            }

            centre /= standing.Count;
            Dust(effects, new Vector3(centre.x, groundY + 0.15f, centre.z), seed);
        }

        static Vector3 Direction(ref StoneRandom random, Vector3 normal)
        {
            Vector3 direction = new Vector3(random.Range(-1f, 1f), random.Range(0.2f, 1f), random.Range(-1f, 1f));
            if (Vector3.Dot(direction, normal) < 0f)
            {
                direction -= 2f * Vector3.Dot(direction, normal) * normal;
            }
            Vector3 upwardTangent = Vector3.up - normal * Vector3.Dot(Vector3.up, normal);
            return (direction + normal * 0.5f + upwardTangent * 0.3f).normalized;
        }
    }
}

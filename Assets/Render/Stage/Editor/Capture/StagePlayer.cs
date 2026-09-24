using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // What a player does through the game's public calls: place allies near the enemies, cast a skill on a target
    public class StagePlayer
    {
        public static readonly string[] Allies =
        {
            "Assets/Data/Entities/NormalEntity/NormalEntity.asset",
            "Assets/Data/Entities/ChainLightningEntity/ChainLightningEntity.asset"
        };

        static readonly Vector3[] offsets =
        {
            Vector3.left, Vector3.back, Vector3.right, Vector3.forward, 2f * Vector3.left, 2f * Vector3.back
        };

        int _castSlot;

        public void PlaceAllies(RenderManager manager, List<EntityData> allies)
        {
            List<GameObject> enemies = manager.entityManager.GetEntities(Entity.EntityType.Computer);
            Vector3 anchor = enemies.Count > 0 ? enemies[0].transform.position : Vector3.zero;
            GridManager grid = manager.player.grid;
            int next = 0;
            foreach (EntityData data in allies)
            {
                GameObject allyGo = null;
                // SpawnEntity refuses an occupied cell, so the next offset is tried
                while (allyGo == null && next < offsets.Length)
                {
                    Vector3 position = grid.GetNearestWalkablePosition(anchor + offsets[next] * grid.size);
                    next++;
                    allyGo = manager.entityManager.SpawnEntity(data, position, Entity.EntityType.Player);
                }

                Debug.Log($"[StagePlayer] Placed {data.name}: {(allyGo != null ? allyGo.name : "refused")}");
            }
        }

        public static List<EntityData> LoadAllies()
        {
            List<EntityData> allies = new List<EntityData>();
            foreach (string path in Allies)
            {
                allies.Add(AssetDatabase.LoadAssetAtPath<EntityData>(path));
            }
            return allies;
        }

        // Tries the next skill slot on the target, as a click on the button then on the target would
        public void CastOn(RenderManager manager, Entity target)
        {
            Character character = manager.player.character;
            if (target == null || character == null || character.skillSlots.Count == 0)
            {
                return;
            }

            CharacterSkillSlot slot = character.skillSlots[_castSlot++ % character.skillSlots.Count];
            InteractionManager.instance.CancelInteraction();
            slot.UseSkill();
            AInteraction interaction = InteractionManager.instance.GetInteraction();
            Camera camera = manager.gameCamera;
            Vector3 aim = target.targetPoint != null ? target.targetPoint.transform.position : target.transform.position;
            Ray ray = new Ray(camera.transform.position, aim - camera.transform.position);
            if (interaction != null && Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interaction.GetLayerMask())
                && interaction.IsValidTarget(hit.transform.gameObject))
            {
                interaction.OnMouseClick(hit);
                return;
            }

            InteractionManager.instance.CancelInteraction();
        }
    }
}

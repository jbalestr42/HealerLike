using System.Collections.Generic;
using HealerLike.Render.Creatures;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    // The menu's establishing shot: the battle meadow seen low across the grass toward the forest, with one
    // signature creature of each class idling in it. RenderManager builds the grass and the environment around the
    // board this names; this owns the camera's rest pose and drift and the creatures, which stand under one host in
    // the menu scene, so leaving the menu takes them with it even before Clear runs.
    public class StageBackdrop
    {
        public static readonly string HostName = "Render Menu Backdrop";
        public static readonly int MaxCreatures = 3;
        // Board-space feet, in cells from the board centre: x runs away from the camera, z across it
        static readonly Vector3[] feet =
        {
            new Vector3(-2.4f, 0f, 1.7f),
            new Vector3(-1.2f, 0f, 0f),
            new Vector3(-2.7f, 0f, -1.8f),
        };

        // The battle board's size, with no grid drawn on it: the grass and the ring need one to grow around
        public static readonly Bounds Board = new Bounds(Vector3.zero, new Vector3(16f, 0f, 16f));
        // A look across the meadow along the portrait heading, lower than the battle's, the far ring and its ridge
        // fading into the fog at the top of a phone, where the title sits
        public static readonly float Pitch = 26f;
        public static readonly float Height = 8f;
        public static readonly float Setback = 16f;
        // A slow breathing sway, well under the half unit that would make the environment reframe
        public static readonly float DriftAmplitude = 0.18f;
        public static readonly float DriftPeriod = 26f;

        GameObject _host;
        Camera _camera;
        Pose _rest;
        float _cellSize = 1f;
        readonly List<CreaturePreview> _creatures = new List<CreaturePreview>();
        readonly List<Vector3> _positions = new List<Vector3>();

        public bool isAttached { get { return _host != null; } }
        public GameObject host { get { return _host; } }
        public int creatureCount { get { return _creatures.Count; } }
        public Pose rest { get { return _rest; } }

        // Only the Toolkit menu gets the backdrop; the game scenes bring their own board
        public static bool Hosts(string path)
        {
            return path == StageTarget.MenuPath;
        }

        // Behind the creatures along the portrait yaw, looking down at the pitch
        public static Pose RestPose(Bounds board)
        {
            Quaternion heading = Quaternion.Euler(0f, StageCalibration.PortraitYaw, 0f);
            Vector3 position = board.center - heading * Vector3.forward * Setback + Vector3.up * (board.max.y + Height);
            return new Pose(position, Quaternion.Euler(Pitch, StageCalibration.PortraitYaw, 0f));
        }

        // The rest pose slid sideways and a touch up and down; the view direction never changes
        public static Pose DriftPose(Pose rest, float time)
        {
            float phase = time * 2f * Mathf.PI / DriftPeriod;
            Vector3 right = rest.rotation * Vector3.right;
            Vector3 offset = right * (DriftAmplitude * Mathf.Sin(phase))
                             + Vector3.up * (DriftAmplitude * 0.35f * Mathf.Sin(phase * 2f));
            return new Pose(rest.position + offset, rest.rotation);
        }

        public static Vector3 Foot(Bounds board, int index, float cellSize)
        {
            Quaternion heading = Quaternion.Euler(0f, StageCalibration.PortraitYaw, 0f);
            Vector3 local = feet[index % feet.Length] * cellSize;
            // Board space turned so x runs along the view; z across it
            Vector3 world = heading * new Vector3(-local.z, 0f, local.x);
            return new Vector3(board.center.x + world.x, board.max.y, board.center.z + world.z);
        }

        // One creature per class the menu offers, its signature unit: the last of its roster that no other class
        // deploys (Cleric's Zealot, Druid's Grove Keeper, Warlock's Blood Cultist), else its last unit not yet standing
        public static List<EntityData> Signatures(GameData data)
        {
            List<EntityData> signatures = new List<EntityData>();
            if (data == null || data.characters == null)
            {
                return signatures;
            }

            foreach (CharacterData character in data.characters)
            {
                if (signatures.Count >= MaxCreatures)
                {
                    break;
                }

                EntityData signature = Signature(data.characters, character, signatures);
                if (signature != null)
                {
                    signatures.Add(signature);
                }
            }

            return signatures;
        }

        static EntityData Signature(List<CharacterData> characters, CharacterData character, List<EntityData> taken)
        {
            if (character == null || character.entities == null)
            {
                return null;
            }

            EntityData fallback = null;
            for (int i = character.entities.Count - 1; i >= 0; i--)
            {
                EntityData entity = character.entities[i];
                if (entity == null || taken.Contains(entity))
                {
                    continue;
                }

                if (!IsDeployedByAnother(characters, character, entity))
                {
                    return entity;
                }

                fallback = fallback ?? entity;
            }

            return fallback;
        }

        static bool IsDeployedByAnother(List<CharacterData> characters, CharacterData owner, EntityData entity)
        {
            foreach (CharacterData other in characters)
            {
                if (other != null && other != owner && other.entities != null && other.entities.Contains(entity))
                {
                    return true;
                }
            }

            return false;
        }

        public void Init(Scene scene, CreatureLooks looks, PrimitiveMeshes meshes, Camera camera, Bounds board,
            float cellSize, IReadOnlyList<EntityData> creatures)
        {
            Clear();
            _camera = camera;
            _cellSize = cellSize;
            _rest = camera != null ? new Pose(camera.transform.position, camera.transform.rotation) : RestPose(board);
            _host = new GameObject(HostName);
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(_host, scene);
            }

            if (looks == null || creatures == null)
            {
                return;
            }

            for (int i = 0; i < creatures.Count && _creatures.Count < MaxCreatures; i++)
            {
                EntityData data = creatures[i];
                CreaturePreview preview = new CreaturePreview();
                if (data == null || !preview.Init(looks, data, Entity.EntityType.Player, meshes, _host.transform,
                        cellSize))
                {
                    preview.Dispose();
                    continue;
                }

                _positions.Add(Foot(board, _creatures.Count, cellSize));
                _creatures.Add(preview);
            }

            Tick(0f, 0f);
        }

        public void Tick(float time, float deltaTime)
        {
            if (_host == null)
            {
                return;
            }

            Vector3? facing = null;
            if (_camera != null)
            {
                Pose pose = DriftPose(_rest, time);
                _camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
                facing = -_camera.transform.forward;
            }

            for (int i = 0; i < _creatures.Count; i++)
            {
                _creatures[i].Tick(time, deltaTime, new FootFrame(_positions[i], Vector3.up, _cellSize), facing);
            }
        }

        public void Clear()
        {
            foreach (CreaturePreview creature in _creatures)
            {
                creature.Dispose();
            }

            _creatures.Clear();
            _positions.Clear();
            if (_host != null)
            {
                RenderObjects.Release(_host);
            }

            _host = null;
            _camera = null;
        }
    }
}

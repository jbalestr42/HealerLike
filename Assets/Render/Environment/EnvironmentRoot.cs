using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Environment
{
    // The root of Environment.prefab: holds its parts and hands each one what it reads, once
    public class EnvironmentRoot : MonoBehaviour
    {
        // The plane sits a hair under the board top
        static readonly float groundDrop = 0.01f;

        [SerializeField] EnvironmentScatter _scatter;
        [SerializeField] EnvironmentForeground _foreground;
        [SerializeField] EnvironmentRidge _ridge;
        [SerializeField] EnvironmentGrass _grass;
        [SerializeField] EnvironmentGust _gust;
        [SerializeField] Transform _ground;

        public EnvironmentForeground foreground { get { return _foreground; } }
        public EnvironmentRidge ridge { get { return _ridge; } }
        public EnvironmentGrass grass { get { return _grass; } }
        public EnvironmentGust gust { get { return _gust; } }

        public void Frame(Camera camera, Bounds board)
        {
            if (camera == null) return;
            Vector2 fog = StageCalibration.BackgroundFog(camera.transform.position, board, camera.transform.eulerAngles.y);
            _scatter.Frame(camera);
            _ridge.Frame(fog.x, fog.y);
            if (_foreground.enabled) _foreground.Build();
        }

        public void Init(PrimitiveMeshes meshes, Camera camera, Rect board, float cellSize, float surfaceY,
                         ZoneRegistry zones, float fogStart, float fogEnd)
        {
            if (_scatter == null || _foreground == null || _ridge == null || _grass == null)
            {
                Debug.LogError("[EnvironmentRoot] The prefab is missing one of its parts.");
                return;
            }

            if (_ground != null)
            {
                _ground.position = new Vector3(board.center.x, surfaceY - groundDrop, board.center.y);
            }

            _grass.Init(board, cellSize, surfaceY, camera, zones);
            _scatter.Init(board, cellSize, surfaceY, camera, _gust, fogEnd, meshes);
            _foreground.Init(camera, surfaceY, meshes);
            _ridge.Init(camera, board, surfaceY, fogStart, fogEnd, meshes);
        }
    }
}

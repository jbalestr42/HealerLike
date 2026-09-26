using UnityEngine;
using HealerLike.Render.Environment;
using HealerLike.Render.Look;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    public class StageEnvironment
    {
        EnvironmentRoot _root;
        Pose _view;

        public EnvironmentRoot root { get { return _root; } }

        public EnvironmentGust gust { get { return _root != null ? _root.gust : null; } }
        public EnvironmentForeground foreground { get { return _root != null ? _root.foreground : null; } }

        public void Init(EnvironmentRoot prefab, RenderManager manager, Rect board)
        {
            Clear();
            _root = Object.Instantiate(prefab, manager.transform);
            LookSettings settings = manager.look.settings;
            _root.Init(manager.meshes, manager.gameCamera, board, manager.player.grid.size, manager.board.max.y,
                manager.zones, settings.fogStart, settings.fogEnd);
            Frame(manager.gameCamera, manager.board, true);
        }

        public void Tick(ZoneRegistry zones)
        {
            if (_root != null)
            {
                _root.grass.UpdateStrips(zones);
            }
        }

        public void Frame(Camera camera, Bounds board, bool force)
        {
            if (_root == null || camera == null)
            {
                return;
            }

            Transform view = camera.transform;
            if (!force && Vector3.Distance(view.position, _view.position) < 0.5f
                && Quaternion.Angle(view.rotation, _view.rotation) < 0.5f)
            {
                return;
            }
            _root.Frame(camera, board);
            _view = new Pose(view.position, view.rotation);
        }

        public void Clear()
        {
            if (_root != null)
            {
                RenderObjects.Release(_root.gameObject);
            }
            _root = null;
        }
    }
}

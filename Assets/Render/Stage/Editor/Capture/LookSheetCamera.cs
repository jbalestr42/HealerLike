using HealerLike.Render.Creatures;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Stage
{
    // The two cameras a sheet is rendered at: today's portrait overview, and the board camera the reference study
    // names, the 16-cell board filling 1080 px so a cell at the board centre is about 67 px; and the renders
    // a sheet reads back from them
    public static class LookSheetCamera
    {
        public static readonly int BoardCells = 16;

        // Same pitch and field of view as the portrait, pulled back along its view until a cell at the board centre
        // spans width / BoardCells pixels
        public static Pose Board(Vector3 centre, float cellSize, Quaternion rotation, float fieldOfView, float aspect,
                                 int width)
        {
            float cellPixels = (float)width / BoardCells;
            float tanH = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect;
            float distance = cellSize * width / (2f * tanH * cellPixels);
            return new Pose(centre - rotation * Vector3.forward * distance, rotation);
        }

        // How wide a cell at the point draws, in pixels of a frame the given width
        public static float CellPixels(Camera camera, Vector3 point, float cellSize, int width)
        {
            Vector3 centre = camera.WorldToViewportPoint(point);
            Vector3 across = camera.WorldToViewportPoint(point + camera.transform.right * cellSize);
            return (across.x - centre.x) * width;
        }

        public static Vector2Int ToPixel(Camera camera, Vector3 world, int width, int height)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            return new Vector2Int(Mathf.RoundToInt(viewport.x * width), Mathf.RoundToInt(viewport.y * height));
        }

        // The middle of what a unit draws, the hidden game model aside
        public static Vector3 DrawnCentre(Transform unit)
        {
            Bounds bounds = new Bounds(unit.position, Vector3.zero);
            foreach (Renderer renderer in CreatureRenderers.Find(unit))
            {
                if (renderer.enabled && renderer is MeshRenderer)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return bounds.center;
        }

        // The frame as the camera draws it, rendered into a texture and read back
        public static Color32[] Render(Camera camera, int width, int height)
        {
            Texture2D texture = StageReadback.Render(camera, width, height);
            try
            {
                return texture.GetPixels32();
            }
            finally
            {
                RenderObjects.Release(texture);
            }
        }

        // The game frame without grass: the grass only draws for the game camera, so a copy of it sees the ground,
        // painted for this render in the given colour with its cell grid off
        public static Color32[] RenderFlat(Camera flatCamera, Camera game, Renderer ground, Color colour, int width,
                                           int height)
        {
            flatCamera.CopyFrom(game);
            UniversalAdditionalCameraData flatData = flatCamera.GetUniversalAdditionalCameraData();
            flatData.renderPostProcessing = game.GetUniversalAdditionalCameraData().renderPostProcessing;
            MaterialPropertyBlock saved = new MaterialPropertyBlock();
            MaterialPropertyBlock flat = new MaterialPropertyBlock();
            if (ground != null)
            {
                ground.GetPropertyBlock(saved);
                ground.GetPropertyBlock(flat);
                flat.SetColor(RenderObjects.BaseColorId, colour);
                flat.SetFloat("_HLGroundGrid", 0f);
                ground.SetPropertyBlock(flat);
            }

            bool enabled = flatCamera.enabled;
            flatCamera.enabled = true;
            try
            {
                return Render(flatCamera, width, height);
            }
            finally
            {
                flatCamera.enabled = enabled;
                if (ground != null)
                {
                    ground.SetPropertyBlock(saved);
                }
            }
        }
    }
}

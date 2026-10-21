using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The battle focus framing: the box a body takes and the viewport frames the fight is kept inside
    public static class BattleFocusBounds
    {
        // Bodies occupy the central playfield with space below for spell entry and above for the HUD.
        public static readonly Rect FocusFrame = Rect.MinMaxRect(0.08f, 0.16f, 0.92f, 0.80f);
        // Negative lift puts the combat bounds centre at viewport y=.44 instead of reserving space for a hidden healer.
        public static readonly float FocusLift = -0.12f;
        // While every body stays inside this wider frame the view keeps easing, past it the view widens at once
        public static readonly Rect VisibleFrame = Rect.MinMaxRect(0.06f, 0.14f, 0.94f, 0.82f);
        // Room around the bodies for heads and roots
        public static readonly Vector3 Padding = new Vector3(0.8f, 0.5f, 0.8f);

        // A body with no mesh renderer yet, and the size past which a renderer is an effect rather than a body
        static readonly Vector3 defaultBodySize = new Vector3(1.2f, 2f, 1.2f);
        static readonly float maxBodySize = 8f;

        public static Pose Fit(Bounds bounds, float pitch, float fov, float aspect)
        {
            return StageCalibration.Fit(bounds, pitch, fov, aspect, FocusFrame, FocusLift);
        }

        public static Pose Fit(Bounds bounds, Quaternion rotation, float fov, float aspect)
        {
            return StageCalibration.Fit(bounds, rotation, fov, aspect, FocusFrame, FocusLift);
        }

        // Authored mesh bounds include heads and roots, transient effects and lines are left out
        public static Bounds Body(Transform body, Renderer[] renderers)
        {
            bool hasMesh = false;
            Bounds bounds = new Bounds(body.position + Vector3.up, defaultBodySize);
            foreach (Renderer bodyRenderer in renderers)
            {
                bool isBody = bodyRenderer != null && bodyRenderer.enabled && bodyRenderer.gameObject.activeInHierarchy
                              && bodyRenderer is MeshRenderer && bodyRenderer.bounds.size.magnitude < maxBodySize;
                if (!isBody)
                {
                    continue;
                }

                if (hasMesh)
                {
                    bounds.Encapsulate(bodyRenderer.bounds);
                }
                else
                {
                    bounds = bodyRenderer.bounds;
                    hasMesh = true;
                }
            }

            return bounds;
        }
    }
}

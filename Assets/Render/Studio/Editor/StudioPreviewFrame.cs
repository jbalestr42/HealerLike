using System;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // One render of a studio preview through the stage pipeline and the studio look, into the window or a texture
    public static class StudioPreviewFrame
    {
        public static void Draw(PreviewRenderUtility utility, Rect rect, Action render = null)
        {
            utility.BeginPreview(rect, GUIStyle.none);
            Texture texture;
            try
            {
                Render(utility, render);
            }
            finally
            {
                texture = utility.EndPreview();
            }
            if (texture)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
            }
            GUI.Label(new Rect(rect.x + 12f, rect.yMax - 24f, rect.width - 24f, 18f),
                "Drag to orbit  ·  Scroll to zoom  ·  Shift-drag to pan", EditorStyles.whiteMiniLabel);
        }

        // The caller owns the texture, it outlives the preview
        public static Texture2D Capture(PreviewRenderUtility utility, int width, int height, Action render = null)
        {
            utility.BeginStaticPreview(new Rect(0f, 0f, width, height));
            bool completed = false;
            Texture2D image;
            try
            {
                Render(utility, render);
                completed = true;
            }
            finally
            {
                image = utility.EndStaticPreview();
                if (!completed && image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            return image;
        }

        public static Texture2D Capture(PreviewRenderUtility utility, StudioPreviewCamera framing,
            IPreviewSubject subject, int width, int height)
        {
            Camera camera = utility.camera;
            framing.BeginCapture(camera);
            try
            {
                framing.Apply(camera, width, height, false, subject);
                return Capture(utility, width, height);
            }
            finally
            {
                framing.EndCapture(camera);
            }
        }

        // A capture's size, kept between 16 and 4096 pixels a side
        public static int ClampSize(int size)
        {
            return Mathf.Clamp(size, 16, 4096);
        }

        static void Render(PreviewRenderUtility utility, Action render)
        {
            using (StudioPipelineScope pipeline = new StudioPipelineScope())
            using (StudioLookScope look = new StudioLookScope())
            {
                pipeline.Begin();
                look.Begin(utility.camera);
                if (render != null)
                {
                    render();
                }
                else
                {
                    utility.Render(true, false);
                }
            }
        }

        // Grows bounds over the subject's visible renderers; true once the bounds hold one
        public static bool Encapsulate(GameObject subject, ref Bounds bounds, bool isFound)
        {
            if (!subject)
            {
                return isFound;
            }

            foreach (Renderer renderer in subject.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.bounds.size.sqrMagnitude < 0.00001f)
                {
                    continue;
                }

                if (!isFound)
                {
                    bounds = renderer.bounds;
                    isFound = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return isFound;
        }

        // The message a preview draws in place of its render
        public static void DrawMessage(Rect rect, string message)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, 90f), message,
                EditorStyles.wordWrappedLabel);
        }
    }
}

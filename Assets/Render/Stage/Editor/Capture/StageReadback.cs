using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{
    // Renders a camera into a texture of the given size and reads it back; the caller destroys the texture
    public static class StageReadback
    {
        public static Texture2D Render(Camera camera, int width, int height)
        {
            float aspect = camera.aspect;
            RenderTexture target = null;
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = null;
            bool completed = false;
            try
            {
                target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                camera.aspect = (float)width / height;
                RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();
                request.destination = target;
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                completed = true;
                return texture;
            }
            finally
            {
                if (camera != null)
                {
                    camera.aspect = aspect;
                }
                RenderTexture.active = previous;
                if (target != null)
                {
                    RenderTexture.ReleaseTemporary(target);
                }
                if (!completed)
                {
                    RenderObjects.Release(texture);
                }
            }
        }
    }
}

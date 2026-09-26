using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{
    // Renders a camera into a texture of the given size and reads it back; the caller destroys the texture
    public static class StageReadback
    {
        public static Texture2D Render(Camera camera, int width, int height)
        {
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            float aspect = camera.aspect;
            bool completed = false;
            try
            {
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
                camera.aspect = aspect;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (!completed)
                {
                    RenderObjects.Release(texture);
                }
            }
        }
    }
}

using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Whether this device binds a StructuredBuffer to a vertex shader. OpenGL ES 3.1 guarantees none there
    // (GL_MAX_VERTEX_SHADER_STORAGE_BLOCKS may be 0), and the ground simulation's stamp passes, the grass tufts and
    // the heal rings all read theirs from vertex stages, so they run only when the device reports at least one.
    public static class VertexStorageBuffers
    {
        public static readonly string UnavailableReason = "vertex storage buffers unavailable";

        public static bool isAvailable
        {
            get { return !isForcedUnavailable && SystemInfo.maxComputeBufferInputsVertex >= 1; }
        }

#if UNITY_EDITOR
        // Editor only, never in a player: a test or a capture simulates a device without them
        public static bool isForcedUnavailable;
#else
        static bool isForcedUnavailable { get { return false; } }
#endif
    }
}

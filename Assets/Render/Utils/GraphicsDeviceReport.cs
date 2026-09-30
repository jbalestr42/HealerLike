using System.Text;
using HealerLike.Render.Grass;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render
{
    // What graphics device the player got, logged once at startup for any scene. A Mali device carries its driver
    // revision in graphicsDeviceVersion, which is the line to read. Every line starts with the tag so one logcat grep
    // returns the whole block.
    public static class GraphicsDeviceReport
    {
        public static readonly string Tag = "[HLGFX]";

        static bool _isLogged;

        public struct Facts
        {
            public string appVersion;
            public bool isDevelopmentBuild;
            public string deviceModel;
            public string operatingSystem;
            public GraphicsDeviceType deviceType;
            public string deviceName;
            public string deviceVersion;
            public string deviceVendor;
            public int shaderLevel;
            public bool supportsComputeShaders;
            public int maxComputeBufferInputsVertex;
            public int maxComputeBufferInputsFragment;
            public bool supportsArgbHalf;
            public bool supportsRHalf;
            // Null when the ground simulation can run
            public string groundUnsupportedReason;
        }

        public static Facts Capture()
        {
            return new Facts
            {
                appVersion = Application.version,
                isDevelopmentBuild = Debug.isDebugBuild,
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                deviceType = SystemInfo.graphicsDeviceType,
                deviceName = SystemInfo.graphicsDeviceName,
                deviceVersion = SystemInfo.graphicsDeviceVersion,
                deviceVendor = SystemInfo.graphicsDeviceVendor,
                shaderLevel = SystemInfo.graphicsShaderLevel,
                supportsComputeShaders = SystemInfo.supportsComputeShaders,
                maxComputeBufferInputsVertex = SystemInfo.maxComputeBufferInputsVertex,
                maxComputeBufferInputsFragment = SystemInfo.maxComputeBufferInputsFragment,
                supportsArgbHalf = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf),
                supportsRHalf = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf),
                groundUnsupportedReason = GroundSimulation.UnsupportedReason()
            };
        }

        public static string Compose(Facts facts)
        {
            StringBuilder text = new StringBuilder();
            Line(text, "---- graphics device ----");
            Line(text, "app version: " + facts.appVersion
                + (facts.isDevelopmentBuild ? " (development build)" : " (release build)"));
            Line(text, "device model: " + facts.deviceModel);
            Line(text, "operating system: " + facts.operatingSystem);
            Line(text, "graphicsDeviceType: " + facts.deviceType);
            Line(text, "graphicsDeviceName: " + facts.deviceName);
            Line(text, "graphicsDeviceVersion: " + facts.deviceVersion);
            Line(text, "graphicsDeviceVendor: " + facts.deviceVendor);
            Line(text, "graphicsShaderLevel: " + facts.shaderLevel);
            Line(text, "supportsComputeShaders: " + facts.supportsComputeShaders);
            Line(text, "maxComputeBufferInputsVertex: " + facts.maxComputeBufferInputsVertex);
            Line(text, "maxComputeBufferInputsFragment: " + facts.maxComputeBufferInputsFragment);
            Line(text, "renderTexture ARGBHalf: " + facts.supportsArgbHalf);
            Line(text, "renderTexture RHalf: " + facts.supportsRHalf);
            Line(text, "ground simulation: "
                + (facts.groundUnsupportedReason == null ? "supported" : "off, " + facts.groundUnsupportedReason));
            text.Append(Tag).Append(" ---- end graphics device ----");
            return text.ToString();
        }

        // Also reachable through -executeMethod, so a batchmode Editor prints the same block for its own device
        public static void Log()
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", Compose(Capture()));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LogOnce()
        {
            if (_isLogged)
            {
                return;
            }
            _isLogged = true;
            Log();
        }

        static void Line(StringBuilder text, string line)
        {
            text.Append(Tag).Append(' ').Append(line).Append('\n');
        }
    }
}

using System;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The scene the stage loads beside it at boot. The Toolkit menu unless something picked another one, so a plain
    // boot opens on Start expedition (the class screen) and Sandbox; StageLauncher reads it at boot and
    // StageInterface records every gameplay choice the menu makes. Capture sessions select their scene up front.
    public static class StageTarget
    {
        public static readonly string MainPath = "Assets/Scenes/Main.unity";
        public static readonly string SandboxPath = "Assets/Scenes/Sandbox.unity";
        public static readonly string MenuPath = "Assets/Scenes/Toolkit/MenuToolkit.unity";

        // What a boot with nothing selected opens
        public static readonly string BootPath = MenuPath;

        static string _scenePath;

        public static bool isSelected { get { return _scenePath != null; } }

        public static string scenePath { get { return _scenePath ?? BootPath; } }

        public static void Select(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("The stage target needs a scene path.", nameof(path));
            }

            _scenePath = path;
        }

        // Back to no selection: the next boot opens the menu
        public static void Reset()
        {
            _scenePath = null;
        }

        // The selection when one was made, else the path the launcher was authored with, else the menu
        public static string Resolve(string authoredPath)
        {
            if (_scenePath != null)
            {
                return _scenePath;
            }

            return string.IsNullOrEmpty(authoredPath) ? BootPath : authoredPath;
        }

        // A play session never inherits the previous one's choice, even without a domain reload
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Reset();
        }
    }
}

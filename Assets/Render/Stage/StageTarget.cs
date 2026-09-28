using System;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The game scene the stage loads beside it. Main unless the menu picked another one; StageLauncher reads it at
    // boot and StageInterface records every gameplay choice the menu makes.
    public static class StageTarget
    {
        public static readonly string MainPath = "Assets/Scenes/Main.unity";
        public static readonly string SandboxPath = "Assets/Scenes/Sandbox.unity";

        static string _scenePath;

        public static bool isSelected { get { return _scenePath != null; } }

        public static string scenePath { get { return _scenePath ?? MainPath; } }

        public static void Select(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("The stage target needs a scene path.", nameof(path));
            }

            _scenePath = path;
        }

        // Back to Main, the scene a normal start plays
        public static void Reset()
        {
            _scenePath = null;
        }

        // The selection when one was made, else the path the launcher was authored with, else Main
        public static string Resolve(string authoredPath)
        {
            if (_scenePath != null)
            {
                return _scenePath;
            }

            return string.IsNullOrEmpty(authoredPath) ? MainPath : authoredPath;
        }

        // A play session never inherits the previous one's choice, even without a domain reload
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Reset();
        }
    }
}

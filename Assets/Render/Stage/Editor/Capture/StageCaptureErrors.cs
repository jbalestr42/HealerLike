using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Keep engine errors visible in the original log and make them fail native acceptance.
    public sealed class StageCaptureErrors : IDisposable
    {
        public readonly List<string> messages = new List<string>();
        public int count { get; private set; }
        public StageCaptureErrors() { Application.logMessageReceived += OnLog; }
        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            count++;
            if (messages.Count < 16)
            {
                messages.Add(message + "\n" + stack);
            }
        }
        public void Dispose() { Application.logMessageReceived -= OnLog; }
    }
}

using System;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    // Fixtures borrow the live shader publication and restore it after releasing their own simulations.
    public class GroundTestGlobals : IDisposable
    {
        readonly Texture _motion = Shader.GetGlobalTexture(GroundSimulation.MotionId);
        readonly Texture _crush = Shader.GetGlobalTexture(GroundSimulation.CrushId);
        readonly Texture _state = Shader.GetGlobalTexture(GroundSimulation.StateId);
        readonly Vector4 _rect = Shader.GetGlobalVector(GroundSimulation.RectId);
        readonly float _active = Shader.GetGlobalFloat(GroundSimulation.ActiveId);

        public void Dispose()
        {
            Shader.SetGlobalTexture(GroundSimulation.MotionId, _motion);
            Shader.SetGlobalTexture(GroundSimulation.CrushId, _crush);
            Shader.SetGlobalTexture(GroundSimulation.StateId, _state);
            Shader.SetGlobalVector(GroundSimulation.RectId, _rect);
            Shader.SetGlobalFloat(GroundSimulation.ActiveId, _active);
        }
    }
}

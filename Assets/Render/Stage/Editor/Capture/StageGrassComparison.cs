using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    public class StageGrassComparison : IDisposable
    {
        class DrawState
        {
            public GrassDraw draw;
            public float normalEdges;
            public ShadowCastingMode shadows;
        }

        readonly List<DrawState> _states = new List<DrawState>();

        public StageGrassComparison(GrassField[] fields)
        {
            foreach (GrassField field in fields)
            {
                GrassDraw draw = field.tuftDraw;
                if (draw != null)
                {
                    _states.Add(new DrawState
                    {
                        draw = draw,
                        normalEdges = draw.properties.HasFloat("_HLNormalEdges")
                            ? draw.properties.GetFloat("_HLNormalEdges")
                            : field.lookMaterial.GetFloat("_HLNormalEdges"),
                        shadows = draw.shadowCastingMode
                    });
                }
            }
        }

        public void Dispose()
        {
            foreach (DrawState state in _states)
            {
                state.draw.properties.SetFloat("_HLNormalEdges", state.normalEdges);
                state.draw.shadowCastingMode = state.shadows;
            }
            _states.Clear();
        }
    }
}

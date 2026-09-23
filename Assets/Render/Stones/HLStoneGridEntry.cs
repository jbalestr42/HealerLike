using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealerLike.Render.Stones
{
    // Disabled entries keep their lock: disabling this component does not stop the generator's coroutine.
    [RequireComponent(typeof(GridGenerator))]
    public class HLStoneGridEntry : MonoBehaviour
    {
        [FormerlySerializedAs("demoSceneOnly")]
        [SerializeField] bool _demoSceneOnly;
        [FormerlySerializedAs("generator")]
        [SerializeField] GridGenerator _generator;
        [FormerlySerializedAs("systems")]
        [SerializeField] HLStoneBlockGridSystem[] _systems;
        [FormerlySerializedAs("completionFence")]
        [SerializeField] HLStoneGenerationFence _completionFence;

        public bool isGenerating { get; private set; }

        public void Generate(GridManager grid, Transform ground, int seed)
        {
            if (!_demoSceneOnly)
            {
                throw new InvalidOperationException(
                    "Stone grid generation is demo-scene-only; production attachment must never call Generate");
            }
            if (isGenerating)
            {
                throw new InvalidOperationException("Wait for the previous stone grid generation to finish");
            }
            if (grid == null || ground == null)
            {
                throw new ArgumentNullException(grid == null ? nameof(grid) : nameof(ground));
            }
            if (_completionFence == null || _systems == null || _systems.Length == 0)
            {
                throw new InvalidOperationException("Wire the stone systems and final generation fence");
            }

            if (_generator == null)
            {
                _generator = GetComponent<GridGenerator>();
            }
            foreach (HLStoneBlockGridSystem system in _systems)
            {
                system.Configure(seed, grid.size);
            }

            isGenerating = true;
            _completionFence.Bind(this);
            try
            {
                _generator.Generate(grid, ground, seed);
            }
            catch
            {
                isGenerating = false;
                throw;
            }
        }

        public void CompleteGeneration()
        {
            isGenerating = false;
        }
    }
}

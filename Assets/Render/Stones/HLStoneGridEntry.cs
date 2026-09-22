using System;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    [RequireComponent(typeof(GridGenerator))]
    public sealed class HLStoneGridEntry : MonoBehaviour
    {
        [SerializeField] GridGenerator generator;
        [SerializeField] HLStoneBlockGridSystem[] systems;
        [SerializeField] HLStoneGenerationFence completionFence;
        public bool IsGenerating { get; private set; }
        public void Generate(GridManager grid,Transform ground,int seed)
        {
            if(IsGenerating) throw new InvalidOperationException("Wait for the previous stone grid generation to finish");
            if(grid==null || ground==null) throw new ArgumentNullException(grid==null?nameof(grid):nameof(ground));
            if(completionFence==null || systems==null || systems.Length==0) throw new InvalidOperationException("Wire the stone systems and final generation fence");
            if(generator==null) generator=GetComponent<GridGenerator>();
            foreach(var system in systems) system.Configure(seed,grid.size);
            IsGenerating=true; completionFence.Bind(this);
            try { generator.Generate(grid,ground,seed); }
            catch { IsGenerating=false; throw; }
        }
        public void CompleteGeneration() { IsGenerating=false; }
        // Disabled entries retain their lock: disabling this component does not stop the generator's coroutine.
    }
}

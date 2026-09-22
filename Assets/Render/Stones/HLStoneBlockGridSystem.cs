using System;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public sealed class HLStoneBlockGridSystem : BlockGridSystem
    {
        int gridSeed; float cellSize; bool configured;
        public void Configure(int seed,float size)
        {
            if(!float.IsFinite(size) || size<=0) throw new ArgumentOutOfRangeException(nameof(size));
            gridSeed=seed; cellSize=size; configured=true;
        }
        public override GameObject SpawnObject(GridCell cell)
        {
            if(!configured) throw new InvalidOperationException("Configure the stone seed before spawning");
            var go=base.SpawnObject(cell);
            var clump=go.GetComponentInChildren<HLStoneTerrainClump>();
            if(clump==null) throw new InvalidOperationException("Stone block prefab needs HLStoneTerrainClump");
            clump.Initialize(HLStoneSeed.ForCell(gridSeed,cell.coord),cellSize);
            return go;
        }
    }
}

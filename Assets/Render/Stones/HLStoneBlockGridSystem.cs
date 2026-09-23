using System;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneBlockGridSystem : BlockGridSystem
    {
        int _gridSeed;
        float _cellSize;
        bool _isConfigured;

        public void Configure(int seed, float size)
        {
            if (!float.IsFinite(size) || size <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            _gridSeed = seed;
            _cellSize = size;
            _isConfigured = true;
        }

        public override GameObject SpawnObject(GridCell cell)
        {
            if (!_isConfigured)
            {
                throw new InvalidOperationException("Configure the stone seed before spawning");
            }

            GameObject blockGo = base.SpawnObject(cell);
            HLStoneTerrainClump clump = blockGo.GetComponentInChildren<HLStoneTerrainClump>();
            if (clump == null)
            {
                throw new InvalidOperationException("Stone block prefab needs HLStoneTerrainClump");
            }

            clump.Initialize(HLStoneSeed.ForCell(_gridSeed, cell.coord), _cellSize);
            return blockGo;
        }
    }
}

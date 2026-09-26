using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    // The cells a boost buff lays around a creature, shown in the grass instead of as flat tiles: each cell's own
    // renderers are hidden and it becomes a soft patch of lush, lit grass for as long as it stands. The cells are
    // the buff's children of the entity, so they are found when that entity's children change.
    public class BoostCellGround : MonoBehaviour
    {
        // In cells: the lit patch's radius, a little inside its cell so neighbours read apart
        public static readonly float PatchRadius = 0.45f;

        struct Patch
        {
            public BoostCell cell;
            public ZoneHandle zone;
        }

        readonly List<Patch> _patches = new List<Patch>();
        readonly List<BoostCell> _found = new List<BoostCell>();
        Transform _owner;
        Collider _hold;
        ZoneRegistry _zones;
        float _cellSize = 1f;
        int _childrenKey = -1;

        public int patchCount { get { return _patches.Count; } }

        public void Init(Transform owner, ZoneRegistry zones, float cellSize)
        {
            Clear();
            _owner = owner;
            _hold = EntityHold.Find(owner);
            _zones = zones;
            _cellSize = RenderMath.IsPositive(cellSize) ? cellSize : 1f;
            _childrenKey = -1;
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!_owner || _zones == null)
            {
                return;
            }

            // Which children the entity has, not only how many: a cell laid the frame another child leaves still
            // changes it
            int childrenKey = ChildrenKey(_owner);
            if (childrenKey != _childrenKey)
            {
                _childrenKey = childrenKey;
                Rescan();
            }

            for (int i = _patches.Count - 1; i >= 0; i--)
            {
                Patch patch = _patches[i];
                if (!patch.cell || !patch.cell.gameObject.activeInHierarchy)
                {
                    patch.zone.Clear();
                    _patches.RemoveAt(i);
                    continue;
                }

                // The cells ride along with a held creature; they light the grass again once it lands
                if (EntityHold.IsHeld(_hold))
                {
                    patch.zone.Clear();
                    continue;
                }

                patch.zone.Refresh(ZoneKind.Boost, patch.cell.transform.position, PatchRadius * _cellSize, 1f);
            }
        }

        void Rescan()
        {
            _found.Clear();
            for (int i = 0; i < _owner.childCount; i++)
            {
                BoostCell cell = _owner.GetChild(i).GetComponent<BoostCell>();
                if (cell != null && !Contains(cell))
                {
                    _found.Add(cell);
                }
            }

            foreach (BoostCell cell in _found)
            {
                foreach (Renderer renderer in cell.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }

                ZoneHandle zone = new ZoneHandle();
                zone.Init(_zones);
                _patches.Add(new Patch { cell = cell, zone = zone });
            }
        }

        static int ChildrenKey(Transform owner)
        {
            int key = owner.childCount;
            for (int i = 0; i < owner.childCount; i++)
            {
                key = key * 31 + owner.GetChild(i).GetEntityId().GetHashCode();
            }

            return key;
        }

        bool Contains(BoostCell cell)
        {
            foreach (Patch patch in _patches)
            {
                if (patch.cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        void Clear()
        {
            foreach (Patch patch in _patches)
            {
                patch.zone.Clear();
            }

            _patches.Clear();
        }

        void OnDisable()
        {
            Clear();
            _childrenKey = -1;
        }
    }
}

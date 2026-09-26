using System.Collections.Generic;
using HealerLike.Render.Grass;
using HealerLike.Render.Deliveries;
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
            public GroundHandle patch;
            public HiddenRenderers renderers;
        }

        readonly List<Patch> _patches = new List<Patch>();
        readonly List<BoostCell> _found = new List<BoostCell>();
        Transform _owner;
        Collider _hold;
        Ground _ground;
        float _cellSize = 1f;
        int _childrenKey = -1;

        public int patchCount { get { return _patches.Count; } }

        public void Init(Transform owner, Ground ground, float cellSize)
        {
            Clear();
            _owner = owner;
            _hold = EntityHold.Find(owner);
            _ground = ground;
            _cellSize = RenderMath.IsPositive(cellSize) ? cellSize : 1f;
            _childrenKey = -1;
        }

        void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled || !_owner || _ground == null)
            {
                Clear();
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
                if (!patch.cell || patch.cell.transform.parent != _owner)
                {
                    patch.patch.Release();
                    patch.renderers.Restore();
                    _patches.RemoveAt(i);
                    continue;
                }

                // The cells ride along with a held creature; they light the grass again once it lands
                if (!patch.cell.gameObject.activeInHierarchy || EntityHold.IsHeld(_hold))
                {
                    patch.patch.Hide();
                    continue;
                }

                patch.patch.Show(patch.cell.transform.position, PatchRadius * _cellSize, 1f);
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
                HiddenRenderers renderers = new HiddenRenderers();
                renderers.Capture(cell.gameObject);
                _patches.Add(new Patch
                {
                    cell = cell, patch = _ground.Hold(_ground.vocabulary.boost), renderers = renderers
                });
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
                patch.patch.Release();
                patch.renderers.Restore();
            }

            _patches.Clear();
            _childrenKey = -1;
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}

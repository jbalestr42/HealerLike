using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stage;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    // Presentation of the accepted rig: stable lost-part identity, visibility and the shadow it casts.
    public class StoneParts
    {
        static readonly uint shedSalt = 201;
        static readonly Transform[] noParts = new Transform[0];
        CreatureBuilder _builder;
        CreatureRig _rig;
        int _rigRevision;
        StoneGroundDisc _groundShadow;
        StageKeyLight _keyLight;
        StoneImpacts _impacts;
        Transform _owner;
        int _shedPart = -1;
        string _shedId;

        public int shedPart { get { return _shedPart; } }
        public IReadOnlyList<Transform> parts { get { return _rig != null ? _rig.partTransforms : noParts; } }

        public void Init(CreatureBuilder builder, Transform owner, StoneGroundDisc groundShadow,
            StageKeyLight keyLight, StoneImpacts impacts)
        {
            _builder = builder;
            _owner = owner;
            _groundShadow = groundShadow;
            _keyLight = keyLight;
            _impacts = impacts;
            _rig = null;
            _rigRevision = 0;
            _shedPart = -1;
            _shedId = null;
        }

        public void Refresh(bool isCollapsed, bool isVisible)
        {
            CreatureRig rig = _builder != null ? _builder.rig : null;
            if (rig == _rig && (rig == null || rig.revision == _rigRevision))
            {
                return;
            }

            _rig = rig;
            _rigRevision = rig != null ? rig.revision : 0;
            IReadOnlyList<Transform> partTransforms = parts;
            if (_shedId != null && rig != null)
            {
                _shedPart = -1;
                for (int i = 0; i < rig.parts.Count; i++)
                {
                    if (rig.parts[i].id == _shedId)
                    {
                        _shedPart = i;
                        break;
                    }
                }
            }
            _impacts.ReadParts(partTransforms);
            if (isCollapsed)
            {
                Hide();
                return;
            }

            // A body set up again on the same rig stands whole, a rebuilt rig keeps the limb it lost
            for (int i = 0; i < partTransforms.Count; i++)
            {
                partTransforms[i].gameObject.SetActive(i != _shedPart);
            }

            if (_groundShadow != null && partTransforms.Count > 0)
            {
                Bounds bounds = StoneGroundDisc.Measure(_owner, partTransforms);
                _groundShadow.Init(bounds, StageKeyLight.KeyDirection, _keyLight);
                _groundShadow.Show(isVisible);
            }
        }

        // One limb or accessory, picked by the seed, falls and splits
        public void Shed(StoneEffects effects, uint seed, Vector3 velocity, float ground)
        {
            if (_rig == null)
            {
                return;
            }

            IReadOnlyList<Transform> partTransforms = _rig.partTransforms;
            List<int> candidates = new List<int>();
            for (int i = 0; i < partTransforms.Count; i++)
            {
                PartRole role = _rig.parts[i].role;
                if ((role == PartRole.Limb || role == PartRole.Accessory) && partTransforms[i].gameObject.activeSelf)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            _shedPart = candidates[(int)(seed % (uint)candidates.Count)];
            _shedId = _rig.parts[_shedPart].id;
            Transform part = partTransforms[_shedPart];
            Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
            Material material = part.GetComponent<Renderer>().sharedMaterial;
            StoneEmitters.DetachedPart(effects, mesh, material, part.localToWorldMatrix, velocity,
                ground, SeededRandom.ForPart(seed, shedSalt));
            part.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (_groundShadow != null)
            {
                _groundShadow.Show(false);
            }

            foreach (Transform part in parts)
            {
                part.gameObject.SetActive(false);
            }
        }
    }
}

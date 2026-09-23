using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Deliveries
{
    // Draws the tip fragment of one delivery style, instanced once per mesh, in a frame that looks along the travel
    public class DeliveryTip
    {
        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");

        readonly List<Mesh> _meshes = new List<Mesh>();
        readonly List<List<int>> _groups = new List<List<int>>();
        Matrix4x4[][] _matrices = Array.Empty<Matrix4x4[]>();
        Vector4[][] _colours = Array.Empty<Vector4[]>();
        MaterialPropertyBlock[] _blocks = Array.Empty<MaterialPropertyBlock>();
        LookPart[] _parts = Array.Empty<LookPart>();
        LookPalette _palette;
        bool _hasStyle;

        DeliveryStyle _style;
        public DeliveryStyle style { get { return _style; } }

        public int partCount { get { return _parts.Length; } }

        public bool isSet { get { return _hasStyle; } }

        // Without a vocabulary every style keeps the one bead the arm always had
        public void SetStyle(DeliveryStyle value, DeliveryVocabulary vocabulary, PrimitiveMeshes meshes)
        {
            _style = value;
            _hasStyle = true;
            _parts = vocabulary ? vocabulary.GetTip(value) : Bead();
            _palette = vocabulary ? vocabulary.palette : null;
            _meshes.Clear();
            _groups.Clear();
            if (!meshes)
            {
                _parts = Array.Empty<LookPart>();
            }

            for (int i = 0; i < _parts.Length; i++)
            {
                Mesh mesh = meshes.GetMesh(_parts[i].primitive);
                int group = _meshes.IndexOf(mesh);
                if (group < 0)
                {
                    _meshes.Add(mesh);
                    _groups.Add(new List<int>());
                    group = _meshes.Count - 1;
                }

                _groups[group].Add(i);
            }

            _matrices = new Matrix4x4[_groups.Count][];
            _colours = new Vector4[_groups.Count][];
            _blocks = new MaterialPropertyBlock[_groups.Count];
            for (int g = 0; g < _groups.Count; g++)
            {
                _matrices[g] = new Matrix4x4[_groups[g].Count];
                _colours[g] = new Vector4[_groups[g].Count];
                _blocks[g] = new MaterialPropertyBlock();
            }
        }

        public LookPart Part(int index)
        {
            return _parts[index];
        }

        public Matrix4x4 PartMatrix(Matrix4x4 frame, int index)
        {
            LookPart part = _parts[index];
            return frame * Matrix4x4.TRS(part.position, Quaternion.Euler(part.euler), part.size);
        }

        // The accent role takes the tip colour and the stem roles the arm's, anything else asks the palette
        public Color PartColour(int index, Color tip, Color stem)
        {
            LookPart part = _parts[index];
            Color colour = stem;
            if (part.colour == ColourRole.Accent)
            {
                colour = tip;
            }
            else if (part.colour != ColourRole.Stem && part.colour != ColourRole.Limb && part.colour != ColourRole.Body
                && _palette)
            {
                colour = _palette.Colour(part.colour, EffectFamily.Damage);
            }

            return PrimitiveMeshes.Brighten(colour, part.glow);
        }

        public void Draw(Matrix4x4 frame, Material material, Color tip, Color stem, int layer)
        {
            if (_parts.Length == 0 || !material || !SystemInfo.supportsInstancing || !material.enableInstancing)
            {
                return;
            }

            for (int g = 0; g < _groups.Count; g++)
            {
                List<int> group = _groups[g];
                for (int k = 0; k < group.Count; k++)
                {
                    _matrices[g][k] = PartMatrix(frame, group[k]);
                    _colours[g][k] = PartColour(group[k], tip, stem);
                }

                _blocks[g].SetVectorArray(baseColorId, _colours[g]);
                Graphics.DrawMeshInstanced(_meshes[g], 0, material, _matrices[g], group.Count, _blocks[g],
                    ShadowCastingMode.On, true, layer);
            }
        }

        // A frame at the tip whose forward follows the travel, scaled to the tip's width
        public static Matrix4x4 Frame(Vector3 position, Vector3 travel, float width)
        {
            Quaternion rotation = Quaternion.identity;
            if (travel.sqrMagnitude > 0.000001f)
            {
                Vector3 forward = travel.normalized;
                Vector3 up = Mathf.Abs(forward.y) < 0.99f ? Vector3.up : Vector3.forward;
                rotation = Quaternion.LookRotation(forward, up);
            }

            return Matrix4x4.TRS(position, rotation, Vector3.one * width);
        }

        static LookPart[] Bead()
        {
            LookPart bead = new LookPart();
            bead.id = "Bead";
            bead.primitive = Primitive.Sphere;
            bead.role = PartRole.Tip;
            bead.colour = ColourRole.Accent;
            bead.size = Vector3.one;
            return new LookPart[] { bead };
        }
    }
}

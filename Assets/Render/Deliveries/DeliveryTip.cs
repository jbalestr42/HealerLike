using System;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Deliveries
{
    // The tip fragment of one delivery style, its parts drawn under one root in a frame that looks along the travel.
    // A tip part draws the rig's wider tip outline, so the accent separates from the arm by an edge.
    public class DeliveryTip
    {
        // A travel shorter than this has no direction
        static readonly float stillSquared = 0.000001f;

        readonly PartPaint _paint = new PartPaint();
        LookPart[] _parts = Array.Empty<LookPart>();
        Mesh[] _meshes = Array.Empty<Mesh>();
        Renderer[] _renderers = Array.Empty<Renderer>();
        LookPalette _palette;
        Transform _root;
        bool _hasStyle;
        bool _isBuilt;

        DeliveryStyle _style;
        public DeliveryStyle style { get { return _style; } }

        public int partCount { get { return _parts.Length; } }

        public bool isSet { get { return _hasStyle; } }

        // Without a vocabulary every style keeps the one bead the arm always had
        public void SetStyle(DeliveryStyle value, DeliveryVocabulary vocabulary, PrimitiveMeshes meshes)
        {
            _style = value;
            _hasStyle = true;
            _parts = Bead();
            _palette = null;
            if (vocabulary)
            {
                _parts = vocabulary.GetTip(value);
                _palette = vocabulary.palette;
            }

            if (!meshes)
            {
                _parts = Array.Empty<LookPart>();
            }

            _meshes = new Mesh[_parts.Length];
            for (int i = 0; i < _parts.Length; i++)
            {
                _meshes[i] = meshes.GetMesh(_parts[i].primitive);
            }

            ReleaseParts();
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

        // The accent role takes the delivery's tip colour and the stem role the arm's own, every other role is
        // the palette's
        public Color PartColour(int index, Color tip, Color stem)
        {
            LookPart part = _parts[index];
            Color colour = tip;
            if (part.colour == ColourRole.Stem)
            {
                colour = stem;
            }
            else if (part.colour != ColourRole.Accent)
            {
                colour = PaletteColour(part.colour);
            }

            return PrimitiveMeshes.Brighten(colour, part.glow);
        }

        // The parts are made under the parent on the first draw after a style change, then only move and recolour
        public void Draw(Transform parent, Matrix4x4 frame, Material material, Color tip, Color stem)
        {
            if (_parts.Length == 0 || !material)
            {
                return;
            }

            if (!_root)
            {
                _root = new GameObject("DeliveryTip").transform;
                _root.SetParent(parent, false);
                _root.gameObject.layer = parent.gameObject.layer;
                _isBuilt = false;
            }

            if (!_isBuilt)
            {
                Build(material);
            }

            _root.gameObject.SetActive(true);
            _root.SetPositionAndRotation(frame.GetColumn(3), frame.rotation);
            float parentScale = 1f;
            if (_root.parent)
            {
                parentScale = _root.parent.lossyScale.x;
            }

            _root.localScale = Vector3.one * (frame.lossyScale.x / parentScale);
            for (int i = 0; i < _renderers.Length; i++)
            {
                _paint.Paint(_renderers[i], _parts[i].role == PartRole.Tip, PartColour(i, tip, stem), 0f);
            }
        }

        public void Hide()
        {
            if (_root)
            {
                _root.gameObject.SetActive(false);
            }
        }

        public void Release()
        {
            if (_root)
            {
                RenderObjects.Release(_root.gameObject);
            }

            _root = null;
            _renderers = Array.Empty<Renderer>();
            _isBuilt = false;
        }

        // A frame at the tip whose forward follows the travel, scaled to the tip's width
        public static Matrix4x4 Frame(Vector3 position, Vector3 travel, float width)
        {
            Quaternion rotation = Quaternion.identity;
            if (travel.sqrMagnitude > stillSquared)
            {
                Vector3 forward = travel.normalized;
                Vector3 up = Vector3.forward;
                if (Mathf.Abs(forward.y) < 0.99f)
                {
                    up = Vector3.up;
                }

                rotation = Quaternion.LookRotation(forward, up);
            }

            return Matrix4x4.TRS(position, rotation, Vector3.one * width);
        }

        void Build(Material material)
        {
            ReleaseParts();
            _renderers = new Renderer[_parts.Length];
            for (int i = 0; i < _parts.Length; i++)
            {
                LookPart part = _parts[i];
                Transform partTransform = PrimitiveMeshes.Geometry(part.id, _root, _meshes[i], material, Color.white);
                partTransform.localPosition = part.position;
                partTransform.localRotation = Quaternion.Euler(part.euler);
                partTransform.localScale = part.size;
                partTransform.gameObject.layer = _root.gameObject.layer;
                _renderers[i] = partTransform.GetComponent<Renderer>();
            }

            _isBuilt = true;
        }

        void ReleaseParts()
        {
            foreach (Renderer renderer in _renderers)
            {
                if (renderer)
                {
                    RenderObjects.Release(renderer.gameObject);
                }
            }

            _renderers = Array.Empty<Renderer>();
            _isBuilt = false;
        }

        Color PaletteColour(ColourRole role)
        {
            if (!_palette)
            {
                Debug.LogError("[DeliveryTip] No palette.");
                return Color.magenta;
            }
            return _palette.Colour(role, EffectFamily.Damage);
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

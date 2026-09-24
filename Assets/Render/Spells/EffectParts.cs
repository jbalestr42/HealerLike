using System.Collections.Generic;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // The parts of one element, built under it from its entry: the shapes, the stalk under each shape, one bead per
    // stack, the critical rings and the side rim. The shapes and stalks move, the rest only shows or hides.
    public class EffectParts
    {
        static readonly float threadWidth = 0.012f;
        static readonly float beamWidth = 0.025f;
        // A bead runs the length of a beam in this time
        static readonly float beadSeconds = 0.6f;

        readonly List<Transform> _shapes = new List<Transform>();
        readonly List<LookPart> _shapeParts = new List<LookPart>();
        readonly List<Transform> _stalks = new List<Transform>();
        readonly List<LookPart> _stalkParts = new List<LookPart>();
        readonly List<Transform> _beads = new List<Transform>();
        readonly List<Transform> _rings = new List<Transform>();
        readonly List<Transform> _rims = new List<Transform>();
        readonly List<Transform> _all = new List<Transform>();
        MaterialPropertyBlock _block;
        EffectRecipe _recipe;
        // The side of the unit the effect sits on, its body and stem parts take that side's colours
        LookSide _targetSide;

        public List<Transform> shapes { get { return _shapes; } }
        public List<LookPart> shapeParts { get { return _shapeParts; } }
        public List<Transform> stalks { get { return _stalks; } }
        public List<Transform> rings { get { return _rings; } }
        public List<Transform> all { get { return _all; } }

        public void Build(EffectRecipe recipe, Transform root, PrimitiveMeshes meshes, Material material,
                          LookSide targetSide)
        {
            _recipe = recipe;
            _targetSide = targetSide;
            _block = new MaterialPropertyBlock();
            foreach (LookPart part in recipe.entry.parts)
            {
                Transform built = Build(part, root, meshes, material);
                if (part.role == PartRole.Stem)
                {
                    _stalks.Add(built);
                    _stalkParts.Add(part);
                }
                else
                {
                    _shapes.Add(built);
                    _shapeParts.Add(part);
                }
            }

            BuildHidden(recipe.entry.stackBeads, root, meshes, material, _beads);
            BuildHidden(recipe.entry.criticalRings, root, meshes, material, _rings);
            BuildHidden(recipe.entry.sideRim, root, meshes, material, _rims);
            if (recipe.socket == EffectSocket.Link)
            {
                SortByX(_shapes, _shapeParts);
                SortByX(_stalks, _stalkParts);
            }
        }

        // Shows the first shapes with their stalks and hides the rest, returns how many show
        public int ShowShapes(int shown)
        {
            int count = Mathf.Clamp(shown, 0, _shapes.Count);
            for (int i = 0; i < _shapes.Count; i++)
            {
                _shapes[i].gameObject.SetActive(i < count);
                if (i < _stalks.Count)
                {
                    _stalks[i].gameObject.SetActive(i < count);
                }
            }

            return count;
        }

        public void ShowStacks(int stacks)
        {
            for (int i = 0; i < _beads.Count; i++)
            {
                _beads[i].gameObject.SetActive(i < stacks);
            }
        }

        public void ShowCritical()
        {
            foreach (Transform ring in _rings)
            {
                ring.gameObject.SetActive(true);
            }
        }

        // A caster on a side draws that side's rim, a sourceless effect the neutral stone body
        public void ShowSide(Entity.EntityType side)
        {
            if (_rims.Count == 0 || _recipe == null || _recipe.palette == null)
            {
                return;
            }

            Color colour = _recipe.palette.Colour(ColourRole.Body, _recipe.family, LookSide.Stone);
            if (side != Entity.EntityType.None)
            {
                colour = _recipe.palette.Colour(ColourRole.Rim, _recipe.family, LookDerivation.Side(side));
            }

            foreach (Transform rim in _rims)
            {
                rim.gameObject.SetActive(true);
                _block.SetColor(RenderObjects.BaseColorId, colour);
                rim.GetComponent<Renderer>().SetPropertyBlock(_block);
            }
        }

        // A stalk runs from the socket's floor up to its shape
        public void PoseStalk(int index, Vector3 top, float width)
        {
            LookPart part = _stalkParts[index];
            float height = Mathf.Max(0f, top.y);
            _stalks[index].localPosition = new Vector3(top.x, height * 0.5f, top.z);
            _stalks[index].localRotation = Quaternion.identity;
            _stalks[index].localScale = new Vector3(part.size.x * width, height, part.size.z * width);
        }

        // Beads travel along segments on a curve from start to end, a contact thread keeps only the segments
        public void PoseLink(Vector3 start, Vector3 end, bool isContactThread, float age, int count)
        {
            int segments = Mathf.Max(1, _stalks.Count);
            float width = beamWidth;
            if (isContactThread)
            {
                width = threadWidth;
            }

            for (int i = 0; i < _stalks.Count; i++)
            {
                float t = (float)i / segments;
                Vector3 point = LinkPoint(start, end, isContactThread, t);
                Vector3 next = LinkPoint(start, end, isContactThread, Mathf.Min(1f, t + 1f / segments));
                _stalks[i].position = (point + next) * 0.5f;
                if (next == point)
                {
                    _stalks[i].rotation = Quaternion.identity;
                }
                else
                {
                    _stalks[i].rotation = Quaternion.FromToRotation(Vector3.up, next - point);
                }

                _stalks[i].localScale = new Vector3(width, Vector3.Distance(point, next), width);
            }

            for (int i = 0; i < _shapes.Count; i++)
            {
                float t = Mathf.Repeat((float)i / Mathf.Max(1, _shapes.Count) + age / beadSeconds, 1f);
                _shapes[i].gameObject.SetActive(!isContactThread && i < count);
                _shapes[i].position = EffectMotion.Curve(start, end, t);
            }
        }

        public void Fade(float fade)
        {
            foreach (Transform shape in _shapes)
            {
                shape.localScale *= fade;
            }

            foreach (Transform stalk in _stalks)
            {
                stalk.localScale *= fade;
            }
        }

        Color Colour(LookPart part)
        {
            // A beam's segments and beads all take the accent
            if (part.colour == ColourRole.Accent || _recipe.socket == EffectSocket.Link || _recipe.palette == null)
            {
                return _recipe.colour;
            }

            return _recipe.palette.Colour(part.colour, _recipe.family, _targetSide);
        }

        void BuildHidden(LookPart[] source, Transform root, PrimitiveMeshes meshes, Material material,
                         List<Transform> built)
        {
            foreach (LookPart part in source)
            {
                Transform partTransform = Build(part, root, meshes, material);
                partTransform.gameObject.SetActive(false);
                built.Add(partTransform);
            }
        }

        Transform Build(LookPart part, Transform root, PrimitiveMeshes meshes, Material material)
        {
            Mesh mesh = meshes.GetMesh(part.primitive, 0);
            Transform built = PrimitiveMeshes.Geometry(part.id, root, mesh, material, Colour(part), part.glow);
            built.localPosition = part.position;
            built.localRotation = Quaternion.Euler(part.euler);
            built.localScale = part.size;
            _all.Add(built);
            return built;
        }

        static Vector3 LinkPoint(Vector3 start, Vector3 end, bool isContactThread, float t)
        {
            if (isContactThread)
            {
                return EffectMotion.Thread(start, end, t);
            }

            return EffectMotion.Curve(start, end, t);
        }

        static void SortByX(List<Transform> built, List<LookPart> source)
        {
            for (int i = 1; i < source.Count; i++)
            {
                for (int j = i; j > 0 && source[j].position.x < source[j - 1].position.x; j--)
                {
                    LookPart part = source[j];
                    source[j] = source[j - 1];
                    source[j - 1] = part;
                    Transform swap = built[j];
                    built[j] = built[j - 1];
                    built[j - 1] = swap;
                }
            }
        }
    }
}

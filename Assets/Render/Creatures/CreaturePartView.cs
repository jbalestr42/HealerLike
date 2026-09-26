using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // A retained recipe part owns its geometry and complete material-block state.
    public class CreaturePartView
    {
        static readonly float pulseSwell = 0.06f;
        static readonly float chargeSwell = 0.24f;
        readonly PartPaint _paint = new PartPaint();
        CreaturePart _part;
        Renderer _renderer;
        Color _colour;
        bool _hasOchreFaces;
        Vector3 _growthAnchor;
        float _delay;
        public Transform pivot { get; private set; }
        public Transform geometry { get; private set; }

        public Renderer renderer
        {
            get { return _renderer; }
        }

        public CreaturePartView()
        {
            pivot = new GameObject("Part").transform;
        }

        public void Init(
            CreaturePart part,
            Transform parent,
            Mesh mesh,
            Material material,
            float cellSize,
            int seed,
            Color ochre
        )
        {
            _part = part;
            _colour = part.role == PartRole.Tip ? part.colour : ColourJitter.Vary(part.colour, seed);
            pivot.name = part.id;
            pivot.gameObject.SetActive(true);
            pivot.SetParent(parent, false);
            pivot.localPosition = part.localPosition * cellSize;
            pivot.localRotation = Quaternion.Euler(part.localEuler);
            if (geometry == null)
            {
                geometry = PrimitiveMeshes.Geometry("Geometry", pivot, mesh, material, _colour, part.glow);
            }

            geometry.gameObject.SetActive(true);
            geometry.GetComponent<MeshFilter>().sharedMesh = mesh;
            geometry.localPosition = Vector3.zero;
            geometry.localScale = part.dimensions * cellSize;
            _growthAnchor = mesh
                ? new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z)
                : Vector3.down * 0.5f;
            _renderer = geometry.GetComponent<Renderer>();
            _paint.Clear(_renderer);
            _hasOchreFaces = mesh && mesh.subMeshCount > 1;
            _renderer.sharedMaterials = _hasOchreFaces
                ? new Material[] { material, material }
                : new Material[] { material };
            Paint(_colour, ochre, part.glow);
        }

        public void SetAppearanceDelay(float height)
        {
            _delay = CreatureAppearance.PartDelay(_part.role, height);
        }

        public void Tick(
            float time,
            IdlePose idle,
            float cellSize,
            float pulse,
            float charge,
            float health,
            float light,
            float elapsed,
            Color wilt,
            Color ochre,
            CreatureSelection selection = default
        )
        {
            _paint.selection = selection;
            float swell = 1f + pulse * pulseSwell;
            if (_part.role == PartRole.Head || _part.role == PartRole.Tip)
            {
                swell += charge * chargeSwell;
            }

            Vector3 posedScale = Vector3.Scale(_part.dimensions, idle.bodyScale) * cellSize * swell;
            float growth = CreatureAppearance.Scale(elapsed, _delay);
            geometry.localScale = posedScale * growth;
            geometry.localPosition = Vector3.Scale(_growthAnchor, posedScale) * (1f - growth);
            if (_part.role == PartRole.Crown)
            {
                pivot.localRotation =
                    Quaternion.Euler(_part.localEuler)
                    * Quaternion.AngleAxis(time * CreatureRig.CrownSpinDegrees, Vector3.up);
            }

            Color colour;
            if (_part.role == PartRole.Tip)
            {
                float value = Mathf.Lerp(CreatureRig.WiltedTipValue, 1f, health);
                colour = new Color(_colour.r * value, _colour.g * value, _colour.b * value, _colour.a);
            }
            else
            {
                wilt.a = _colour.a;
                colour = Color.Lerp(wilt, _colour, health);
            }

            Paint(colour, ochre, _part.glow * light);
        }

        void Paint(Color colour, Color ochre, float glow)
        {
            if (_hasOchreFaces)
            {
                _paint.Paint(_renderer, _part.role == PartRole.Tip, colour, ochre, glow);
                return;
            }

            _paint.Paint(_renderer, _part.role == PartRole.Tip, colour, glow);
        }
    }
}

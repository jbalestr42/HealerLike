using System.Collections;
using HealerLike.Render.Creatures;
using HealerLike.Render.Environment;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    /// <summary>Wave 7: the runtime half of the beauty tracks' stage wiring (grid globals, tip light, launch gust,
    /// healer bind, terrain clump trample and ground colour). Everything static is placed by HLStageBuilder.</summary>
    [DefaultExecutionOrder(-1900), DisallowMultipleComponent]
    public sealed class HLStageBeautyWiring : MonoBehaviour
    {
        public const float GridStrength = .12f, TipLight = .035f, TrampleMargin = .15f;
        static readonly int GridOrigin = Shader.PropertyToID("_HLGridOrigin"), GridCell = Shader.PropertyToID("_HLGridCell"),
            GridExtent = Shader.PropertyToID("_HLGridExtent"), GridStrengthId = Shader.PropertyToID("_HLGridStrength"),
            TipLightId = Shader.PropertyToID("_HLTipLight"), BaseColor = Shader.PropertyToID("_BaseColor");
        [SerializeField] HLRenderBootstrap bootstrap;
        [SerializeField] GridManager grid;
        [SerializeField] HLEnvironmentGust gust;
        [SerializeField] Material groundMaterial;
        [SerializeField] HLCharacterView healerView;
        [SerializeField] Character healer;
        [SerializeField] HLCreatureRecipe healerRecipe;
        [SerializeField] Transform healerAnchor;
        [SerializeField] Material healerMaterial;
        public int WiredClumps { get; private set; }

        public void Configure(HLRenderBootstrap owner, GridManager board, HLEnvironmentGust environmentGust, Material ground)
        { bootstrap = owner; grid = board; gust = environmentGust; groundMaterial = ground; }
        public void ConfigureHealer(HLCharacterView view, Character character, HLCreatureRecipe recipe, Transform anchor, Material material)
        { healerView = view; healer = character; healerRecipe = recipe; healerAnchor = anchor; healerMaterial = material; }

        public static Bounds Board(Vector3 position, int width, int height, float size)
            => new Bounds(position, new Vector3(width * size, 0, height * size));
        public static void PublishGrid(Bounds board, float cell, float strength, float tipLight)
        {
            Shader.SetGlobalVector(GridOrigin, board.min);
            Shader.SetGlobalFloat(GridCell, cell);
            Shader.SetGlobalVector(GridExtent, board.size);
            Shader.SetGlobalFloat(GridStrengthId, strength);
            Shader.SetGlobalFloat(TipLightId, tipLight);
        }
        // Look beauty report: teardown zeroes both additive controls; HLLookController never overwrites them.
        public static void ClearGrid() { Shader.SetGlobalFloat(GridStrengthId, 0f); Shader.SetGlobalFloat(TipLightId, 0f); }
        public static float TrampleRadius(float footprintWorldRadius) => Mathf.Max(0, footprintWorldRadius) + TrampleMargin;
        public static HLTrampleZone AttachTrample(GameObject obstacleRoot, float footprintWorldRadius)
        {
            var trample = obstacleRoot.GetComponent<HLTrampleZone>();
            if (!trample) trample = obstacleRoot.AddComponent<HLTrampleZone>();
            trample.Radius = TrampleRadius(footprintWorldRadius);
            return trample;
        }
        // Grass beauty report: the zone sits at the footprint centre. A clump's bare disc may be offset from its pivot.
        public static HLTrampleZone WireClump(HLStoneTerrainClump clump, Material ground)
        {
            if (ground && ground.HasProperty(BaseColor) && clump.TryGetComponent<HLStoneGroundRing>(out var ring))
                ring.GroundColour = ground.GetColor(BaseColor);
            Vector3 centre = clump.BareGroundCenter, offset = centre - clump.transform.position; offset.y = 0;
            var root = clump.gameObject;
            if (offset.sqrMagnitude > .0025f)
            {
                var child = clump.transform.Find("HLTrample");
                if (!child) { child = new GameObject("HLTrample").transform; child.SetParent(clump.transform, false); }
                child.position = centre; root = child.gameObject;
            }
            return AttachTrample(root, clump.BareGroundRadius);
        }

        void OnEnable()
        {
            if (grid) PublishGrid(Board(grid.transform.position, grid.width, grid.height, grid.size), grid.size, GridStrength, TipLight);
            if (gust) HLStageLaunchGust.Target = gust;
        }
        IEnumerator Start()
        {
            if (healerView && healer)
                healerView.Bind(healer, healerRecipe, healerAnchor, healerMaterial, bootstrap ? bootstrap.Registry : HLRenderRegistry.Current, grid ? grid.size : 1);
            // Attach presentation only to already-existing cosmetic environment clumps.
            yield return null;
            WiredClumps = 0;
            foreach (var clump in FindObjectsByType<HLStoneTerrainClump>(FindObjectsSortMode.None)) { WireClump(clump, groundMaterial); WiredClumps++; }
            Debug.Log($"HL stage wiring: trample on {WiredClumps} existing cosmetic clumps; no grid generation");
        }
        void OnDisable()
        {
            ClearGrid();
            if (HLStageLaunchGust.Target == gust) HLStageLaunchGust.Target = null;
        }
    }
}

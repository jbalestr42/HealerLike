using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    // Seeded ring of stones and plants around the grid, never inside it, built once
    public class EnvironmentScatter : AEnvironmentSpawner
    {
        // Counts are (fewest, extra up to): a cairn stacks two to four stones
        static readonly Vector2Int cairnLayers = new Vector2Int(2, 3);
        // Cairn: each stone smaller than the one below, off centre and resting partly inside it
        static readonly float cairnShrink = 0.2f;
        static readonly float cairnOffset = 0.08f;
        static readonly float cairnRest = 0.8f;
        // Stones sink this share of their height into the ground so they read as rooted
        static readonly float stoneSink = 0.12f;
        // Mushroom tree: a tall stem, a flat underside and a cone or dome cap nodding on top
        static readonly Vector2 mushroomStemHeight = new Vector2(2.2f, 4.2f);
        static readonly float mushroomStemWidth = 0.22f;
        static readonly float mushroomCapHeight = 0.92f;
        static readonly Vector3 mushroomUnderScale = new Vector3(1.5f, 0.14f, 1.5f);
        static readonly float mushroomUnderDrop = 0.08f;
        static readonly float mushroomConeChance = 0.4f;
        static readonly Vector3 mushroomConeScale = new Vector3(1.4f, 0.7f, 1.4f);
        static readonly Vector3 mushroomDomeScale = new Vector3(1.6f, 0.45f, 1.6f);
        static readonly Color mushroomStem = new Color(0.65f, 0.82f, 0.62f);
        static readonly Color mushroomCapTeal = new Color(0.44f, 0.74f, 0.61f);
        static readonly Color mushroomCapPale = new Color(0.64f, 0.78f, 0.65f);
        static readonly float mushroomSwayPerStem = 0.45f;
        static readonly Vector2 mushroomSwayRange = new Vector2(0.6f, 3f);
        static readonly float mushroomNodDegrees = 1.4f;
        // Spiral fern: fronds of joints that curl tighter and shrink toward the tip
        static readonly Vector2Int fernFronds = new Vector2Int(3, 3);
        static readonly float fernFrondSpread = 15f;
        static readonly int fernJoints = 9;
        static readonly float fernJointLength = 0.3f;
        static readonly float fernJointShrink = 0.88f;
        static readonly Vector2 fernRootCurl = new Vector2(10f, 25f);
        static readonly float fernCurl = 22f;
        static readonly float fernCurlStep = 5f;
        // Joint size over its length
        static readonly Vector3 fernJointShape = new Vector3(0.9f, 1.15f, 0.9f);
        static readonly float fernJointSway = 0.12f;
        // A positive correction opens each negative curl angle during a gust
        static readonly float fernUncurl = -3f;
        static readonly float fernSway = 1.6f;
        // Rosette: solid leaves about six times as long as they are wide at the base, a third as thick
        static readonly Vector2Int rosetteLeaves = new Vector2Int(7, 5);
        static readonly float rosetteSpread = 10f;
        static readonly Vector2 rosetteTilt = new Vector2(15f, 45f);
        static readonly Vector2 rosetteLength = new Vector2(0.8f, 1.6f);
        static readonly float rosetteWidth = 0.16f;
        static readonly float rosetteThickness = 0.05f;
        static readonly float rosetteSway = 0.45f;
        // Sphere cluster: thin stems, each ending in a ball that sits a little over its tip
        static readonly Vector2Int clusterStems = new Vector2Int(3, 3);
        static readonly float clusterSpread = 20f;
        static readonly float clusterMaxTilt = 15f;
        static readonly Vector2 clusterStemHeight = new Vector2(0.6f, 1.6f);
        static readonly Vector2 clusterBallSize = new Vector2(0.3f, 0.55f);
        static readonly float clusterStemWidth = 0.05f;
        static readonly float clusterBallOverlap = 0.2f;
        static readonly float clusterSway = 1.2f;

        [SerializeField] LookPalette _palette;
        [SerializeField] EnvironmentSettings _settings = EnvironmentSettings.Default;
        public EnvironmentSettings settings { get { return _settings; } set { _settings = value; } }

        uint _colourSeed;
        EnvironmentSway _sway;

        List<EnvironmentItem> _items = new List<EnvironmentItem>();
        public IReadOnlyList<EnvironmentItem> items { get { return _items; } }

        public void Init(Rect board, float cellSize, float surfaceY, Camera viewCamera, EnvironmentGust gust,
                         float fogEnd, PrimitiveMeshes meshes)
        {
            if (meshes == null)
            {
                Debug.LogError("[EnvironmentScatter] Init needs the primitive meshes.");
                return;
            }

            _meshes = meshes;
            _items = EnvironmentLayout.Generate(_settings, board, cellSize, surfaceY);
            Transform itemsRoot = CreateRoot("EnvironmentItems");
            _sway = itemsRoot.gameObject.AddComponent<EnvironmentSway>();
            _sway.Init(viewCamera, gust, fogEnd, Time.timeAsDouble);
            foreach (EnvironmentItem item in _items)
            {
                Spawn(item);
            }
        }

        void Spawn(EnvironmentItem item)
        {
            Transform pivot = new GameObject(item.kind.ToString()).transform;
            pivot.SetParent(root, false);
            pivot.position = item.position;
            pivot.rotation = Quaternion.Euler(0f, item.yaw, 0f);
            SeededRandom random = new SeededRandom(item.seed);
            float s = item.scale;
            _colourSeed = item.seed;
            switch (item.kind)
            {
                case EnvironmentKind.Boulder:
                    Stone(pivot, item.seed, StonePresets.Boulder, Vector3.zero, s, item.paletteIndex);
                    break;
                case EnvironmentKind.Monolith:
                    int palette = item.paletteIndex % 2 == 0 ? 1 : 2;
                    Stone(pivot, item.seed, StonePresets.Monolith, Vector3.zero, s, palette);
                    break;
                case EnvironmentKind.Cairn:
                    SpawnCairn(pivot, item, random, s);
                    break;
                case EnvironmentKind.MushroomTree:
                    SpawnMushroomTree(pivot, item, random, s);
                    break;
                case EnvironmentKind.SpiralFern:
                    SpawnSpiralFern(pivot, item, random, s);
                    break;
                case EnvironmentKind.BladeRosette:
                    SpawnBladeRosette(pivot, item, random, s);
                    break;
                case EnvironmentKind.SphereCluster:
                    SpawnSphereCluster(pivot, item, random, s);
                    break;
            }
        }

        // Two to four stones shrinking upward, each resting on the one below
        void SpawnCairn(Transform pivot, EnvironmentItem item, SeededRandom random, float s)
        {
            int layers = cairnLayers.x + (int)(random.Next01() * cairnLayers.y);
            float y = 0f;
            for (int i = 0; i < layers; i++)
            {
                float k = s * (1f - i * cairnShrink);
                float x = random.Range(-cairnOffset, cairnOffset) * s;
                float z = random.Range(-cairnOffset, cairnOffset) * s;
                uint seed = SeededRandom.ForPart(item.seed, (uint)i + 1);
                Vector3 part = Stone(pivot, seed, StonePresets.Cairn, new Vector3(x, y, z), k, item.paletteIndex + i);
                y += part.y * cairnRest;
            }
        }

        void SpawnMushroomTree(Transform pivot, EnvironmentItem item, SeededRandom random, float s)
        {
            float stem = random.Range(mushroomStemHeight.x, mushroomStemHeight.y) * s;
            Vector3 stemScale = new Vector3(mushroomStemWidth * s, stem, mushroomStemWidth * s);
            PlantPart(pivot, _meshes.capsule, Vector3.zero, Quaternion.identity, stemScale, mushroomStem);

            Transform cap = new GameObject("NoddingCap").transform;
            cap.SetParent(pivot, false);
            cap.localPosition = Vector3.up * (stem * mushroomCapHeight);
            Vector3 underBottom = Vector3.down * mushroomUnderDrop * s;
            Color under = Colour(ColourRole.Stem);
            PlantPart(cap, _meshes.sphere, underBottom, Quaternion.identity, mushroomUnderScale * s, under);

            bool isCone = random.Next01() < mushroomConeChance;
            Mesh top = isCone ? _meshes.cone : _meshes.sphere;
            Vector3 topScale = isCone ? mushroomConeScale * s : mushroomDomeScale * s;
            Color topColour = random.Next01() < 0.5f ? mushroomCapTeal : mushroomCapPale;
            PlantPart(cap, top, Vector3.zero, Quaternion.identity, topScale, topColour);

            float sway = Mathf.Clamp(stem * mushroomSwayPerStem, mushroomSwayRange.x, mushroomSwayRange.y);
            _sway.Add(pivot, pivot, item.seed, sway, 0f);
            _sway.Add(cap, pivot, item.seed + 1, mushroomNodDegrees, 0f);
        }

        void SpawnSpiralFern(Transform pivot, EnvironmentItem item, SeededRandom random, float s)
        {
            int fronds = fernFronds.x + (int)(random.Next01() * fernFronds.y);
            for (int f = 0; f < fronds; f++)
            {
                Transform frond = new GameObject("Frond").transform;
                frond.SetParent(pivot, false);
                float around = f * 360f / fronds + random.Range(-fernFrondSpread, fernFrondSpread);
                frond.localRotation = Quaternion.Euler(0f, around, 0f);
                Transform linkParent = frond;
                float length = fernJointLength * s;
                for (int i = 0; i < fernJoints; i++)
                {
                    Transform joint = new GameObject("CurlJoint").transform;
                    joint.SetParent(linkParent, false);
                    joint.localPosition = i == 0 ? Vector3.zero : Vector3.up * length / fernJointShrink;
                    float curl = i == 0 ? random.Range(fernRootCurl.x, fernRootCurl.y) : fernCurl + i * fernCurlStep;
                    joint.localRotation = Quaternion.Euler(0f, 0f, -curl);
                    Vector3 scale = fernJointShape * length;
                    Color colour = Color.Lerp(Colour(ColourRole.Stem), Colour(ColourRole.Body), i / (fernJoints - 1f));
                    PlantPart(joint, _meshes.sphere, Vector3.zero, Quaternion.identity, scale, colour);
                    uint seed = item.seed + (uint)(f * 16 + i);
                    _sway.Add(joint, pivot, seed, fernJointSway * s, fernUncurl);
                    linkParent = joint;
                    length *= fernJointShrink;
                }
            }

            _sway.Add(pivot, pivot, item.seed, fernSway * s, 0f);
        }

        void SpawnBladeRosette(Transform pivot, EnvironmentItem item, SeededRandom random, float s)
        {
            int leaves = rosetteLeaves.x + (int)(random.Next01() * rosetteLeaves.y);
            for (int i = 0; i < leaves; i++)
            {
                float around = i * 360f / leaves + random.Range(-rosetteSpread, rosetteSpread);
                Quaternion rotation = Quaternion.Euler(0f, around, random.Range(rosetteTilt.x, rosetteTilt.y));
                float length = random.Range(rosetteLength.x, rosetteLength.y) * s;
                Vector3 scale = new Vector3(length * rosetteWidth, length, length * rosetteThickness);
                Color colour = Color.Lerp(Colour(ColourRole.Stem), Colour(ColourRole.Body), random.Next01());
                PlantPart(pivot, _meshes.leaf, Vector3.zero, rotation, scale, colour);
            }

            _sway.Add(pivot, pivot, item.seed, rosetteSway * s, 0f);
        }

        void SpawnSphereCluster(Transform pivot, EnvironmentItem item, SeededRandom random, float s)
        {
            int stems = clusterStems.x + (int)(random.Next01() * clusterStems.y);
            for (int i = 0; i < stems; i++)
            {
                float around = i * 360f / stems + random.Range(-clusterSpread, clusterSpread);
                Quaternion tilt = Quaternion.Euler(0f, around, random.Range(0f, clusterMaxTilt));
                float h = random.Range(clusterStemHeight.x, clusterStemHeight.y) * s;
                float d = random.Range(clusterBallSize.x, clusterBallSize.y) * s;
                Vector3 stemScale = new Vector3(clusterStemWidth * s, h, clusterStemWidth * s);
                PlantPart(pivot, _meshes.cylinder, Vector3.zero, tilt, stemScale, Colour(ColourRole.Stem));
                Vector3 bottom = tilt * Vector3.up * h - Vector3.up * (d * clusterBallOverlap);
                PlantPart(pivot, _meshes.sphere, bottom, Quaternion.identity, Vector3.one * d, Colour(ColourRole.Body));
            }

            _sway.Add(pivot, pivot, item.seed, clusterSway * s, 0f);
        }

        void PlantPart(Transform parent, Mesh mesh, Vector3 bottom, Quaternion rotation, Vector3 scale, Color colour)
        {
            Part(parent, mesh, _plantMaterial, bottom, rotation, scale, ColourJitter.VaryScenery(colour, _colourSeed), mesh.name);
        }

        // Scatter plants are plants, scatter rocks stones; each part varies its colour from the role's
        Color Colour(ColourRole role)
        {
            return Colour(role, LookSide.Plant);
        }

        Color Colour(ColourRole role, LookSide side)
        {
            if (_palette == null)
            {
                Debug.LogError("[EnvironmentScatter] No palette.");
                return Color.magenta;
            }
            return _palette.Colour(role, EffectFamily.Damage, side);
        }

        // A rock's palette index, wrapped to four, picks its body, its limb or its ochre
        Color StoneColour(int index)
        {
            int wrapped = index & 3;
            if (wrapped == 0)
            {
                return Colour(ColourRole.Body, LookSide.Stone);
            }

            if (wrapped == 3)
            {
                return Colour(ColourRole.Ochre, LookSide.Stone);
            }
            return Colour(ColourRole.Limb, LookSide.Stone);
        }

        // Returns the stone's world size
        Vector3 Stone(Transform parent, uint seed, StoneSettings shape, Vector3 bottom, float scale, int palette)
        {
            Mesh mesh = CreateStone(seed, shape, "EnvironmentStone");
            Vector3 sunkBottom = bottom - Vector3.up * (mesh.bounds.size.y * scale * stoneSink);
            Color colour = ColourJitter.VaryScenery(StoneColour(palette), _colourSeed);
            Vector3 size = Vector3.one * scale;
            Part(parent, mesh, _stoneMaterial, sunkBottom, Quaternion.identity, size, colour, "Stone");
            return Vector3.Scale(mesh.bounds.size, size);
        }
    }
}

using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Studio.Editor
{
    // The private scene a studio preview renders: two directional lights, copies of the three look materials,
    // and a ground disc under a half-metre grid drawn as geometry, so no shader global reaches the open scene
    public class StudioPreviewScene
    {
        static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
        static readonly string defaultMaterialPath = "Assets/Render/Look/Look_Default.mat";
        static readonly string bodyMaterialPath = "Assets/Render/Look/Look_Body.mat";
        static readonly string stoneMaterialPath = "Assets/Render/Look/Look_Stone.mat";
        static readonly Color groundColour = new Color(0.15f, 0.19f, 0.22f);
        static readonly Color axisColour = new Color(0.32f, 0.4f, 0.45f);
        static readonly Color gridColour = new Color(0.22f, 0.28f, 0.31f);

        PreviewRenderUtility _utility;
        PrimitiveMeshes _meshes;
        Material _material;
        Material _bodyMaterial;
        Material _stoneMaterial;
        GameObject _root;
        GameObject _ground;
        string _error;

        public PreviewRenderUtility utility { get { return _utility; } }

        public PrimitiveMeshes meshes { get { return _meshes; } }

        public Material material { get { return _material; } }

        public Material bodyMaterial { get { return _bodyMaterial; } }

        public Material stoneMaterial { get { return _stoneMaterial; } }

        public GameObject root { get { return _root; } }

        // Set when a project asset the scene needs is missing; the scene then has lights and nothing else
        public string error { get { return _error; } }

        public bool isStarted { get { return _utility != null; } }

        // The title names the root and the material copies: "Spell Studio" gives "Spell Studio Preview Material"
        public void Init(string title)
        {
            _utility = new PreviewRenderUtility();
            foreach (Light light in _utility.lights)
            {
                light.enabled = true;
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
            }

            _utility.lights[0].intensity = 1.25f;
            // The creature preview starts at yaw 30. Keep the stage sun's shoulder relationship at that
            // angle; the preview remains an isolated, orbitable scene without the stage shadow atlas.
            Vector3 key = Quaternion.Euler(0f, 30f - StageCalibration.PortraitYaw, 0f) * StageKeyLight.KeyDirection;
            _utility.lights[0].transform.rotation = Quaternion.LookRotation(-key.normalized, Vector3.up);
            _utility.lights[1].intensity = 0.55f;
            _utility.lights[1].transform.rotation = Quaternion.Euler(320f, 145f, 0f);
            _utility.ambientColor = new Color(0.35f, 0.39f, 0.45f);

            _meshes = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath);
            Material source = AssetDatabase.LoadAssetAtPath<Material>(defaultMaterialPath);
            Material bodySource = AssetDatabase.LoadAssetAtPath<Material>(bodyMaterialPath);
            Material stoneSource = AssetDatabase.LoadAssetAtPath<Material>(stoneMaterialPath);
            if (!_meshes || !source || !bodySource || !stoneSource)
            {
                _error = "Preview requires PrimitiveMeshes.asset and the Look_Default, Look_Body and Look_Stone "
                    + "materials from Assets/Render.";
                return;
            }

            _material = Copy(source, title + " Preview Material");
            _bodyMaterial = Copy(bodySource, title + " Body Material");
            _stoneMaterial = Copy(stoneSource, title + " Stone Material");
            _root = new GameObject(title + " Preview");
            _root.hideFlags = HideFlags.HideAndDontSave;
            _utility.AddSingleGO(_root);
            _ground = AddChild("Preview Ground");
            BuildGround();
        }

        public GameObject AddChild(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_root.transform, false);
            return child;
        }

        // A stone target draws every part with the stone material; plants give body and head surfaces their own look.
        public Material Shared(LookSide side)
        {
            if (side == LookSide.Stone)
            {
                return _stoneMaterial;
            }
            return _material;
        }

        public Material Body(LookSide side)
        {
            if (side == LookSide.Stone)
            {
                return _stoneMaterial;
            }
            return _bodyMaterial;
        }

        public void ShowGround(bool isShown)
        {
            if (_ground)
            {
                _ground.SetActive(isShown);
            }
        }

        public void Dispose()
        {
            if (_utility != null)
            {
                _utility.Cleanup();
            }

            _utility = null;
            if (_material)
            {
                Object.DestroyImmediate(_material);
            }

            if (_bodyMaterial)
            {
                Object.DestroyImmediate(_bodyMaterial);
            }

            if (_stoneMaterial)
            {
                Object.DestroyImmediate(_stoneMaterial);
            }
            _root = null;
        }

        // Neither saved with a scene nor listed in the hierarchy
        public static void HideTree(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void BuildGround()
        {
            if (_meshes.disc)
            {
                Transform disc = PrimitiveMeshes.Geometry("Ground", _ground.transform, _meshes.disc, _material,
                    groundColour);
                disc.localScale = Vector3.one * 8f;
                disc.localPosition = Vector3.down * 0.012f;
            }

            for (int i = -8; i <= 8; i++)
            {
                Color colour = gridColour;
                if (i == 0)
                {
                    colour = axisColour;
                }

                float offset = i * 0.5f;
                Transform x = PrimitiveMeshes.Geometry("Grid X", _ground.transform, _meshes.cylinder, _material,
                    colour);
                PrimitiveMeshes.Segment(x, new Vector3(-4f, 0f, offset), new Vector3(4f, 0f, offset), 0.002f);
                Transform z = PrimitiveMeshes.Geometry("Grid Z", _ground.transform, _meshes.cylinder, _material,
                    colour);
                PrimitiveMeshes.Segment(z, new Vector3(offset, 0f, -4f), new Vector3(offset, 0f, 4f), 0.002f);
            }
        }

        static Material Copy(Material source, string name)
        {
            Material copy = new Material(source);
            copy.name = name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            return copy;
        }
    }
}

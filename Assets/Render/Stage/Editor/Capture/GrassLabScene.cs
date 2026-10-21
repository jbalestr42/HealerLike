using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grass;
using HealerLike.Render.Look;
using HealerLike.Render.Zones;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Owns the isolated lab fixtures and restores the stage publications only after they are released.
    public class GrassLabScene : IDisposable
    {
        public static readonly Rect Area = new Rect(-4f, -4f, 8f, 8f);
        static readonly int labLayer = 30;
        static readonly string pipelinePath = "Assets/Settings/Very High_PipelineAsset.asset";
        static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";
        readonly RenderManager _manager;
        readonly bool _managerEnabled;
        readonly GroundCaptureState _state;
        readonly Texture _motion;
        readonly Texture _crush;
        readonly Texture _groundState;
        readonly Vector4 _groundRect;
        readonly float _groundActive;
        readonly GraphicsBuffer _zoneBuffer;
        readonly int _zoneCount;
        readonly List<CreatureBody> _creatures = new List<CreatureBody>();
        bool _isDisposed;

        public Camera camera { get; private set; }
        public LookController look { get; private set; }
        public ZoneRegistry registry { get; private set; }
        public Ground ground { get; private set; }
        public GrassField field { get; private set; }
        public IReadOnlyList<CreatureBody> creatures { get { return _creatures; } }

        // A cosmetic creature and the body it presses into the grass.
        public class CreatureBody : IGroundBody
        {
            public Transform anchor;
            public readonly CreaturePreview preview = new CreaturePreview();
            readonly BodyMeshes _meshes = new BodyMeshes();

            public void Refresh()
            {
                _meshes.Refresh(preview.rig);
            }

            public int AppendCapsules(BodyCapsule[] into, int start)
            {
                return _meshes.Append(into, start, anchor.position.y + TrampleZone.BodyReach, TrampleZone.MaxCapsules);
            }
        }

        public GrassLabScene(RenderManager manager)
        {
            _manager = manager;
            _managerEnabled = manager.enabled;
            _motion = Shader.GetGlobalTexture(GroundSimulation.MotionId);
            _crush = Shader.GetGlobalTexture(GroundSimulation.CrushId);
            _groundState = Shader.GetGlobalTexture(GroundSimulation.StateId);
            _groundRect = Shader.GetGlobalVector(GroundSimulation.RectId);
            _groundActive = Shader.GetGlobalFloat(GroundSimulation.ActiveId);
            _zoneBuffer = manager.zones != null ? manager.zones.buffer : null;
            _zoneCount = Shader.GetGlobalInt("_HLZoneCount");
            _state = new GroundCaptureState(Object.FindObjectsByType<LookController>());
            manager.enabled = false;
        }

        public bool Init()
        {
            QualitySettings.renderPipeline = RenderAssets.Load<RenderPipelineAsset>(pipelinePath);
            camera = CreateCamera();
            look = Fixture("GrassLabLook").AddComponent<LookController>();
            LookSettings settings = LookSettings.Default;
            settings.fogStart = 25f;
            settings.fogEnd = 60f;
            look.settings = settings;
            registry = Fixture("GrassLabZones").AddComponent<ZoneRegistry>();
            registry.Init();
            ground = new Ground();
            CreateGround();
            field = Fixture("GrassLabGrass").AddComponent<GrassField>();
            EnvironmentAuthoring.SetGrass(field);
            EnvironmentAuthoring.SetGround(field);
            field.Init(Area, 1f, 0f, camera, registry.buffer, ZonePacker.MaxZones);
            bool complete = Creature("NormalEntity", Entity.EntityType.Player, new Vector3(-3.2f, 0f, 0.5f));
            complete &= Creature("SoldierEntity", Entity.EntityType.Computer, new Vector3(2.2f, 0f, 2.2f));
            complete &= Creature("NormalEntity", Entity.EntityType.Player, new Vector3(-1.5f, 0f, -1.5f));
            complete &= Creature("NormalEntity", Entity.EntityType.Player, new Vector3(1.4f, 0f, -2.2f));
            if (complete)
            {
                _creatures[3].anchor.gameObject.SetActive(false);
            }
            return complete;
        }

        public GameObject Fixture(string name)
        {
            GameObject fixture = new GameObject(name);
            fixture.layer = labLayer;
            _state.created.Add(fixture);
            return fixture;
        }

        Camera CreateCamera()
        {
            Camera result = Fixture("GrassLabCamera").AddComponent<Camera>();
            result.cullingMask = 1 << labLayer;
            result.fieldOfView = 40f;
            result.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            result.transform.position = -result.transform.forward * 11.5f + Vector3.back * 0.6f;
            result.clearFlags = CameraClearFlags.SolidColor;
            result.backgroundColor = new Color(0.74f, 0.82f, 0.83f);
            result.enabled = false;
            Light light = Fixture("GrassLabKey").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = StageKeyLight.Aim(StageKeyLight.KeyDirection);
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            return result;
        }

        void CreateGround()
        {
            Material material = new Material(RenderAssets.Load<Shader>(lookShaderPath));
            _state.created.Add(material);
            material.SetColor(RenderObjects.BaseColorId, ((Color)new Color32(78, 126, 87, 255)).linear);
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _state.created.Add(surface);
            surface.layer = labLayer;
            surface.transform.localScale = new Vector3(8f, 0.2f, 8f);
            surface.transform.position = Vector3.down * 0.1f;
            surface.GetComponent<Renderer>().sharedMaterial = material;
        }

        bool Creature(string entity, Entity.EntityType side, Vector3 position)
        {
            GameObject anchor = Fixture("GrassLab" + entity);
            anchor.transform.position = position;
            CreatureBody creature = new CreatureBody { anchor = anchor.transform };
            _creatures.Add(creature);
            EntityData data = LookSheetData.LoadEntity(entity);
            if (!creature.preview.Init(_manager.creatureLooks, data, side, _manager.meshes, anchor.transform, 1f))
            {
                return false;
            }

            creature.preview.CompleteAppearance();
            creature.preview.Tick(0f, 0f, new FootFrame(position, Vector3.up, 1f), Vector3.back);
            foreach (Transform child in anchor.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = labLayer;
            }
            creature.Refresh();
            ground.AddBody(creature);
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                if (field != null)
                {
                    field.Release();
                }
                if (registry != null)
                {
                    registry.Release();
                }
                foreach (CreatureBody creature in _creatures)
                {
                    creature.preview.Dispose();
                }
                _creatures.Clear();
                if (ground != null)
                {
                    ground.Dispose();
                }
            }
            finally
            {
                _state.Dispose();
                Shader.SetGlobalTexture(GroundSimulation.MotionId, _motion);
                Shader.SetGlobalTexture(GroundSimulation.CrushId, _crush);
                Shader.SetGlobalTexture(GroundSimulation.StateId, _groundState);
                Shader.SetGlobalVector(GroundSimulation.RectId, _groundRect);
                Shader.SetGlobalFloat(GroundSimulation.ActiveId, _groundActive);
                bool hasZones = _zoneBuffer != null && _zoneBuffer.IsValid();
                Shader.SetGlobalBuffer("_HLZones", hasZones ? _zoneBuffer : null);
                Shader.SetGlobalInt("_HLZoneCount", hasZones ? _zoneCount : 0);
                if (_manager != null)
                {
                    _manager.enabled = _managerEnabled;
                }
            }
        }
    }
}

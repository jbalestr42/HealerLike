using System;
using System.Collections;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    [DefaultExecutionOrder(-2000), DisallowMultipleComponent]
    public sealed class HLRenderBootstrap : MonoBehaviour
    {
        [SerializeField] Behaviour lookController;
        [SerializeField] Behaviour zoneRegistry;
        [SerializeField] Behaviour zoneBridge;
        [SerializeField] Behaviour grassField;
        [SerializeField] MonoBehaviour spellVisualSink;
        [SerializeField] GridManager grid;
        [SerializeField] Transform ground;
        [Tooltip("The healer's resolved-heal pulse. A Character is not an Entity, so no EntityModel walk initializes it.")]
        [SerializeField] HLHealPulse healPulse;
        [SerializeField] GameObject healSource;
        public enum Framing { Portrait, Landscape }
        [Tooltip("Portrait is Julien's device orientation; landscape keeps the wave-3 50 degree framing. Look calibration is for portrait.")]
        [SerializeField] Framing framing = Framing.Portrait;
        [SerializeField] Camera stageCamera;
        [SerializeField] Pose portraitPose = new Pose(Vector3.zero, Quaternion.identity);
        [SerializeField] Pose landscapePose = new Pose(Vector3.zero, Quaternion.identity);
        HLRenderRegistry registry;
        bool owns;
        public HLRenderRegistry Registry => registry;
        public Pose OverviewPose => framing==Framing.Portrait ? portraitPose : landscapePose;
        public Camera StageCamera => stageCamera;
        public Framing CameraFraming { get => framing; set { framing = value; ApplyFraming(); } }
        public void ConfigureFraming(Camera camera, Pose portrait, Pose landscape)
        {
            stageCamera = camera; portraitPose = portrait; landscapePose = landscape; ApplyFraming();
        }
        public void ApplyFraming()
        {
            if (!stageCamera) return;
            var pose = framing == Framing.Portrait ? portraitPose : landscapePose;
            stageCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            stageCamera.aspect=framing==Framing.Portrait ? 9f/16f : 16f/9f;
            UpdateCameraFog(pose.position);
            if(Application.isPlaying)
                foreach(var foreground in GetComponentsInChildren<HealerLike.Render.Environment.HLEnvironmentForeground>()) foreground.Build();
        }

        public void UpdateCameraFog(Vector3 position)
        {
            if(grid && lookController is HealerLike.Render.Look.HLLookController look)
            {
                var fog=HLStageCalibration.BackgroundFog(position, new Bounds(grid.transform.position,new Vector3(grid.width*grid.size,0,grid.height*grid.size)));
                var settings=look.Settings; settings.FogStart=fog.x; settings.FogEnd=fog.y; look.Settings=settings; look.ApplyGlobals();
            }
        }

        // Explicit injection keeps ownership testable without a scene or gameplay singleton.
        public void Configure(IHLSpellVisualSink sink, Behaviour look, Behaviour zones)
        {
            if (owns) throw new InvalidOperationException("Disable bootstrap before rewiring.");
            registry = new HLRenderRegistry { SpellSink = sink, ZoneOwner = zones as IHLZoneOwner };
            spellVisualSink = sink as MonoBehaviour;
            lookController = look;
            zoneRegistry = zones;
        }
        void OnEnable()
        {
            if (owns) return;
            ApplyFraming();
            if (HLRenderRegistry.Current != null)
            {
                Debug.LogWarning("HL stage registry already has an owner.", this);
                return;
            }
            if (registry == null) registry = new HLRenderRegistry { SpellSink = spellVisualSink as IHLSpellVisualSink, ZoneOwner = zoneRegistry as IHLZoneOwner };
            HLRenderRegistry.Current = registry;
            owns = true;
            if (zoneRegistry) zoneRegistry.enabled = true;
            // Explicit stage adapter; the sink would otherwise reach the same owner through the registry.
            if (spellVisualSink is HLSpellVisualSink sink && zoneRegistry is HLZoneRegistry zones)
                sink.areaPulse = (center, radius, kind, strength) => zones.AddPulse(kind, center, radius, strength, HLSpellVisualSink.PulseSeconds);
            if (spellVisualSink) spellVisualSink.enabled = true;
            if (healPulse) healPulse.Initialize(healSource);
            if (lookController) lookController.enabled = true;
            if (zoneBridge) zoneBridge.enabled = true;
            if (grassField) grassField.enabled = true;
        }
        void OnDisable()
        {
            if (!owns) return;
            if (spellVisualSink is HLSpellVisualSink sink) sink.areaPulse = null;
            if (healPulse) healPulse.Initialize(null);
            if (spellVisualSink) spellVisualSink.enabled = false;
            if (zoneBridge) zoneBridge.enabled = false;
            if (grassField) grassField.enabled = false;
            if (lookController) lookController.enabled = false;
            if (zoneRegistry) zoneRegistry.enabled = false;
            if (ReferenceEquals(HLRenderRegistry.Current, registry)) HLRenderRegistry.Current = null;
            owns = false;
        }
    }
}

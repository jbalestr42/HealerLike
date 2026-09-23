using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HealerLike.Render.Creatures;
using HealerLike.Render.Environment;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public static class HLStageBuilder
    {
        public const string Root = "Assets/Render/Stage/";
        public const string ScenePath = Root + "HLRenderLook.unity";
        static readonly List<string> todo = new List<string>();
        static readonly Dictionary<string,string> copies = new Dictionary<string,string>();
        static Material green, stone;
        public const string LookDefault = "Assets/Render/Look/HLLook_Default.mat";
        public const string LookStone = "Assets/Render/Look/HLLook_Stone.mat";
        // Placeholder materials shipped by the tracks and wave 2, mapped to the look material that replaces them.
        static readonly (string placeholder, string look)[] placeholderMaterials =
        {
            ("Assets/Render/Spells/Data/HLSpellPlaceholder.mat", LookDefault),
            ("Assets/Render/Creatures/Data/HLPlaceholder.mat", LookDefault),
            ("Assets/Render/Stones/HLPlaceholderStone.mat", LookStone),
            (Root + "Materials/HLAllyPlaceholder.mat", LookDefault),
            (Root + "Materials/HLStonePlaceholder.mat", LookStone),
        };
        public static Type Resolve(string name) => TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().FirstOrDefault(t => t.Name == name);

        [MenuItem("HL/Stage/Build isolated gameplay stage")]
        public static void Build()
        {
            todo.Clear(); copies.Clear();
            Directory.CreateDirectory(Root + "Data"); Directory.CreateDirectory(Root + "Prefabs");
            AssetDatabase.Refresh();
            green = AssetDatabase.LoadAssetAtPath<Material>(LookDefault);
            stone = StoneMaterial();
            if (!green || green.shader.name != "HL/Look/Primitive") throw new InvalidOperationException("HLLook_Default.mat must use HL/Look/Primitive.");
            SwapPlaceholderMaterials();
            // Copy the complete small data catalog so random choices cannot escape into source data.
            foreach (string path in Directory.GetFiles("Assets/Data", "*.asset", SearchOption.AllDirectories)) Copy(path, "Data/" + path.Substring("Assets/Data/".Length));
            foreach (string path in Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories)) Copy(path, "Prefabs/" + path.Substring("Assets/Prefabs/".Length));
            int areaIndex = 0;
            foreach (string guid in new[] { "12ad1a3a6d09f3547a6309e670bc74fe", "5ac17d7d6c2d7de4b8a657becba43743", "01853b17441166446a7cdcca5640f029" }) Copy(AssetDatabase.GUIDToAssetPath(guid), "Prefabs/Area/Area" + (++areaIndex) + ".prefab");
            Copy("Assets/Scenes/Main.unity", "HLRenderLook.unity", false);
            AssetDatabase.Refresh();
            var guidMap = copies.ToDictionary(k => AssetDatabase.AssetPathToGUID(k.Key), v => AssetDatabase.AssetPathToGUID(v.Value));
            foreach (string path in copies.Values)
            {
                string yaml = File.ReadAllText(path);
                foreach (var pair in guidMap) yaml = yaml.Replace("guid: " + pair.Key, "guid: " + pair.Value);
                File.WriteAllText(path, yaml);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Models();
            WireRestartPrefab();
            foreach (var pair in copies.Where(p => p.Key.Contains("/Projectiles/") || p.Value.Contains("/Area/")))
                VisualVariant(pair.Key, pair.Value);
            AssetDatabase.SaveAssets();
            RepairVariantReferences();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var root in scene.GetRootGameObjects())
                if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Cinemachine otherwise overwrites the authored framing on the first play frame.
            foreach (var b in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include))
                if ((b.GetType().Namespace ?? "").StartsWith("Unity.Cinemachine")) b.enabled = false;
            var camera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            // Wave-3 landscape pose, kept as the bootstrap's landscape option.
            var landscape = new Pose();
            camera.fieldOfView = HLStageCalibration.PortraitFov; camera.nearClipPlane = .1f; camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(191,210,224,255);
            var cameraData = camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing = false;
            var grid = Object.FindAnyObjectByType<GridManager>();
            var ground = new SerializedObject(grid).FindProperty("_ground").objectReferenceValue as GameObject;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
                if ((r.name == "Ground" && r.gameObject != ground) || r.name == "MiddleLine" || r.name == "Sphere") r.enabled = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.35f,.40f,.5f);
            foreach (var light in Object.FindObjectsByType<Light>()) light.lightmapBakeType = LightmapBakeType.Realtime;
            var key = KeyLight();
            var bounds = new Bounds(new Vector3(grid.transform.position.x,.505f,grid.transform.position.z), new Vector3(grid.width*grid.size,0,grid.height*grid.size));
            var portrait = HLStageCalibration.PlayableFrame(bounds, HLStageCalibration.PortraitPitch, HLStageCalibration.PortraitFov, HLStageCalibration.PortraitAspect, HLStageCalibration.PortraitCentreY);
            landscape = HLStageCalibration.PlayableFrame(bounds, 46, camera.fieldOfView, 16f/9f, .46f);
            camera.aspect=HLStageCalibration.PortraitAspect;
            camera.transform.SetPositionAndRotation(portrait.position, portrait.rotation);
            var stage = new GameObject("HLRenderStage"); stage.SetActive(false);
            var bootstrap = stage.AddComponent<HLRenderBootstrap>();
            var look = Optional(stage, "HLLookController") as Behaviour;
            var zones = Optional(stage, "HLZoneRegistry") as Behaviour;
            var sink = SpellSink(stage);
            if (look) { Calibrate(look, camera, bounds); look.enabled = false; }
            if (zones) zones.enabled = false;
            bootstrap.ConfigureFraming(camera, portrait, landscape);
            var so = new SerializedObject(bootstrap);
            so.FindProperty("lookController").objectReferenceValue = look;
            so.FindProperty("zoneRegistry").objectReferenceValue = zones;
            so.FindProperty("spellVisualSink").objectReferenceValue = sink;
            so.FindProperty("grid").objectReferenceValue = grid;
            so.FindProperty("ground").objectReferenceValue = ground.transform;
            so.ApplyModifiedPropertiesWithoutUndo(); stage.SetActive(true);
            var character = Object.FindAnyObjectByType<Character>();
            (HLCharacterView view, Character character, Transform anchor) healer = default;
            if (character)
            {
                var anchor = new GameObject("HLHealerAnchor"); anchor.transform.SetParent(character.transform,false);
                var view = Optional(anchor,"HLCharacterView");
                if (!view) { var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere); bulb.name="HLHealerPlaceholder"; bulb.transform.SetParent(anchor.transform,false); bulb.transform.localPosition=Vector3.up*.8f; bulb.transform.localScale=new Vector3(.45f,.65f,.45f); Object.DestroyImmediate(bulb.GetComponent<Collider>()); bulb.GetComponent<Renderer>().sharedMaterial=green; }
                var pulse = anchor.AddComponent<HLHealPulse>();
                HLStageBeautyWiring.AttachTrample(anchor, CreatureFootprint(anchor.transform));
                var bso = new SerializedObject(bootstrap); bso.FindProperty("healPulse").objectReferenceValue = pulse; bso.FindProperty("healSource").objectReferenceValue = character.gameObject; bso.ApplyModifiedPropertiesWithoutUndo();
                if (view) healer = (view as HLCharacterView, character, anchor.transform);
                if (view) { var vso=new SerializedObject(view); vso.FindProperty("_character").objectReferenceValue=character; vso.FindProperty("_visualAnchor").objectReferenceValue=anchor.transform; vso.FindProperty("_recipe").objectReferenceValue=AssetDatabase.LoadMainAssetAtPath("Assets/Render/Creatures/Data/HLHealer.asset"); vso.FindProperty("_material").objectReferenceValue=green; vso.ApplyModifiedPropertiesWithoutUndo(); }
            }
            Grass(stage.transform, bounds, grid, ground.transform, camera, zones, bootstrap);
            var battleFocus=stage.AddComponent<HLBattleFocus>();
            var gameView=Object.FindAnyObjectByType<GameView>(FindObjectsInactive.Include);
            battleFocus.Configure(camera,bootstrap,gameView ? gameView.gameHUD.nextWaveButton : null);
            var range = stage.AddComponent<HLStageRangeDriver>(); range.Mode = HLStageRangeDriver.PreviewMode.Pointer;
            if (key) stage.AddComponent<HLStageKeyLight>().KeyLight = key;
            var groundMaterial = StageGroundMaterial(GroundMaterial());
            if (ground.TryGetComponent<Renderer>(out var renderer)) renderer.sharedMaterial = groundMaterial;
            var gust = Environment(stage.transform, grid, ground.transform, camera, zones as HealerLike.Render.Zones.HLZoneRegistry, groundMaterial);
            var wiring = stage.AddComponent<HLStageBeautyWiring>();
            wiring.Configure(bootstrap, grid, gust, groundMaterial);
            if (healer.view) wiring.ConfigureHealer(healer.view, healer.character, AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>("Assets/Render/Creatures/Data/HLHealer.asset"), healer.anchor, green);
            EditorUtility.SetDirty(wiring);
            WireRenderers();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            var fog = WaveThreeFog(camera, bounds);
            WriteCalibration(camera, bounds);
            File.WriteAllText(Root + "WAVE3-TODO.md", "# Wave 3 integration\n\nRe-run `HLStageBuilder.Build` after the other tracks land. No other track source is copied into this branch.\n\n" + string.Join("\n",todo.Distinct().Select(s => "- " + s)) + "\n\n# Calibration\n\n" +
                $"Source: Main (menu loads Main; Build Settings instead enables TestHealer). Board {grid.width} x {grid.height}, cell {grid.size}; roots y=.505. Portrait camera {HLStageCalibration.PortraitPitch} degrees, FOV 40, position {camera.transform.position}. 1920-high hatch spacing {HLStageCalibration.HatchSpacing(camera,CentreDepth(camera,bounds),HLStageCalibration.PortraitHeight):F5}; fog {fog.x:F3}/{fog.y:F3}, six bands, pale #BFD2E0, 1px outline. Numerical calibration awaits final shader/grass captures.\n" +
                "\n# CONTRACT-CONFLICT\n\nThe older look spec proposes stock RenderObjects, but the frozen contract requires T1 HLOutlines. The wave-2 fallback is labelled HLOutlines_PLACEHOLDER and must be replaced by the real feature. Main is the menu target but absent from enabled Build Settings; stage copies Main without changing the source scene list. The Ultra quality slot references missing pipeline GUID a0da25f9ff8de264189edd30d9654c37; Graphics Settings falls back to Low. All six existing pipeline assets and their six renderers are covered. The copied scene hides the 100-unit debug ground (its top .51 obscures the board at .5), middle line and debug sphere renderers; their colliders remain unchanged.\n");
            AssetDatabase.Refresh(); Debug.Log("HL stage build complete: " + ScenePath);
        }
        static void WireRestartPrefab()
        {
            string path=Root+"Prefabs/UI/Views/HLGameOverView.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view=prefab.GetComponentInChildren<GameOverView>(true);
                var button=prefab.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="RestartButton");
                var viewData=new SerializedObject(view); viewData.FindProperty("_restartButton").objectReferenceValue=button; viewData.ApplyModifiedPropertiesWithoutUndo();
                // Keep the view identity used by UIManager; its empty Show/Hide work while disabled.
                // Disabling prevents Start from registering Julien's MenuScene destination.
                view.enabled=false;
                for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--) UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick,i);
                var loader=button.gameObject.AddComponent<HLStageSceneLoader>();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,loader.LoadMenu);
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        static void Copy(string source, string relative, bool prefix = true)
        {
            string path = Root + Path.GetDirectoryName(relative)?.Replace('\\','/') + "/" + (prefix ? "HL" : "") + Path.GetFileName(relative);
            path = path.Replace("//", "/");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path)) { File.Copy(source,path); File.WriteAllText(path+".meta", "fileFormatVersion: 2\nguid: " + Guid.NewGuid().ToString("N") + "\n"); }
            else File.Copy(source,path,true);
            copies[source] = path;
        }
        // Slate variant of HLLook_Default; stones override _BaseColor per instance from the brief's palette.
        static Material StoneMaterial()
        {
            var result = AssetDatabase.LoadAssetAtPath<Material>(LookStone);
            if (!result) { result = new Material(AssetDatabase.LoadAssetAtPath<Material>(LookDefault)) { name = "HLLook_Stone" }; AssetDatabase.CreateAsset(result, LookStone); }
            result.SetColor("_BaseColor", new Color32(142,147,161,255)); result.enableInstancing = true;
            EditorUtility.SetDirty(result); AssetDatabase.SaveAssets();
            return result;
        }
        // Rewrites every serialized reference (renderers and material fields) from a placeholder to its look material.
        static void SwapPlaceholderMaterials()
        {
            var map = new Dictionary<string,string>();
            foreach (var (placeholder, look) in placeholderMaterials)
            {
                string from = AssetDatabase.AssetPathToGUID(placeholder), to = AssetDatabase.AssetPathToGUID(look);
                if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to)) map["guid: " + from] = "guid: " + to;
            }
            foreach (string path in Directory.GetFiles("Assets/Render", "*.*", SearchOption.AllDirectories).Where(p => p.EndsWith(".prefab") || p.EndsWith(".asset") || p.EndsWith(".unity")))
            {
                string yaml = File.ReadAllText(path), swapped = yaml;
                foreach (var pair in map) swapped = swapped.Replace(pair.Key, pair.Value);
                if (swapped != yaml) File.WriteAllText(path, swapped);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        static MonoBehaviour SpellSink(GameObject stage)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Spells/Prefabs/HLSpellVisualSink.prefab");
            if (!prefab) return Optional(stage, "HLSpellVisualSink") as MonoBehaviour;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.transform);
            var sink = instance.GetComponent(Resolve("HLSpellVisualSink")) as MonoBehaviour;
            var so = new SerializedObject(sink); so.FindProperty("material").objectReferenceValue = green; so.ApplyModifiedPropertiesWithoutUndo();
            return sink;
        }
        // Stage-owned model variant: the track prefab plus the observers EntityModel.Init walks before any buff starts.
        // A range at least the board's width bruises every cell from anywhere: the whole board darkens and the readout
        // carries no information (Soldier's authored range is 100). Only shorter ranges get the bruise.
        public const float BruiseMaxRange = 16;
        public static bool Bruises(float range) => range > 0 && range < BruiseMaxRange;
        public static float AuthoredRange(SerializedObject entityData)
        {
            var attributes = entityData.FindProperty("attributes");
            for (int i = 0; attributes != null && i < attributes.arraySize; i++)
            {
                var entry = attributes.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").intValue == (int)AttributeType.Range) return entry.FindPropertyRelative("value").floatValue;
            }
            return 0;
        }
        static GameObject StageModel(GameObject real, bool ally, bool bruise)
        {
            string path = Root + "Prefabs/Models/" + real.name + ".prefab";
            Directory.CreateDirectory(Root + "Prefabs/Models");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(real);
            try
            {
                if (!go.GetComponent<HealerLike.Render.Spells.HLStatusObserver>()) go.AddComponent<HealerLike.Render.Spells.HLStatusObserver>();
                if (ally && !go.GetComponent<HealerLike.Render.Zones.HLRangePreview>()) go.AddComponent<HealerLike.Render.Zones.HLRangePreview>().ObservePointer = false;
                // Finding 8: any entity that heals gets its resolved-heal pulse; EntityModel.Init walks it as an IVisualBehaviour.
                if (!go.GetComponent<HLHealPulse>()) go.AddComponent<HLHealPulse>();
                // Grass wave 5: enemy range readout; it clears itself on non-Computer entities.
                var existing = go.GetComponent<HLBruiseZone>();
                if (bruise && !existing) go.AddComponent<HLBruiseZone>();
                else if (!bruise && existing) Object.DestroyImmediate(existing);
                // Grass beauty: a flattened ring around every creature root. The root crown stays inside its cell.
                HLStageBeautyWiring.AttachTrample(go, CreatureFootprint(go.transform));
                return PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { Object.DestroyImmediate(go); }
        }
        // Creatures beauty: the root footprint stays within the cell bound (Julien's grid cell is 1 unit).
        public const float CellSize = 1;
        public static float CreatureFootprint(Transform root) => CellSize * .5f * Mathf.Max(Mathf.Abs(root.lossyScale.x), Mathf.Abs(root.lossyScale.z));
        static Component Optional(GameObject go, string name)
        {
            var type = Resolve(name);
            if (type != null) return go.GetComponent(type) ?? go.AddComponent(type);
            var marker = go.AddComponent<HLStagePlaceholder>(); marker.requiredType = name;
            marker.replacement = "Rebuild stage after owning track lands.";
            todo.Add(go.name + ": missing " + name + "; marker has no runtime effects."); return null;
        }
        static void Models()
        {
            foreach (string path in copies.Values.Where(p => p.EndsWith(".asset")))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (!(asset is EntityData) && !(asset is CharacterData)) continue;
                var so = new SerializedObject(asset); var model = so.FindProperty("model");
                if (model == null || !model.objectReferenceValue) continue;
                string title = so.FindProperty("title")?.stringValue ?? asset.name;
                string name = title.Replace(" ", "").Replace("Entity", "");
                bool enemy = path.Contains("Enemies") || name.Contains("Soldier") || name.Contains("HitArmor");
                // Creatures wave 5: the buffer is a plant (HLBladeRosette recipe in HLHitArmorBuffer.prefab), not the stone cairn.
                string real = asset is CharacterData ? "Assets/Render/Creatures/Prefabs/HLHealerCharacter.prefab" :
                    name.Contains("HitArmor") ? "Assets/Render/Creatures/Prefabs/HLHitArmorBuffer.prefab" : enemy ?
                    "Assets/Render/Stones/Prefabs/HLStoneSoldierModel.prefab" :
                    "Assets/Render/Creatures/Prefabs/HL" + name + ".prefab";
                var replacement = AssetDatabase.LoadAssetAtPath<GameObject>(real);
                if (!replacement)
                {
                    string target = Root + "Prefabs/HLModel" + asset.name + ".prefab";
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(model.objectReferenceValue);
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                    var primitive = GameObject.CreatePrimitive(enemy ? PrimitiveType.Cube : PrimitiveType.Sphere);
                    primitive.name = "HLModelPlaceholder"; primitive.transform.SetParent(go.transform,false);
                    primitive.transform.localPosition = Vector3.up*.5f; primitive.transform.localScale = new Vector3(.6f,.85f,.6f);
                    Object.DestroyImmediate(primitive.GetComponent<Collider>()); primitive.GetComponent<Renderer>().sharedMaterial = enemy ? stone : green;
                    replacement = PrefabUtility.SaveAsPrefabAsset(go,target); Object.DestroyImmediate(go);
                    todo.Add(target + " -> " + real + " (preserves original model sockets and HUD; primitive placeholder).");
                }
                else if (asset is EntityData) replacement = StageModel(replacement, !enemy, enemy && Bruises(AuthoredRange(so)));
                model.objectReferenceValue = replacement; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
            }
        }
        static void VisualVariant(string source, string destination)
        {
            // Save connected variants, retaining projectile lifetime, collider and motion components.
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
            if (go.GetComponent<Projectile>())
            {
                var observer = go.GetComponent<HLProjectileVisualObserver>() ?? go.AddComponent<HLProjectileVisualObserver>();
                ConfigureObserver(observer, Path.GetFileNameWithoutExtension(source));
                if (!go.GetComponent<HLStoneProjectileImpactBridge>()) go.AddComponent<HLStoneProjectileImpactBridge>();
                if (!go.GetComponent<HLLaunchWave>()) go.AddComponent<HLLaunchWave>();
                // Environment beauty: the plant ring's gust at every real launch, beside the grass gust.
                if (!go.GetComponent<HLStageLaunchGust>()) go.AddComponent<HLStageLaunchGust>();
                // Spells beauty: gold threads between confirmed lightning contacts, on the two lightning variants only.
                if (IsLightning(source) && !go.GetComponent<HLChainContactVisual>()) go.AddComponent<HLChainContactVisual>();
                // The observer hides the gameplay renderers itself once it holds a visual lease and restores them
                // otherwise (creatures fix 3), so the variant keeps the original renderer states.
                LegacyChainVisualOff(go);
            }
            if (go.GetComponent<AreaOfEffect>())
            {
                go.name=Path.GetFileNameWithoutExtension(destination); Optional(go,"HLAreaPulse");
                // The inherited explosion graph is an opaque HDR disk that covers the authored grass footprint.
                // Preserve AreaOfEffect and lifetime scripts; only the render-owned variant silences legacy visuals.
                Optional(go,"HLLegacyAreaVisualMask");
            }
            PrefabUtility.SaveAsPrefabAsset(go,destination); Object.DestroyImmediate(go);
        }
        public static bool IsLightning(string path) { string n = Path.GetFileNameWithoutExtension(path); return n.Contains("ChainLightning") || n.Contains("ChannelingLightning"); }
        public const string DeliveryStylesPath = "Assets/Render/Spells/Data/HLDeliveryStyles.asset";
        public const string CreatureProjectiles = "Assets/Render/Creatures/Prefabs/HLProjectile";
        // Review finding 4: start from the creatures track's authored observer (contact path, channel presentation),
        // then set the delivery style the spells track recorded for this projectile.
        public static void ConfigureObserver(HLProjectileVisualObserver observer, string projectileName)
        {
            var authored = AssetDatabase.LoadAssetAtPath<GameObject>(CreatureProjectiles + projectileName + ".prefab");
            var template = authored ? authored.GetComponent<HLProjectileVisualObserver>() : null;
            if (template && UnityEditorInternal.ComponentUtility.CopyComponent(template)) UnityEditorInternal.ComponentUtility.PasteComponentValues(observer);
            else todo.Add("No creature-authored observer for " + projectileName + "; default observer settings kept.");
            var styles = AssetDatabase.LoadAssetAtPath<HLDeliveryStyles>(DeliveryStylesPath);
            var so = new SerializedObject(observer);
            so.FindProperty("_deliveryStyle").intValue = (int)(styles ? styles.ForName(projectileName) : HLDeliveryStyle.Direct);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // Review finding 2: the legacy chain coroutine draws a LineRenderer from Entity.skillStartPoint every frame, which
        // advances the round-robin firing socket. Its hits are applied synchronously before that loop; the loop and the
        // duration only draw the line. A negative fixed duration skips the loop (no socket read) and the component, its
        // hits and its destruction stay intact. The creature observer draws the chain instead.
        public static bool LegacyChainVisualOff(GameObject projectile)
        {
            var chain = projectile.GetComponent<ChainLightningProjectile>();
            if (!chain) return false;
            var so = new SerializedObject(chain);
            so.FindProperty("_effectMode").intValue = (int)ChainLightningProjectile.EffectMode.FixedDuration;
            so.FindProperty("_effectDuration").floatValue = -1;
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (var line in projectile.GetComponentsInChildren<LineRenderer>(true)) line.enabled = false;
            return true;
        }
        static void RepairVariantReferences()
        {
            var replacements = new Dictionary<string,string>();
            foreach(var pair in copies.Where(p=>p.Key.Contains("/Projectiles/") || p.Value.Contains("/Area/")))
            {
                var target=AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value);
                var objects=target.GetComponentsInChildren<Transform>(true).SelectMany(t=>new Object[]{t.gameObject}.Concat(t.GetComponents<Component>().Cast<Object>()));
                foreach(var obj in objects)
                {
                    if(!obj) continue;
                    var original=PrefabUtility.GetCorrespondingObjectFromSourceAtPath(obj,pair.Key);
                    if(!original) continue;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string originalGuid,out long originalId);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj,out string targetGuid,out long targetId);
                    replacements["fileID: "+originalId+", guid: "+targetGuid]="fileID: "+targetId+", guid: "+targetGuid;
                }
            }
            foreach(string path in copies.Values)
            {
                string yaml=File.ReadAllText(path);
                foreach(var pair in replacements) yaml=yaml.Replace(pair.Key,pair.Value);
                File.WriteAllText(path,yaml);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        static void Calibrate(Behaviour look, Camera camera, Bounds bounds)
        {
            var so = new SerializedObject(look); var settings = so.FindProperty("settings");
            if (settings == null) throw new InvalidOperationException("Review HLLookController settings ABI.");
            var fog = WaveThreeFog(camera, bounds);
            settings.FindPropertyRelative("FogColor").colorValue = new Color32(191,210,224,255);
            settings.FindPropertyRelative("FogStart").floatValue = fog.x; settings.FindPropertyRelative("FogEnd").floatValue = fog.y;
            settings.FindPropertyRelative("FogBands").intValue = 6;
            settings.FindPropertyRelative("ShadowTint").colorValue = new Color32(63,91,148,255);
            settings.FindPropertyRelative("InkStrength").floatValue = .75f;
            float spacing = HLStageCalibration.HatchSpacing(camera,CentreDepth(camera,bounds),HLStageCalibration.PortraitHeight);
            settings.FindPropertyRelative("InkScale").floatValue = spacing;
            settings.FindPropertyRelative("InkWidth").floatValue = spacing*.04f;
            settings.FindPropertyRelative("InkDistStart").floatValue = fog.x;
            settings.FindPropertyRelative("InkFarSpacing").floatValue = spacing*1.2f;
            // Look beauty: outlines are pixel-normalized now, so no distance-derived inflation.
            settings.FindPropertyRelative("OutlineWidthPixels").floatValue = 1f;
            // Look wave 5: pixel-space hatch spacing; a serialized controller predating the field would read zero.
            settings.FindPropertyRelative("InkSpacingPixels").floatValue = 3.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // Wave-3 capture review: fog from the near edge washed out the back half of the board. Start at the
        // board centre and stretch the end by half the board's depth span, so the far row keeps about 60% colour.
        public static Vector2 WaveThreeFog(Camera camera, Bounds bounds)
        {
            var edge = HLStageCalibration.FogRange(camera.transform.position, bounds);
            float centre = Vector3.Distance(camera.transform.position, bounds.center);
            return HLStageCalibration.BackgroundFog(camera.transform.position,bounds);
        }
        public static float CentreDepth(Camera camera, Bounds bounds) => Vector3.Distance(camera.transform.position, bounds.center);
        // Wave 3 drew 1 px at depth 31 on a 1080-high target. Keep the same world thickness: scale by the pixels per
        // world unit at the board centre, rounded to a quarter pixel, never under one pixel.
        public static float OutlinePixels(Camera camera, Bounds bounds)
        {
            float wave3 = 1080 / HLStageCalibration.HatchSpacing(camera,31,4), now = HLStageCalibration.PortraitHeight / HLStageCalibration.HatchSpacing(camera,CentreDepth(camera,bounds),4);
            return Mathf.Max(1, Mathf.Round(now / wave3 * 4) / 4);
        }
        static void WriteCalibration(Camera camera, Bounds bounds)
        {
            var fog = WaveThreeFog(camera, bounds); var edge = HLStageCalibration.FogRange(camera.transform.position, bounds);
            float spacing = HLStageCalibration.HatchSpacing(camera,CentreDepth(camera,bounds),HLStageCalibration.PortraitHeight);
            Debug.Log($"HL calibration: camera {camera.transform.position.ToString("F3")} euler {camera.transform.eulerAngles.ToString("F2")} fov {camera.fieldOfView}; near edge {edge.x:F3} centre {CentreDepth(camera,bounds):F3} edge-range end {edge.y:F3}; fog {fog.x:F3}/{fog.y:F3}; ink scale {spacing:F5} width {spacing*.04f:F5} far {spacing*1.2f:F5}; outline 1 px (pixel-normalized; distance-derived would be {OutlinePixels(camera,bounds)})");
        }
        // One upper-left key light aimed along the stones' cheap-shadow direction, so real and cheap shadows agree.
        static Light KeyLight()
        {
            var key = Object.FindObjectsByType<Light>(FindObjectsInactive.Include).FirstOrDefault(l => l.type == LightType.Directional);
            if (!key) { todo.Add("No directional light in the copied scene; no key light."); return null; }
            key.transform.rotation = HLStageKeyLight.Aim(HLStageKeyLight.StoneKeyDirection);
            key.shadows = LightShadows.Soft; key.enabled = true; key.gameObject.SetActive(true);
            RenderSettings.sun = key;
            return key;
        }
        static Material GroundMaterial()
        {
            const string path = "Assets/Render/Environment/HLLook_Ground.mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!result) { result = new Material(AssetDatabase.LoadAssetAtPath<Material>(LookDefault)) { name = "HLLook_Ground" }; AssetDatabase.CreateAsset(result, path); }
            result.SetColor("_BaseColor", new Color32(78,126,87,255)); result.enableInstancing = true;
            EditorUtility.SetDirty(result); AssetDatabase.SaveAssets();
            return result;
        }
        // Look beauty: the battlefield grid is a material switch; set it on a Stage-owned copy, not the Environment asset.
        public const string StageGroundPath = Root + "Materials/HLStageGround.mat";
        static Material StageGroundMaterial(Material source)
        {
            Directory.CreateDirectory(Root + "Materials");
            var result = AssetDatabase.LoadAssetAtPath<Material>(StageGroundPath);
            if (!result) { result = new Material(source) { name = "HLStageGround" }; AssetDatabase.CreateAsset(result, StageGroundPath); }
            else result.CopyPropertiesFromMaterial(source);
            result.SetFloat("_HLGroundGrid", 1f); result.enableInstancing = true;
            EditorUtility.SetDirty(result); AssetDatabase.SaveAssets();
            return result;
        }
        // Julien-scale ground (1000 x 1000) a hair under the board top, the grass ring and the scattered ring.
        static HLEnvironmentGust Environment(Transform parent, GridManager grid, Transform ground, Camera camera, HealerLike.Render.Zones.HLZoneRegistry zones, Material groundMaterial)
        {
            float top = ground.TryGetComponent<Renderer>(out var groundRenderer) ? groundRenderer.bounds.max.y : .5f;
            var root = new GameObject("HLEnvironment"); root.transform.SetParent(parent,false);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane); plane.name = "HLEnvironmentGround";
            Object.DestroyImmediate(plane.GetComponent<Collider>()); // never intercept gameplay raycasts
            plane.transform.SetParent(root.transform,false);
            plane.transform.position = new Vector3(grid.transform.position.x, top - .01f, grid.transform.position.z);
            plane.transform.localScale = new Vector3(100,1,100);
            var planeRenderer = plane.GetComponent<MeshRenderer>(); planeRenderer.sharedMaterial = groundMaterial; planeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            var gridRect = HealerLike.Render.Environment.HLEnvironmentScatter.GridRect(grid);
            var proxies = new List<GridManager>(); var fields = new List<HealerLike.Render.Grass.HLGrassField>();
            // The ring bands grade outward from the board's default density.
            float boardDensity = HealerLike.Render.Grass.HLGrassLayout.DefaultBudget / (gridRect.width * gridRect.height);
            // Wave-4 capture: a quarter-density ring read as bare ground in a hard rectangle. Graded bands instead, dense at
            // the board edge and thinning outward; the first two bands cover the camera's near edge (z -12.6) and beyond.
            var strips = HealerLike.Render.Environment.HLEnvironmentGrass.Bands(gridRect, RingWidths, RingFractions, boardDensity);
            for (int i = 0; i < strips.Length; i++)
            {
                var rect = strips[i].rect;
                var strip = new GameObject("HLGrassRing" + i + "_band" + strips[i].band); strip.transform.SetParent(root.transform,false);
                strip.transform.position = new Vector3(rect.center.x, grid.transform.position.y, rect.center.y);
                var proxy = strip.AddComponent<GridManager>(); var pso = new SerializedObject(proxy);
                pso.FindProperty("_width").intValue = Mathf.RoundToInt(rect.width / grid.size); pso.FindProperty("_height").intValue = Mathf.RoundToInt(rect.height / grid.size);
                pso.FindProperty("_size").floatValue = grid.size; pso.ApplyModifiedPropertiesWithoutUndo();
                var field = strip.AddComponent<HealerLike.Render.Grass.HLGrassField>(); var fso = new SerializedObject(field);
                fso.FindProperty("_grid").objectReferenceValue = proxy; fso.FindProperty("_ground").objectReferenceValue = ground;
                fso.FindProperty("_gameplayCamera").objectReferenceValue = camera;
                fso.FindProperty("_updateGrass").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/HLGrass.compute");
                fso.FindProperty("_lookMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(LookDefault);
                fso.FindProperty("_ringShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrassRing.shader");
                fso.FindProperty("_bladeBudget").intValue = strips[i].budget;
                fso.FindProperty("_seed").longValue = 11 + i; fso.ApplyModifiedPropertiesWithoutUndo();
                proxies.Add(proxy); fields.Add(field);
                Debug.Log($"HL grass ring {i}: band {strips[i].band} rect {rect} density {strips[i].density:F1} blades {strips[i].budget}");
            }
            root.AddComponent<HealerLike.Render.Environment.HLEnvironmentGrass>().Configure(zones, proxies.ToArray(), fields.ToArray());
            var scatter = root.AddComponent<HealerLike.Render.Environment.HLEnvironmentScatter>(); var sso = new SerializedObject(scatter);
            sso.FindProperty("_grid").objectReferenceValue = grid; sso.FindProperty("_plantMaterial").objectReferenceValue = green;
            sso.FindProperty("_stoneMaterial").objectReferenceValue = stone; sso.FindProperty("_surfaceY").floatValue = top;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var fog = WaveThreeFog(camera, new Bounds(new Vector3(grid.transform.position.x,.505f,grid.transform.position.z), new Vector3(grid.width*grid.size,0,grid.height*grid.size)));
            // Environment beauty: sway, uncurl and distance LOD read this gust and the calibrated fog end.
            var gust = root.AddComponent<HLEnvironmentGust>();
            scatter.ConfigureMotion(camera, gust, fog.y);
            EditorUtility.SetDirty(scatter);
            // BEAUTY.md scale ladder: cropped foreground stones and rosettes in the shadow tint, and a far ridge of monoliths
            // and mushroom stems in the last fog band. Both build in Start from the camera pose the bootstrap applied.
            var foreground = new GameObject("HLForeground").AddComponent<HealerLike.Render.Environment.HLEnvironmentForeground>();
            foreground.transform.SetParent(root.transform,false);
            foreground.Configure(camera, stone, green, top, 1707);
            var ridge = new GameObject("HLFarRidge").AddComponent<HealerLike.Render.Environment.HLEnvironmentRidge>();
            ridge.transform.SetParent(root.transform,false);
            ridge.Configure(camera, stone, green, gridRect, top, fog.x, fog.y, 6, 1707);
            var preview = HealerLike.Render.Environment.HLEnvironmentLayout.Generate(scatter.settings, gridRect, grid.size, top);
            Debug.Log("HL environment scatter: " + string.Join(", ", preview.GroupBy(item => item.kind).Select(g => g.Key + "=" + g.Count())) + $" total={preview.Count}");
            return gust;
        }
        public static readonly float[] RingWidths = { 3, 5, 16 };
        public static readonly float[] RingFractions = { .85f, .6f, .15f };
        static Component StoneGrid(GameObject stage, GridManager grid)
        {
            var real = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/HLStoneGrid.prefab");
            if (!real) { Optional(stage,"HLStoneGridEntry"); todo.Add("Stone generation: replace marker with HLStoneGrid.prefab generator/entry; preserve both block-system recipes (20..50, size 5..15; 0..10, size 4..15) and final completion fence. Main generator list is empty; no fallback terrain generation runs."); return null; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(real);
            instance.name = "HLStoneGeneration"; instance.transform.SetParent(stage.transform,false);
            // Use existing gameplay grid, not the template's second grid manager/ground.
            var templateGrid = instance.GetComponent<GridManager>();
            if (templateGrid) Object.DestroyImmediate(templateGrid);
            foreach (var r in instance.GetComponentsInChildren<Renderer>()) r.enabled = false;
            var entry = instance.GetComponent<HLStoneGridEntry>();
            // Finding 5: this scene is the demo fixture; the stones fix refuses Generate unless the entry opts in.
            var eso = new SerializedObject(entry); eso.FindProperty("_demoSceneOnly").boolValue = true; eso.ApplyModifiedPropertiesWithoutUndo();
            SpreadStoneRecipes(entry);
            return entry;
        }
        // Wave-4 capture: the terrain stones sat in three dense blocks. Many small clumps instead of a few 5..15-cell blocks.
        public static readonly Vector2Int[] StoneBlockCounts = { new Vector2Int(9, 14), new Vector2Int(0, 3) };
        public static readonly Vector2Int[] StoneBlockSizes = { new Vector2Int(1, 3), new Vector2Int(1, 2) };
        static void SpreadStoneRecipes(HLStoneGridEntry entry)
        {
            var systems = new SerializedObject(entry).FindProperty("_systems");
            for (int i = 0; i < systems.arraySize && i < StoneBlockCounts.Length; i++)
            {
                var system = systems.GetArrayElementAtIndex(i).objectReferenceValue; if (!system) continue;
                var so = new SerializedObject(system);
                so.FindProperty("_min").intValue = StoneBlockCounts[i].x; so.FindProperty("_max").intValue = StoneBlockCounts[i].y;
                so.FindProperty("_minSize").intValue = StoneBlockSizes[i].x; so.FindProperty("_maxSize").intValue = StoneBlockSizes[i].y;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static void Grass(Transform parent, Bounds board, GridManager grid, Transform ground, Camera camera, Behaviour zones, HLRenderBootstrap bootstrap)
        {
            var go = new GameObject("HLGrassFieldPlaceholder"); go.transform.SetParent(parent,false); go.transform.position = board.center;
            var fieldType=Resolve("HLGrassField");
            if(fieldType!=null && zones)
            {
                go.name="HLGrassField";
                var field=(Behaviour)go.AddComponent(fieldType); var fso=new SerializedObject(field);
                fso.FindProperty("_grid").objectReferenceValue=grid; fso.FindProperty("_ground").objectReferenceValue=ground;
                // 0.6 height at the default budget: a closed carpet of short upright spikes that leaves actor roots readable.
                fso.FindProperty("_bladeHeightScale").floatValue=.6f; fso.FindProperty("_bladeBudget").intValue=HealerLike.Render.Grass.HLGrassLayout.DefaultBudget;
                fso.FindProperty("_gameplayCamera").objectReferenceValue=camera;
                fso.FindProperty("_updateGrass").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/HLGrass.compute");
                fso.FindProperty("_lookMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(LookDefault);
                fso.FindProperty("_ringShader").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrassRing.shader"); fso.ApplyModifiedPropertiesWithoutUndo();
                var bridge=parent.gameObject.AddComponent<HLStageZoneBridge>(); var bso=new SerializedObject(bridge);
                bso.FindProperty("zoneRegistry").objectReferenceValue=zones; bso.FindProperty("grassField").objectReferenceValue=field; bso.ApplyModifiedPropertiesWithoutUndo();
                var bootstrapSO=new SerializedObject(bootstrap); bootstrapSO.FindProperty("zoneBridge").objectReferenceValue=bridge; bootstrapSO.FindProperty("grassField").objectReferenceValue=field; bootstrapSO.ApplyModifiedPropertiesWithoutUndo();
                bridge.enabled=false; field.enabled=false;
                return;
            }
            var marker = go.AddComponent<HLStagePlaceholder>(); marker.requiredType = "HLGrassField";
            marker.replacement = "Bind existing GridManager and ground; cover public dimensions, after cells initialize. Replace this static mesh with T2 GPU grass.";
            // One mesh, no gameplay cells/colliders and no GameObject per blade.
            var vertices = new List<Vector3>(); var indices = new List<int>(); var colors = new List<Color>();
            var random = new System.Random(1707);
            for (int i=0;i<4096;i++)
            {
                float x = ((float)random.NextDouble()-.5f)*board.size.x, z = ((float)random.NextDouble()-.5f)*board.size.z;
                float h = .15f+(float)random.NextDouble()*.2f; int v = vertices.Count;
                vertices.Add(new Vector3(x-.025f,0,z)); vertices.Add(new Vector3(x+.025f,0,z)); vertices.Add(new Vector3(x+.045f,h,z+.02f));
                indices.AddRange(new[]{v,v+2,v+1,v,v+1,v+2});
                colors.Add(new Color32(78,126,87,255)); colors.Add(new Color32(78,126,87,255)); colors.Add(new Color32(155,210,74,255));
            }
            var mesh = new Mesh { name = "HLGrassPlaceholder" }; mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.SetColors(colors); mesh.SetNormals(Enumerable.Repeat(new Vector3(0,.6f,-.8f),vertices.Count).ToArray()); mesh.RecalculateBounds();
            string path = Root+"HLGrassPlaceholder.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; } else AssetDatabase.CreateAsset(mesh,path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial=green;
            todo.Add("HLGrassFieldPlaceholder / HLGrassPlaceholder.asset: static 4096-blade coverage mesh; replace with T2 HLGrassField and HLStageZoneBridge (after zone publication, before grass submission), bound to board [-8,8] squared at y=.505. No wind, zone response or indirect rendering is claimed.");
        }
        // Look wave 5 grass-safe thresholds (captures wave5-edges-*): depth 1 world unit at 31, slope 1, normal 55 deg,
        // density penalty 35 deg, grass opted out through the normal-alpha mask.
        public static void EdgeSettings(SerializedObject outlines)
        {
            outlines.FindProperty("DepthNormalEdges").boolValue = true;
            outlines.FindProperty("DepthThresholdWorld").floatValue = 1;
            outlines.FindProperty("ReferenceDistance").floatValue = 31;
            outlines.FindProperty("DistanceScale").floatValue = 1;
            outlines.FindProperty("NormalAngleDegrees").floatValue = 55;
            outlines.FindProperty("NormalDensityDegrees").floatValue = 35;
            outlines.FindProperty("UseNormalEdgeMask").boolValue = true;
        }
        // Look wave 5's tested Very High setup on every tier: the stage resolves to whichever tier Graphics/Quality
        // settings pick (Low today, through the missing Ultra slot), and the board sits 42 to 48 units from the camera.
        public const float ShadowDistance = 70;
        public static void Shadows(SerializedObject pipeline)
        {
            pipeline.FindProperty("m_MainLightRenderingMode").intValue = 1;
            pipeline.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            pipeline.FindProperty("m_ShadowDistance").floatValue = ShadowDistance;
            pipeline.FindProperty("m_ShadowCascadeCount").intValue = 2;
            pipeline.FindProperty("m_Cascade2Split").floatValue = 1f / 3;
            var map = pipeline.FindProperty("m_MainLightShadowmapResolution"); map.intValue = Mathf.Max(map.intValue, 2048);
            pipeline.FindProperty("m_SoftShadowsSupported").boolValue = true;
        }
        static void WireRenderers()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset",new[]{"Assets/Settings"}))
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var pso = new SerializedObject(pipeline); var list=pso.FindProperty("m_RendererDataList");
                for(int i=0;i<list.arraySize;i++)
                {
                    var data=list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData; if (!data) continue;
                    foreach(var old in data.rendererFeatures.Where(f=>f && f.name.StartsWith("HLOutlines")).ToArray()) { data.rendererFeatures.Remove(old); Object.DestroyImmediate(old,true); }
                    var type=Resolve("HLOutlines"); ScriptableRendererFeature feature;
                    if(type!=null) feature=(ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
                    else {
                        var placeholder=ScriptableObject.CreateInstance<RenderObjects>();
                        placeholder.settings.passTag="HLOutlines_PLACEHOLDER";
                        placeholder.settings.overrideMode=RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None;
                        placeholder.settings.Event=RenderPassEvent.AfterRenderingOpaques;
                        placeholder.settings.filterSettings.LayerMask=~0;
                        placeholder.settings.filterSettings.PassNames=new[]{"HLOutline"};
                        feature=placeholder; todo.Add(AssetDatabase.GetAssetPath(data)+": replace HLOutlines_PLACEHOLDER (hull tag only) with T1 HLOutlines depth/normal feature.");
                    }
                    feature.name=type!=null?"HLOutlines":"HLOutlines_PLACEHOLDER";
                    if(type!=null) { var fso=new SerializedObject(feature); var shader=fso.FindProperty("edgeShader"); if(shader!=null) shader.objectReferenceValue=Shader.Find("Hidden/HL/Look/DepthNormalOutline");
                        // Look wave 5: grass writes zero normal-edge eligibility, so the screen pass no longer inks every blade.
                        EdgeSettings(fso); fso.ApplyModifiedPropertiesWithoutUndo(); }
                    AssetDatabase.AddObjectToAsset(feature,data); data.rendererFeatures.Add(feature); feature.SetActive(true);
                    var dso=new SerializedObject(data); var map=dso.FindProperty("m_RendererFeatureMap"); map.arraySize=data.rendererFeatures.Count;
                    for(int j=0;j<data.rendererFeatures.Count;j++) { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[j],out string _,out long id); map.GetArrayElementAtIndex(j).longValue=id; }
                    dso.ApplyModifiedPropertiesWithoutUndo(); data.SetDirty(); EditorUtility.SetDirty(data);
                }
                Shadows(pso); pso.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
            }
        }
    }
}

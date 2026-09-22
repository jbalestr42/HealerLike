using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        public static Type Resolve(string name) => TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().FirstOrDefault(t => t.Name == name);

        [MenuItem("HL/Stage/Build isolated gameplay stage")]
        public static void Build()
        {
            todo.Clear(); copies.Clear();
            Directory.CreateDirectory(Root + "Data"); Directory.CreateDirectory(Root + "Prefabs");
            Directory.CreateDirectory(Root + "Materials");
            AssetDatabase.Refresh();
            green = Material("HLAllyPlaceholder", new Color32(127,201,63,255));
            stone = Material("HLStonePlaceholder", new Color32(142,147,161,255));
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
            camera.transform.rotation = Quaternion.Euler(50, 0, 0);
            camera.transform.position = new Vector3(0,.5f,0) - camera.transform.forward * 31;
            camera.fieldOfView = 40; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(191,210,224,255);
            var cameraData = camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing = false;
            var grid = Object.FindAnyObjectByType<GridManager>();
            var ground = new SerializedObject(grid).FindProperty("_ground").objectReferenceValue as GameObject;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
                if ((r.name == "Ground" && r.gameObject != ground) || r.name == "MiddleLine" || r.name == "Sphere") r.enabled = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.35f,.40f,.5f);
            foreach (var light in Object.FindObjectsByType<Light>()) light.lightmapBakeType = LightmapBakeType.Realtime;
            var bounds = new Bounds(new Vector3(grid.transform.position.x,.505f,grid.transform.position.z), new Vector3(grid.width*grid.size,0,grid.height*grid.size));
            var stage = new GameObject("HLRenderStage"); stage.SetActive(false);
            var bootstrap = stage.AddComponent<HLRenderBootstrap>();
            var look = Optional(stage, "HLLookController") as Behaviour;
            var zones = Optional(stage, "HLZoneRegistry") as Behaviour;
            var sink = Optional(stage, "HLSpellVisualSink") as MonoBehaviour;
            if (look) { Calibrate(look, camera, bounds); look.enabled = false; }
            if (zones) zones.enabled = false;
            var so = new SerializedObject(bootstrap);
            so.FindProperty("lookController").objectReferenceValue = look;
            so.FindProperty("zoneRegistry").objectReferenceValue = zones;
            so.FindProperty("spellVisualSink").objectReferenceValue = sink;
            so.FindProperty("grid").objectReferenceValue = grid;
            so.FindProperty("ground").objectReferenceValue = ground.transform;
            var entry = StoneGrid(stage, grid);
            so.FindProperty("stoneGridEntry").objectReferenceValue = entry;
            so.ApplyModifiedPropertiesWithoutUndo(); stage.SetActive(true);
            var character = Object.FindAnyObjectByType<Character>();
            if (character)
            {
                var anchor = new GameObject("HLHealerAnchor"); anchor.transform.SetParent(character.transform,false);
                var view = Optional(anchor,"HLCharacterView");
                if (!view) { var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere); bulb.name="HLHealerPlaceholder"; bulb.transform.SetParent(anchor.transform,false); bulb.transform.localPosition=Vector3.up*.8f; bulb.transform.localScale=new Vector3(.45f,.65f,.45f); Object.DestroyImmediate(bulb.GetComponent<Collider>()); bulb.GetComponent<Renderer>().sharedMaterial=green; }
                if (view) { var vso=new SerializedObject(view); vso.FindProperty("character").objectReferenceValue=character; vso.FindProperty("visualAnchor").objectReferenceValue=anchor.transform; vso.FindProperty("recipe").objectReferenceValue=AssetDatabase.LoadMainAssetAtPath("Assets/Render/Creatures/Data/HLHealer.asset"); vso.FindProperty("material").objectReferenceValue=green; vso.ApplyModifiedPropertiesWithoutUndo(); }
            }
            Grass(stage.transform, bounds, grid, ground.transform, camera, zones, bootstrap);
            if (ground.TryGetComponent<Renderer>(out var renderer)) renderer.sharedMaterial = green;
            WireRenderers();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            var fog = HLStageCalibration.FogRange(camera.transform.position, bounds);
            File.WriteAllText(Root + "WAVE3-TODO.md", "# Wave 3 integration\n\nRe-run `HLStageBuilder.Build` after the other tracks land. No other track source is copied into this branch.\n\n" + string.Join("\n",todo.Distinct().Select(s => "- " + s)) + "\n\n# Calibration\n\n" +
                $"Source: Main (menu loads Main; Build Settings instead enables TestHealer). Board {grid.width} x {grid.height}, cell {grid.size}; roots y=.505. Camera 50 degrees, FOV 40, distance 31, position {camera.transform.position}. 1080p hatch spacing {HLStageCalibration.HatchSpacing(camera,31,1080):F5}; fog {fog.x:F3}/{fog.y:F3}, six bands, pale #BFD2E0, 1px outline. Numerical calibration awaits final shader/grass captures.\n" +
                "\n# CONTRACT-CONFLICT\n\nThe older look spec proposes stock RenderObjects, but the frozen contract requires T1 HLOutlines. The wave-2 fallback is labelled HLOutlines_PLACEHOLDER and must be replaced by the real feature. Main is the menu target but absent from enabled Build Settings; stage copies Main without changing the source scene list. The Ultra quality slot references missing pipeline GUID a0da25f9ff8de264189edd30d9654c37; Graphics Settings falls back to Low. All six existing pipeline assets and their six renderers are covered. The copied scene hides the 100-unit debug ground (its top .51 obscures the board at .5), middle line and debug sphere renderers; their colliders remain unchanged.\n");
            AssetDatabase.Refresh(); Debug.Log("HL stage build complete: " + ScenePath);
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
        static Material Material(string name, Color color)
        {
            string path = Root + "Materials/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("HL/Look/Primitive") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (!result) { result = new Material(shader); AssetDatabase.CreateAsset(result,path); }
            result.shader = shader; result.SetColor("_BaseColor",color); result.enableInstancing = true;
            if (shader.name != "HL/Look/Primitive") todo.Add(path + ": replace URP Lit with HL/Look/Primitive; toon, hatch and banded fog are unavailable in this wave.");
            return result;
        }
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
                string real = asset is CharacterData ? "Assets/Render/Creatures/Prefabs/HLHealerCharacter.prefab" : enemy ?
                    "Assets/Render/Stones/Prefabs/" + (name.Contains("HitArmor") ? "HLStoneCairnModel" : "HLStoneSoldierModel") + ".prefab" :
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
                model.objectReferenceValue = replacement; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
            }
        }
        static void VisualVariant(string source, string destination)
        {
            // Save connected variants, retaining projectile lifetime, collider and motion components.
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
            if (go.GetComponent<Projectile>())
            {
                Optional(go, "HLProjectileVisualObserver"); Optional(go, "HLStoneProjectileImpactBridge");
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            }
            if (go.GetComponent<AreaOfEffect>()) { go.name = Path.GetFileNameWithoutExtension(destination); Optional(go,"HLAreaPulse"); }
            PrefabUtility.SaveAsPrefabAsset(go,destination); Object.DestroyImmediate(go);
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
            var fog = HLStageCalibration.FogRange(camera.transform.position,bounds);
            settings.FindPropertyRelative("FogColor").colorValue = new Color32(191,210,224,255);
            settings.FindPropertyRelative("FogStart").floatValue = fog.x; settings.FindPropertyRelative("FogEnd").floatValue = fog.y;
            settings.FindPropertyRelative("FogBands").intValue = 6;
            float spacing = HLStageCalibration.HatchSpacing(camera,31,1080);
            settings.FindPropertyRelative("InkScale").floatValue = spacing;
            settings.FindPropertyRelative("InkWidth").floatValue = spacing*.04f;
            settings.FindPropertyRelative("InkDistStart").floatValue = fog.x;
            settings.FindPropertyRelative("InkFarSpacing").floatValue = spacing*1.2f;
            settings.FindPropertyRelative("OutlineWidthPixels").floatValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
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
            return instance.GetComponent(Resolve("HLStoneGridEntry"));
        }
        static void Grass(Transform parent, Bounds board, GridManager grid, Transform ground, Camera camera, Behaviour zones, HLRenderBootstrap bootstrap)
        {
            var go = new GameObject("HLGrassFieldPlaceholder"); go.transform.SetParent(parent,false); go.transform.position = board.center;
            var fieldType=Resolve("HLGrassField");
            if(fieldType!=null && zones)
            {
                go.name="HLGrassField";
                var field=(Behaviour)go.AddComponent(fieldType); var fso=new SerializedObject(field);
                fso.FindProperty("grid").objectReferenceValue=grid; fso.FindProperty("ground").objectReferenceValue=ground;
                fso.FindProperty("gameplayCamera").objectReferenceValue=camera;
                fso.FindProperty("updateGrass").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/HLGrass.compute");
                fso.FindProperty("grassShader").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrass.shader");
                fso.FindProperty("ringShader").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrassRing.shader"); fso.ApplyModifiedPropertiesWithoutUndo();
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
                colors.Add(new Color32(46,125,79,255)); colors.Add(new Color32(46,125,79,255)); colors.Add(new Color32(155,210,74,255));
            }
            var mesh = new Mesh { name = "HLGrassPlaceholder" }; mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.SetColors(colors); mesh.SetNormals(Enumerable.Repeat(new Vector3(0,.6f,-.8f),vertices.Count).ToArray()); mesh.RecalculateBounds();
            string path = Root+"HLGrassPlaceholder.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; } else AssetDatabase.CreateAsset(mesh,path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial=green;
            todo.Add("HLGrassFieldPlaceholder / HLGrassPlaceholder.asset: static 4096-blade coverage mesh; replace with T2 HLGrassField and HLStageZoneBridge (after zone publication, before grass submission), bound to board [-8,8] squared at y=.505. No wind, zone response or indirect rendering is claimed.");
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
                    if(type!=null) { var fso=new SerializedObject(feature); var shader=fso.FindProperty("edgeShader"); if(shader!=null) shader.objectReferenceValue=Shader.Find("Hidden/HL/Look/DepthNormalOutline"); fso.ApplyModifiedPropertiesWithoutUndo(); }
                    AssetDatabase.AddObjectToAsset(feature,data); data.rendererFeatures.Add(feature); feature.SetActive(true);
                    var dso=new SerializedObject(data); var map=dso.FindProperty("m_RendererFeatureMap"); map.arraySize=data.rendererFeatures.Count;
                    for(int j=0;j<data.rendererFeatures.Count;j++) { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[j],out string _,out long id); map.GetArrayElementAtIndex(j).longValue=id; }
                    dso.ApplyModifiedPropertiesWithoutUndo(); data.SetDirty(); EditorUtility.SetDirty(data);
                }
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Batchmode play sessions that lay units on the board at the portrait camera and write the look grammar's sheets:
    // -executeMethod HealerLike.Render.Stage.LookSheetCapture.Units, or .Effects. The editor exits with the run's code.
    [InitializeOnLoad]
    public static class LookSheetCapture
    {
        static readonly string modeKey = "LookSheetCapture.Mode";
        static readonly string codeKey = "LookSheetCapture.Code";
        static readonly string deadlineKey = "LookSheetCapture.Deadline";
        static EditorWindow _gameView;
        static AStageRun _run;

        static LookSheetCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
        }

        [MenuItem("Tools/Render/Capture Look Sheet")]
        public static void Units()
        {
            Enter("units", 240f);
        }

        [MenuItem("Tools/Render/Capture Effect Sheet")]
        public static void Effects()
        {
            Enter("effects", 240f);
        }

        public static void Finish(bool isPassed)
        {
            _run = null;
            SessionState.SetInt(codeKey, isPassed ? 0 : 1);
            EditorApplication.isPlaying = false;
        }

        static void Enter(string mode, float seconds)
        {
            EditorSceneManager.OpenScene(StagePlay.ScenePath);
            SessionState.SetString(modeKey, mode);
            SessionState.SetInt(codeKey, 1);
            SessionState.SetFloat(deadlineKey, (float)EditorApplication.timeSinceStartup + seconds);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            string mode = SessionState.GetString(modeKey, "");
            if (mode == "")
            {
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                _run = new LookSheetRun(mode == "effects");
                _run.Begin();
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
                int code = SessionState.GetInt(codeKey, 1);
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(code);
            }
        }

        static void OnUpdate()
        {
            if (SessionState.GetString(modeKey, "") == "")
            {
                return;
            }

            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(deadlineKey, 0f))
            {
                Debug.LogError("[LookSheetCapture] The session ran past its deadline.");
                SessionState.SetString(modeKey, "");
                EditorApplication.Exit(2);
                return;
            }

            // Batchmode has no visible Game view, the repaint is what lets end of frame waits resume
            if (EditorApplication.isPlaying)
            {
                if (_gameView == null)
                {
                    Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                    _gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                }

                _gameView.Repaint();
                if (_run != null)
                {
                    _run.Step();
                }
            }
        }
    }

    // Clears the board, lays one unit per cell at the portrait camera, then writes a colour sheet, a greyscale sheet
    // and a labelled contact sheet of every cell
    public class LookSheetRun : AStageRun
    {
        public static readonly int Width = 1080;
        public static readonly int Height = 1920;
        public static readonly int Columns = 4;
        // Whole cells between two units, so every unit lands on a cell centre
        public static readonly int Spacing = 3;
        // A crop spans this many cells across and up, enough for the longest roots and the tallest stem
        public static readonly float CropCells = 2.8f;
        public static readonly int CellPixels = 330;
        public static readonly int LabelScale = 3;

        static readonly DeliveryStyle[] styles =
        {
            DeliveryStyle.Direct, DeliveryStyle.Arc, DeliveryStyle.Rigid, DeliveryStyle.Swarm, DeliveryStyle.Bounce,
            DeliveryStyle.ChainSync, DeliveryStyle.Thrown
        };

        bool _isEffects;
        readonly List<Object> _created = new List<Object>();
        readonly List<Transform> _cells = new List<Transform>();
        readonly List<string> _labels = new List<string>();

        public LookSheetRun(bool isEffects)
        {
            _isEffects = isEffects;
        }

        protected override IEnumerator Run()
        {
            _manager.SetLandscape(false);
            yield return Wait(0.5f);
            ClearBoard();
            yield return NextFrame();

            if (_isEffects)
            {
                yield return SpawnEffects();
            }
            else
            {
                SpawnUnits();
            }

            HideHud();
            yield return Wait(1.2f);
            if (_isEffects)
            {
                ShowImpacts();
                yield return Wait(0.25f);
            }

            string name = _isEffects ? "look-sheet-effects" : "look-sheet-units";
            bool isWritten = Write(name);
            foreach (Object created in _created)
            {
                Object.Destroy(created);
            }

            Debug.Log($"[LookSheetRun] {name} cells {_cells.Count} written {isWritten}");
            LookSheetCapture.Finish(isWritten && _cells.Count > 0);
        }

        // His first wave is already on the board, its cells are taken back for the sheet
        void ClearBoard()
        {
            foreach (Entity.EntityType side in new[] { Entity.EntityType.Player, Entity.EntityType.Computer })
            {
                List<GameObject> entities = new List<GameObject>(_manager.entityManager.GetEntities(side));
                foreach (GameObject entityGo in entities)
                {
                    _manager.entityManager.DestroyEntity(entityGo, side);
                }
            }
        }

        void SpawnUnits()
        {
            List<Vector3> cells = Cells(LookSheetUnits.Roster.Length + LookSheetUnits.Undesigned.Length + 1);
            int next = 0;
            foreach (string path in LookSheetUnits.Roster)
            {
                EntityData data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
                Spawn(data, LookSheetUnits.Side(data), cells[next++], data.name.Replace("Entity", ""));
            }

            foreach (string unit in LookSheetUnits.Undesigned)
            {
                Spawn(LookSheetUnits.Create(unit, _created), LookSheetUnits.Side(unit), cells[next++], unit);
            }

            Character character = _manager.player.character;
            if (character != null)
            {
                character.transform.position = cells[next];
                _cells.Add(character.transform);
                _labels.Add("HEALER");
            }
        }

        IEnumerator SpawnEffects()
        {
            EntityData plant = AssetDatabase.LoadAssetAtPath<EntityData>(LookSheetUnits.Roster[0]);
            List<Vector3> cells = Cells(LookSheetUnits.Families.Length + styles.Length);
            for (int i = 0; i < cells.Count; i++)
            {
                string label = i < LookSheetUnits.Families.Length
                    ? LookSheetUnits.Families[i]
                    : styles[i - LookSheetUnits.Families.Length].ToString();
                Spawn(plant, Entity.EntityType.Player, cells[i], label);
            }

            // The rigs are built on spawn, a frame lets them settle before the gestures start
            yield return NextFrame();
            if (_cells.Count != cells.Count)
            {
                Debug.LogError("[LookSheetRun] A plant was refused a cell, the effects would land on the wrong one");
                yield break;
            }

            GameObject source = _manager.player.character != null ? _manager.player.character.gameObject : null;
            SpellVisualSink sink = _manager.spellSink;
            for (int i = 0; i < LookSheetUnits.Families.Length; i++)
            {
                ABuffHandlerFactory handler = LookSheetUnits.Handler(LookSheetUnits.Families[i], _created);
                if (handler != null)
                {
                    sink.SetStatus(source, _cells[i].gameObject, handler, 1, 0.8f, 6f, ClockKind.Simulation);
                }
            }

            for (int i = 0; i < styles.Length; i++)
            {
                Transform cell = _cells[LookSheetUnits.Families.Length + i];
                CreatureBuilder builder = cell.GetComponentInChildren<CreatureBuilder>();
                GameObject projectile = new GameObject("SheetProjectile");
                _created.Add(projectile);
                Vector3 end = cell.position + new Vector3(1.2f, 0.4f, 1.2f);
                projectile.transform.position = Vector3.Lerp(cell.position + Vector3.up, end, 0.6f);
                bool isBegun = builder != null && builder.BeginDelivery(_manager.NextDeliveryToken(), styles[i], projectile.transform, end);
                Debug.Log($"[LookSheetRun] Delivery {styles[i]} begun {isBegun}");
            }
        }

        // Damage and heal are instant, they show through the sink's impact path just before the capture
        void ShowImpacts()
        {
            GameObject source = _manager.player.character != null ? _manager.player.character.gameObject : null;
            for (int i = 0; i < LookSheetUnits.Families.Length; i++)
            {
                string family = LookSheetUnits.Families[i];
                if (family == "DAMAGE" || family == "HEAL")
                {
                    float amount = family == "HEAL" ? 30f : -30f;
                    _manager.spellSink.ShowImpact(source, _cells[i].gameObject, ResourceKind.Health, amount, false);
                }
            }
        }

        void Spawn(EntityData data, Entity.EntityType side, Vector3 position, string label)
        {
            GameObject entityGo = _manager.entityManager.SpawnEntity(data, position, side);
            if (entityGo == null)
            {
                Debug.LogError($"[LookSheetRun] {label} refused at {position}");
                return;
            }

            _cells.Add(entityGo.transform);
            _labels.Add(label.ToUpperInvariant());
        }

        void HideHud()
        {
            foreach (Transform cell in _cells)
            {
                foreach (Canvas canvas in cell.GetComponentsInChildren<Canvas>(true))
                {
                    canvas.enabled = false;
                }
            }
        }

        // Cell centres around the board centre, row by row from the far side, kept inside the portrait frame
        List<Vector3> Cells(int count)
        {
            GridManager grid = _manager.player.grid;
            Camera camera = _manager.gameCamera;
            camera.aspect = (float)Width / Height;
            Vector3 centre = _manager.board.center;
            int rows = Mathf.CeilToInt((float)count / Columns);
            List<Vector3> cells = new List<Vector3>();
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < Columns && cells.Count < count; column++)
                {
                    Vector3 offset = new Vector3(column - (Columns - 1) * 0.5f, 0f, (rows - 1) * 0.5f - row) * (Spacing * grid.size);
                    // Rounded onto a cell centre first, so no position sits between two cells
                    Vector3 point = centre + offset - _manager.board.min;
                    point.x = (Mathf.Floor(point.x / grid.size) + 0.5f) * grid.size;
                    point.z = (Mathf.Floor(point.z / grid.size) + 0.5f) * grid.size;
                    Vector3 cell = grid.GetNearestWalkablePosition(point + _manager.board.min);
                    Vector3 viewport = camera.WorldToViewportPoint(cell);
                    if (viewport.x < 0.05f || viewport.x > 0.95f || viewport.y < 0.05f || viewport.y > 0.9f)
                    {
                        Debug.LogError($"[LookSheetRun] Cell {cell} falls outside the portrait frame at {viewport}");
                    }
                    cells.Add(cell);
                }
            }
            return cells;
        }

        bool Write(string name)
        {
            Texture2D colour = Capture();
            Texture2D grey = Greyscale(colour);
            Texture2D contact = Contact(colour, grey);
            Directory.CreateDirectory(StagePlay.CaptureFolder);
            string root = StagePlay.CaptureFolder + name;
            File.WriteAllBytes(root + "-colour.png", colour.EncodeToPNG());
            File.WriteAllBytes(root + "-grey.png", grey.EncodeToPNG());
            File.WriteAllBytes(root + "-contact.png", contact.EncodeToPNG());
            Object.Destroy(colour);
            Object.Destroy(grey);
            Object.Destroy(contact);
            return File.Exists(root + "-contact.png");
        }

        // The portrait frame as his device shows it, rendered by the board camera into a texture
        Texture2D Capture()
        {
            Camera camera = _manager.gameCamera;
            RenderTexture target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            camera.aspect = (float)Width / Height;
            RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();
            request.destination = target;
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return texture;
        }

        // Rec. 709 luminance of each pixel
        static Texture2D Greyscale(Texture2D colour)
        {
            Color32[] pixels = colour.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                byte luminance = (byte)Mathf.RoundToInt(0.2126f * pixel.r + 0.7152f * pixel.g + 0.0722f * pixel.b);
                pixels[i] = new Color32(luminance, luminance, luminance, 255);
            }

            Texture2D grey = new Texture2D(colour.width, colour.height, TextureFormat.RGB24, false);
            grey.SetPixels32(pixels);
            grey.Apply();
            return grey;
        }

        // One cell per unit: its colour crop, its greyscale crop, and its label under both, crops enlarged by whole pixels
        Texture2D Contact(Texture2D colour, Texture2D grey)
        {
            Camera camera = _manager.gameCamera;
            float size = _manager.player.grid.size;
            Vector3 origin = camera.WorldToViewportPoint(_manager.board.center);
            Vector3 across = camera.WorldToViewportPoint(_manager.board.center + Vector3.right * size);
            int crop = Mathf.Max(8, Mathf.RoundToInt((across.x - origin.x) * Width * CropCells));
            int zoom = Mathf.Max(1, CellPixels / crop);
            int labelHeight = 8 * LabelScale + 12;
            int cellWidth = crop * zoom * 2 + 12;
            int cellHeight = crop * zoom + labelHeight;
            int rows = Mathf.CeilToInt((float)_cells.Count / Columns);
            Texture2D sheet = new Texture2D(cellWidth * Columns, cellHeight * rows, TextureFormat.RGB24, false);
            Color32[] background = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < background.Length; i++)
            {
                background[i] = new Color32(24, 26, 30, 255);
            }
            sheet.SetPixels32(background);

            for (int i = 0; i < _cells.Count; i++)
            {
                Vector3 viewport = camera.WorldToViewportPoint(Centre(_cells[i]));
                int x = Mathf.RoundToInt(viewport.x * Width) - crop / 2;
                int y = Mathf.RoundToInt(viewport.y * Height) - crop / 2;
                int left = (i % Columns) * cellWidth;
                int bottom = (rows - 1 - i / Columns) * cellHeight;
                Blit(colour, x, y, crop, zoom, sheet, left, bottom + labelHeight);
                Blit(grey, x, y, crop, zoom, sheet, left + crop * zoom + 12, bottom + labelHeight);
                SheetFont.Draw(sheet, _labels[i], left + 6, bottom + labelHeight - 6, LabelScale, Color.white);
            }

            sheet.Apply();
            Debug.Log($"[LookSheetRun] Contact crops of {crop} px, drawn {zoom} times larger");
            return sheet;
        }

        // The middle of what the unit draws, his hidden model aside
        static Vector3 Centre(Transform cell)
        {
            Bounds bounds = new Bounds(cell.position, Vector3.zero);
            foreach (Renderer renderer in cell.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled && renderer is MeshRenderer)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        }

        static void Blit(Texture2D source, int x, int y, int crop, int zoom, Texture2D target, int left, int bottom)
        {
            for (int row = 0; row < crop * zoom; row++)
            {
                for (int column = 0; column < crop * zoom; column++)
                {
                    int sx = x + column / zoom;
                    int sy = y + row / zoom;
                    Color pixel = sx >= 0 && sy >= 0 && sx < source.width && sy < source.height
                        ? source.GetPixel(sx, sy)
                        : Color.black;
                    target.SetPixel(left + column, bottom + row, pixel);
                }
            }
        }
    }

    // The units on the sheet: every entity his data holds today, and the spec's five units built in memory only
    public static class LookSheetUnits
    {
        public static readonly string[] Roster =
        {
            "Assets/Data/Entities/NormalEntity/NormalEntity.asset",
            "Assets/Data/Entities/FastShootEntity/FastShootEntity.asset",
            "Assets/Data/Entities/TripleShootEntity/TripleShootEntity.asset",
            "Assets/Data/Entities/MultiShotEntity/MultiShotEntity.asset",
            "Assets/Data/Entities/RandomShootEntity/RandomShootEntity.asset",
            "Assets/Data/Entities/ChainLightningEntity/ChainLightningEntity.asset",
            "Assets/Data/Entities/ChannelingEntity/ChannelingEntity.asset",
            "Assets/Data/Entities/SwarmEntity/SwarmEntity.asset",
            "Assets/Data/Entities/TestEntity/TestEntity.asset",
            "Assets/Data/Entities/SoldierEntity/SoldierEntity.asset",
            "Assets/Data/Entities/HitArmorBufferEntityEntity/HitArmorBufferEntity.asset"
        };

        public static readonly string[] Undesigned = { "Stormreed", "Puffball", "Old fern", "Needle stone", "Storm idol" };

        public static readonly string[] Families = { "DAMAGE", "HEAL", "ROT", "RENEW", "BOON", "BOON DEFENCE", "BANE" };

        static readonly string projectiles = "Assets/Prefabs/Projectiles/";
        static readonly string damagePath = "Assets/Data/Entities/NormalEntity/AttributeConsumerFactory.asset";
        static readonly string poisonPath = "Assets/Data/EntityItems/PoisonItem/BuffHandlerFactory.asset";
        static readonly string explosionPath = "Assets/Data/EntityItems/ExplodeOnHitItem/AreaOfEffectProjectileBehaviourFactory.asset";

        // His two enemies of today stand on the stone side, as the waves place them
        public static Entity.EntityType Side(EntityData data)
        {
            bool isEnemy = data.name == "SoldierEntity" || data.name == "HitArmorBufferEntity";
            return isEnemy ? Entity.EntityType.Computer : Entity.EntityType.Player;
        }

        public static Entity.EntityType Side(string unit)
        {
            bool isStone = unit == "Needle stone" || unit == "Storm idol";
            return isStone ? Entity.EntityType.Computer : Entity.EntityType.Player;
        }

        // An EntityData that lives in memory only, everything it creates goes in the list for the caller to destroy
        public static EntityData Create(string unit, List<Object> created)
        {
            EntityData normal = AssetDatabase.LoadAssetAtPath<EntityData>(Roster[0]);
            EntityData data = Track(ScriptableObject.CreateInstance<EntityData>(), created);
            data.name = unit;
            data.title = unit;
            data.model = normal.model;
            data.targetBehaviourType = TargetBehaviourType.First;
            data.targetValidators = new List<ATargetValidatorFactory>();
            data.passives = new List<ABuffHandlerFactory>();
            data.onHitEffects = new List<ABuffHandlerFactory>();
            data.skillFactories = new List<ASkillFactory>();
            data.attributes[AttributeType.HealthMax] = 100f;
            data.attributes[AttributeType.AttackRate] = 1f;
            data.attributes[AttributeType.Damage] = 4f;
            data.attributes[AttributeType.Range] = 100f;
            switch (unit)
            {
                case "Stormreed":
                    data.attributes[AttributeType.AttackRate] = 0.5f;
                    data.skillFactories.Add(Shoot("ChannelingLightning", 3, created));
                    break;
                case "Puffball":
                    data.attributes[AttributeType.HealthMax] = 200f;
                    AreaOfEffectSkillFactory pulse = Track(ScriptableObject.CreateInstance<AreaOfEffectSkillFactory>(), created);
                    AreaOfEffectProjectileBehaviourFactory explosion =
                        AssetDatabase.LoadAssetAtPath<AreaOfEffectProjectileBehaviourFactory>(explosionPath);
                    pulse.data = new AreaOfEffectSkillData
                    {
                        onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                        areaOfEffectPrefab = explosion.data.areaOfEffectPrefab.gameObject
                    };
                    data.skillFactories.Add(pulse);
                    break;
                case "Old fern":
                    data.attributes[AttributeType.HealthMax] = 300f;
                    ApplyConsumerOnTimeFactory tick = Track(ScriptableObject.CreateInstance<ApplyConsumerOnTimeFactory>(), created);
                    tick.data = new ApplyConsumerOnTimeData
                    {
                        onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                        consumerFactory = Flat(-3f, created),
                        rate = 2f
                    };
                    data.skillFactories.Add(tick);
                    break;
                case "Needle stone":
                    data.attributes[AttributeType.AttackRate] = 0.5f;
                    data.skillFactories.Add(Shoot("StraightLaserBullet", 2, created));
                    break;
                case "Storm idol":
                    data.attributes[AttributeType.HealthMax] = 400f;
                    data.skillFactories.Add(Shoot("ChainLightning", 1, created));
                    data.onHitEffects.Add(AssetDatabase.LoadAssetAtPath<ABuffHandlerFactory>(poisonPath));
                    break;
            }
            return data;
        }

        // A handler of the named family with no SpellLooks row, so its look is the derived one
        public static ABuffHandlerFactory Handler(string family, List<Object> created)
        {
            BuffHandlerFactory handler = Track(ScriptableObject.CreateInstance<BuffHandlerFactory>(), created);
            handler.data = new BuffHandlerData
            {
                durationType = DurationType.Duration,
                duration = 6f,
                buffFactoryList = new List<ABuffFactory>(),
                tags = new List<GameplayTag>()
            };
            switch (family)
            {
                case "DAMAGE":
                case "HEAL":
                    return null;
                case "ROT":
                    handler.data.isPeriodic = true;
                    handler.data.periodDuration = 2f;
                    handler.data.buffFactoryList.Add(Consume(10f, created));
                    break;
                case "RENEW":
                    handler.data.isPeriodic = true;
                    handler.data.periodDuration = 1f;
                    handler.data.buffFactoryList.Add(Consume(-4f, created));
                    break;
                case "BOON":
                    handler.data.buffFactoryList.Add(Modifier(AttributeType.Damage, 10f, created));
                    break;
                case "BOON DEFENCE":
                    handler.data.buffFactoryList.Add(Modifier(AttributeType.HitArmor, 2f, created));
                    break;
                default:
                    handler.data.buffFactoryList.Add(Modifier(AttributeType.Damage, -5f, created));
                    break;
            }
            return handler;
        }

        static ShootProjectileSkillFactory Shoot(string prefab, int perTarget, List<Object> created)
        {
            ShootProjectileSkillFactory shoot = Track(ScriptableObject.CreateInstance<ShootProjectileSkillFactory>(), created);
            ShootProjectileSkillData.ProjectileData entry = new ShootProjectileSkillData.ProjectileData
            {
                projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectiles + prefab + ".prefab"),
                onHitConsumer = new List<AConsumerFactory> { AssetDatabase.LoadAssetAtPath<AConsumerFactory>(damagePath) },
                numberOfProjectileToShootPerTarget = perTarget
            };
            shoot.data = new ShootProjectileSkillData
            {
                onSkillTriggerFactory = new List<AOnSkillTriggerFactory>(),
                projectiles = new List<ShootProjectileSkillData.ProjectileData> { entry }
            };
            return shoot;
        }

        static ConsumerFactory Flat(float value, List<Object> created)
        {
            ConsumerFactory consumer = Track(ScriptableObject.CreateInstance<ConsumerFactory>(), created);
            FlatValue flat = new FlatValue();
            flat.data = new FlatValueData { value = value };
            consumer.data = new ConsumerData { value = flat, ignoreDamageReduction = true };
            return consumer;
        }

        static ApplyConsumerBuffFactory Consume(float value, List<Object> created)
        {
            ApplyConsumerBuffFactory buff = Track(ScriptableObject.CreateInstance<ApplyConsumerBuffFactory>(), created);
            buff.data = new ApplyConsumerBuffData { consumerFactory = Flat(value, created) };
            return buff;
        }

        static FlatModifierFactory Modifier(AttributeType type, float value, List<Object> created)
        {
            FlatModifierFactory modifier = Track(ScriptableObject.CreateInstance<FlatModifierFactory>(), created);
            modifier.data = new FlatModifierData { type = type, modifierType = AttributeModifierType.Add, value = value };
            return modifier;
        }

        static ObjectType Track<ObjectType>(ObjectType created, List<Object> list) where ObjectType : Object
        {
            list.Add(created);
            return created;
        }
    }

    // A 5 by 7 pixel font, so the capture tool draws its own labels into the sheet
    public static class SheetFont
    {
        static readonly Dictionary<char, string> glyphs = new Dictionary<char, string>
        {
            { 'A', ".###.#...##...#######...##...##...#" },
            { 'B', "####.#...##...#####.#...##...#####." },
            { 'C', ".#####....#....#....#....#.....####" },
            { 'D', "####.#...##...##...##...##...#####." },
            { 'E', "######....#....####.#....#....#####" },
            { 'F', "######....#....####.#....#....#...." },
            { 'G', ".#####....#....#.####...##...#.###." },
            { 'H', "#...##...##...#######...##...##...#" },
            { 'I', "#####..#....#....#....#....#..#####" },
            { 'J', "..###...#....#....#....#.#..#..##.." },
            { 'K', "#...##..#.#.#..##...#.#..#..#.#...#" },
            { 'L', "#....#....#....#....#....#....#####" },
            { 'M', "#...###.###.#.##.#.##...##...##...#" },
            { 'N', "#...###..##.#.##..###...##...##...#" },
            { 'O', ".###.#...##...##...##...##...#.###." },
            { 'P', "####.#...##...#####.#....#....#...." },
            { 'Q', ".###.#...##...##...##.#.##..#..##.#" },
            { 'R', "####.#...##...#####.#.#..#..#.#...#" },
            { 'S', ".#####....#.....###.....#....#####." },
            { 'T', "#####..#....#....#....#....#....#.." },
            { 'U', "#...##...##...##...##...##...#.###." },
            { 'V', "#...##...##...##...##...#.#.#...#.." },
            { 'W', "#...##...##...##.#.##.#.###.###...#" },
            { 'X', "#...##...#.#.#...#...#.#.#...##...#" },
            { 'Y', "#...##...#.#.#...#....#....#....#.." },
            { 'Z', "#####....#...#...#...#...#....#####" },
            { '0', ".###.#...##..###.#.###..##...#.###." },
            { '1', "..#...##....#....#....#....#...###." },
            { '2', ".###.#...#....#...#...#...#...#####" },
            { '3', "####.....#....#.###.....#....#####." },
            { '4', "...#...##..#.#.#..#.#####...#....#." },
            { '5', "######....####.....#....##...#.###." },
            { '6', ".###.#....#....####.#...##...#.###." },
            { '7', "#####....#...#...#...#....#....#..." },
            { '8', ".###.#...##...#.###.#...##...#.###." },
            { '9', ".###.#...##...#.####....#....#.###." },
            { '-', "...............#####..............." },
            { '(', "...#...#...#....#....#.....#.....#." },
            { ')', ".#.....#.....#....#....#...#...#..." },
            { '/', "....#...#....#..#....#....#...#...." },
            { '.', "...........................##...##." },
            { '+', ".......#....#..#####..#....#......." },
            { ' ', "..................................." }
        };

        // Draws text with its top-left corner at the pixel, the texture's origin being bottom-left
        public static void Draw(Texture2D texture, string text, int left, int top, int scale, Color colour)
        {
            int x = left;
            foreach (char character in text.ToUpperInvariant())
            {
                string glyph = glyphs.ContainsKey(character) ? glyphs[character] : glyphs[' '];
                for (int row = 0; row < 7; row++)
                {
                    for (int column = 0; column < 5; column++)
                    {
                        if (glyph[row * 5 + column] == '#')
                        {
                            Fill(texture, x + column * scale, top - (row + 1) * scale, scale, colour);
                        }
                    }
                }
                x += 6 * scale;
            }
        }

        static void Fill(Texture2D texture, int x, int y, int scale, Color colour)
        {
            for (int dy = 0; dy < scale; dy++)
            {
                for (int dx = 0; dx < scale; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && py >= 0 && px < texture.width && py < texture.height)
                    {
                        texture.SetPixel(px, py, colour);
                    }
                }
            }
        }
    }
}

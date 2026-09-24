using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Clears the board and lays the sheet's units one per cell, in batches the portrait frame holds, spawned through
    // the game's EntityManager so the RenderManager builds their views. Each batch renders at the board and the
    // portrait camera, on the grass and on a flat ground of the grass's body colour, and writes colour, greyscale and
    // deuteranopia frames plus a labelled contact sheet per ground and camera. The unit pass also renders each unit
    // alone and lists the silhouettes that overlap; the effect pass applies the spells through the SpellVisualSink
    public class LookSheetRun : AStageRun
    {
        public static readonly int Width = StageCalibration.PortraitWidth;
        public static readonly int Height = StageCalibration.PortraitHeight;
        public static readonly int Columns = 4;
        public static readonly int Rows = 4;
        // Whole cells between two units, so every unit lands on a cell centre
        public static readonly int Spacing = 3;
        // A crop spans this many cells across and up, enough for the longest roots and the tallest stem
        public static readonly float CropCells = 2.8f;
        // Where the unit's foot sits in its mask window, from the bottom
        public static readonly float FootHeight = 0.4f;
        public static readonly int MaskTolerance = 24;
        public static readonly int ClosestPairs = 10;
        public static readonly float[] Moments = { 0.3f, 2.5f };
        public static readonly string Folder = StagePlay.CaptureFolder + "look-sheets/";

        static readonly string[] cameras = { "board", "portrait" };
        static readonly DeliveryStyle[] styles =
        {
            DeliveryStyle.Direct, DeliveryStyle.Arc, DeliveryStyle.Rigid, DeliveryStyle.Swarm, DeliveryStyle.Bounce,
            DeliveryStyle.ChainSync, DeliveryStyle.Thrown
        };

        bool _isUnits;
        bool _isEffects;
        int _failures;
        Camera _flatCamera;
        BattleFocus _focus;
        readonly List<Object> _created = new List<Object>();
        readonly List<Transform> _cells = new List<Transform>();
        readonly List<string> _labels = new List<string>();
        readonly List<GameObject> _spawned = new List<GameObject>();
        readonly Dictionary<string, LookSheetContact> _contacts = new Dictionary<string, LookSheetContact>();
        readonly Dictionary<string, List<bool[]>> _masks = new Dictionary<string, List<bool[]>>();
        readonly Dictionary<string, List<string>> _maskLabels = new Dictionary<string, List<string>>();

        // What one effect cell shows, and what the round must take back
        class EffectCell
        {
            public string label;
            public string spell;
            public bool isOnStone;
            public bool isDelivery;
            public DeliveryStyle style;
            public ABuffHandlerFactory handler;
            public GameObject giver;
            public GameObject projectile;
            public int token;
        }

        public LookSheetRun(bool isUnits, bool isEffects)
        {
            _isUnits = isUnits;
            _isEffects = isEffects;
        }

        protected override IEnumerator Run()
        {
            _manager.SetLandscape(false);
            yield return Wait(0.5f);
            ClearBoard();
            yield return NextFrame();

            // The capture holds the camera still, the focus would ease it back to the overview every frame
            _focus = Object.FindAnyObjectByType<BattleFocus>();
            if (_focus != null)
            {
                _focus.enabled = false;
            }

            GameObject flatGo = new GameObject("LookSheetFlatCamera");
            _created.Add(flatGo);
            _flatCamera = flatGo.AddComponent<Camera>();
            _flatCamera.enabled = false;
            Directory.CreateDirectory(Folder);

            if (_isUnits)
            {
                yield return RunUnits();
                WriteCollisions();
            }

            if (_isEffects)
            {
                yield return RunEffects();
            }

            foreach (KeyValuePair<string, LookSheetContact> pair in _contacts)
            {
                Texture2D contact = pair.Value.Build(pair.Key.Replace("-", " "));
                Write(Folder + pair.Key + "-contact.png", contact);
            }

            Restore();
            Debug.Log($"[LookSheetRun] Sheets {_contacts.Count} failures {_failures} in {Folder}");
            LookSheetCapture.Finish(_failures == 0 && _contacts.Count > 0);
        }

        #region Units

        IEnumerator RunUnits()
        {
            List<string> units = new List<string>(LookSheetUnits.Roster);
            units.AddRange(LookSheetUnits.Proposed);
            units.AddRange(LookSheetUnits.Undesigned);
            units.Add("Healer");
            int batchSize = Columns * Rows;
            for (int start = 0; start < units.Count; start += batchSize)
            {
                List<string> batch = units.GetRange(start, Mathf.Min(batchSize, units.Count - start));
                List<Vector3> cells = Cells(batch.Count);
                for (int i = 0; i < batch.Count; i++)
                {
                    if (batch[i] == "Healer")
                    {
                        PlaceHealer(cells[i], true);
                    }
                    else
                    {
                        EntityData data = LookSheetUnits.Create(batch[i], _created);
                        Spawn(data, LookSheetUnits.Side(batch[i]), cells[i], LookSheetUnits.Label(batch[i]), true);
                    }
                }

                HideHud();
                yield return Wait(1.2f);
                string name = "units-b" + (start / batchSize + 1);
                foreach (string camera in cameras)
                {
                    yield return Aim(camera);
                    CaptureSheet("units", name, camera, "");
                    Masks(camera);
                }

                ClearBatch();
                yield return NextFrame();
            }
        }

        // Each unit rendered alone on the flat ground, its mask taken against the same frame without it
        void Masks(string camera)
        {
            if (!_masks.ContainsKey(camera))
            {
                _masks[camera] = new List<bool[]>();
                _maskLabels[camera] = new List<string>();
            }

            int window = CropSize();
            Dictionary<Renderer, ShadowCastingMode> shown = new Dictionary<Renderer, ShadowCastingMode>();
            foreach (Transform cell in _cells)
            {
                foreach (Renderer renderer in cell.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.enabled)
                    {
                        shown[renderer] = renderer.shadowCastingMode;
                        renderer.enabled = false;
                    }
                }
            }

            Color32[] empty = CaptureFlat();
            for (int i = 0; i < _cells.Count; i++)
            {
                List<Renderer> own = new List<Renderer>();
                foreach (Renderer renderer in _cells[i].GetComponentsInChildren<Renderer>(true))
                {
                    if (shown.ContainsKey(renderer))
                    {
                        renderer.enabled = true;
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        own.Add(renderer);
                    }
                }

                Color32[] alone = CaptureFlat();
                Vector2Int corner = MaskCorner(_cells[i].position, window);
                Color32[] cellPixels = LookSheetImage.Crop(alone, Width, Height, corner.x, corner.y, window);
                Color32[] groundPixels = LookSheetImage.Crop(empty, Width, Height, corner.x, corner.y, window);
                bool[] mask = SilhouetteOverlap.Mask(cellPixels, groundPixels, MaskTolerance);
                _masks[camera].Add(mask);
                _maskLabels[camera].Add(_labels[i]);
                Debug.Log($"[LookSheetRun] Mask {camera} {_labels[i]} {SilhouetteOverlap.Count(mask)} of {mask.Length} px");
                foreach (Renderer renderer in own)
                {
                    renderer.enabled = false;
                    renderer.shadowCastingMode = shown[renderer];
                }
            }

            foreach (KeyValuePair<Renderer, ShadowCastingMode> pair in shown)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = true;
                }
            }
        }

        // The window's bottom-left corner, centred across on the foot with the foot FootHeight up
        Vector2Int MaskCorner(Vector3 foot, int window)
        {
            Vector3 viewport = _manager.gameCamera.WorldToViewportPoint(foot);
            int x = Mathf.RoundToInt(viewport.x * Width) - window / 2;
            int y = Mathf.RoundToInt(viewport.y * Height) - Mathf.RoundToInt(window * FootHeight);
            return new Vector2Int(x, y);
        }

        void WriteCollisions()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine($"# Silhouette pairs above IoU {SilhouetteOverlap.CollisionThreshold}, each unit rendered alone on the flat ground");
            foreach (string camera in cameras)
            {
                if (!_masks.ContainsKey(camera))
                {
                    continue;
                }

                List<SilhouetteOverlap.Pair> pairs = SilhouetteOverlap.Collisions(_masks[camera], SilhouetteOverlap.CollisionThreshold);
                List<string> labels = _maskLabels[camera];
                text.AppendLine();
                text.AppendLine($"## {camera} camera, {labels.Count} units, {pairs.Count} pairs");
                foreach (SilhouetteOverlap.Pair pair in pairs)
                {
                    text.AppendLine($"{pair.iou:0.000}  {labels[pair.first]}  {labels[pair.second]}");
                }

                // The nearest pairs under the threshold too, so a list with no collision still says how close it came
                List<SilhouetteOverlap.Pair> closest = SilhouetteOverlap.Collisions(_masks[camera], 0f);
                text.AppendLine($"### {camera} camera, the {Mathf.Min(ClosestPairs, closest.Count)} closest pairs");
                for (int i = 0; i < Mathf.Min(ClosestPairs, closest.Count); i++)
                {
                    text.AppendLine($"{closest[i].iou:0.000}  {labels[closest[i].first]}  {labels[closest[i].second]}");
                }
                Debug.Log($"[LookSheetRun] Collisions {camera} {pairs.Count}");
            }

            File.WriteAllText(Folder + "collisions.txt", text.ToString());
        }

        #endregion

        #region Effects

        IEnumerator RunEffects()
        {
            List<EffectCell> plants = new List<EffectCell>();
            foreach (string spell in LookSheetSpells.Spells)
            {
                plants.Add(new EffectCell { spell = spell, label = LookSheetSpells.Label(spell, false) });
            }

            List<EffectCell> stones = new List<EffectCell>();
            foreach (string spell in LookSheetSpells.OnStone)
            {
                stones.Add(new EffectCell { spell = spell, isOnStone = true, label = LookSheetSpells.Label(spell, true) });
            }

            foreach (DeliveryStyle style in styles)
            {
                stones.Add(new EffectCell { isDelivery = true, style = style, label = style.ToString() });
            }

            List<List<EffectCell>> batches = new List<List<EffectCell>> { plants, stones };
            for (int b = 0; b < batches.Count; b++)
            {
                List<EffectCell> batch = batches[b];
                List<Vector3> cells = Cells(batch.Count + 1);
                if (!SpawnEffectCells(batch, cells))
                {
                    ClearBatch();
                    continue;
                }

                // The healer stands in the last cell, so its heal links start clear of the other cells
                PlaceHealer(cells[cells.Count - 1], false);
                HideHud();
                yield return Wait(1.2f);
                foreach (string camera in cameras)
                {
                    yield return Aim(camera);
                    float start = Time.time;
                    Apply(batch);
                    foreach (float moment in Moments)
                    {
                        while (Time.time < start + moment)
                        {
                            yield return NextFrame();
                        }

                        string time = "t" + Mathf.RoundToInt(moment * 10f).ToString("00");
                        CaptureSheet("effects", "effects-b" + (b + 1), camera, time);
                    }

                    TakeBack(batch);
                    yield return Wait(1.5f);
                }

                ClearBatch();
                yield return NextFrame();
            }
        }

        bool SpawnEffectCells(List<EffectCell> batch, List<Vector3> cells)
        {
            EntityData plant = LookSheetData.LoadEntity("NormalEntity");
            EntityData stone = LookSheetData.LoadEntity("SoldierEntity");
            float size = _manager.player.grid.size;
            for (int i = 0; i < batch.Count; i++)
            {
                EffectCell cell = batch[i];
                EntityData data = cell.isOnStone ? stone : plant;
                if (cell.spell == "Sprout")
                {
                    data = LookSheetSpells.Sapling(_created);
                }

                Entity.EntityType side = cell.isOnStone ? Entity.EntityType.Computer : Entity.EntityType.Player;
                if (Spawn(data, side, cells[i], cell.label, true) == null)
                {
                    return false;
                }

                cell.handler = cell.spell != null ? LookSheetSpells.Handler(cell.spell, _created) : null;
                if (cell.spell == "Transfusion")
                {
                    // The giver stands one cell to the left, inside the receiver's crop
                    Vector3 left = _manager.player.grid.GetNearestWalkablePosition(cells[i] + Vector3.left * size);
                    cell.giver = Spawn(plant, Entity.EntityType.Player, left, "GIVER", false);
                }
            }
            return true;
        }

        // Everything lands at once, so the two moments are measured from the same start
        void Apply(List<EffectCell> batch)
        {
            ISpellVisualSink sink = _manager.spellSink;
            GameObject healerGo = _manager.player.character != null ? _manager.player.character.gameObject : null;
            for (int i = 0; i < batch.Count; i++)
            {
                EffectCell cell = batch[i];
                GameObject target = _cells[i].gameObject;
                if (cell.isDelivery)
                {
                    BeginDelivery(cell, _cells[i]);
                    continue;
                }

                if (cell.handler != null)
                {
                    float duration = cell.handler.durationType == DurationType.Duration ? cell.handler.duration : float.PositiveInfinity;
                    sink.SetStatus(healerGo, target, cell.handler, LookSheetSpells.Stacks(cell.spell), 0f, duration);
                }

                float impact = LookSheetSpells.Impact(cell.spell);
                if (impact != 0f)
                {
                    sink.ShowImpact(healerGo, target, ResourceKind.Health, impact, false);
                }

                if (cell.giver != null)
                {
                    sink.ShowImpact(healerGo, cell.giver, ResourceKind.Health, -impact, false);
                }
            }
        }

        void BeginDelivery(EffectCell cell, Transform target)
        {
            IDeliverySource source = target.GetComponentInChildren<IDeliverySource>();
            GameObject projectileGo = new GameObject("LookSheetProjectile");
            _created.Add(projectileGo);
            Vector3 end = target.position + new Vector3(1.2f, 0.4f, 1.2f);
            projectileGo.transform.position = Vector3.Lerp(target.position + Vector3.up, end, 0.6f);
            cell.projectile = projectileGo;
            cell.token = _manager.NextDeliveryToken();
            bool isBegun = source != null && source.BeginDelivery(cell.token, cell.style, projectileGo.transform, end);
            Debug.Log($"[LookSheetRun] Delivery {cell.style} begun {isBegun}");
        }

        void TakeBack(List<EffectCell> batch)
        {
            ISpellVisualSink sink = _manager.spellSink;
            GameObject healerGo = _manager.player.character != null ? _manager.player.character.gameObject : null;
            for (int i = 0; i < batch.Count; i++)
            {
                EffectCell cell = batch[i];
                if (cell.handler != null)
                {
                    sink.RemoveStatus(healerGo, _cells[i].gameObject, cell.handler);
                }

                if (cell.isDelivery)
                {
                    IDeliverySource source = _cells[i].GetComponentInChildren<IDeliverySource>();
                    if (source != null)
                    {
                        source.EndDelivery(cell.token);
                    }
                }
            }
        }

        #endregion

        #region Board

        // The first wave is already on the board, its cells are taken back for the sheet
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

        void ClearBatch()
        {
            foreach (GameObject entityGo in _spawned)
            {
                if (entityGo != null)
                {
                    _manager.entityManager.DestroyEntity(entityGo, entityGo.GetComponent<Entity>().entityType);
                }
            }

            _spawned.Clear();
            _cells.Clear();
            _labels.Clear();
        }

        GameObject Spawn(EntityData data, Entity.EntityType side, Vector3 position, string label, bool isCell)
        {
            GameObject entityGo = data != null ? _manager.entityManager.SpawnEntity(data, position, side) : null;
            if (entityGo == null)
            {
                Debug.LogError($"[LookSheetRun] {label} refused at {position}");
                _failures++;
                return null;
            }

            _spawned.Add(entityGo);
            if (isCell)
            {
                _cells.Add(entityGo.transform);
                _labels.Add(label);
            }
            return entityGo;
        }

        void PlaceHealer(Vector3 position, bool isCell)
        {
            Character character = _manager.player.character;
            if (character == null)
            {
                Debug.LogError("[LookSheetRun] The player has no character to place");
                _failures++;
                return;
            }

            character.transform.position = position;
            if (isCell)
            {
                _cells.Add(character.transform);
                _labels.Add("HEALER");
            }
        }

        // Every unit on the board, the Transfusion giver included, draws without its health bar
        void HideHud()
        {
            List<Transform> shown = new List<Transform>(_cells);
            foreach (GameObject entityGo in _spawned)
            {
                shown.Add(entityGo.transform);
            }

            foreach (Transform unit in shown)
            {
                foreach (Canvas canvas in unit.GetComponentsInChildren<Canvas>(true))
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
            camera.transform.SetPositionAndRotation(_manager.overviewPose.position, _manager.overviewPose.rotation);
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

        #endregion

        #region Capture

        // The camera takes the named pose and holds it a few frames, so the grass culls against it
        IEnumerator Aim(string camera)
        {
            Camera game = _manager.gameCamera;
            game.aspect = (float)Width / Height;
            Pose pose = _manager.overviewPose;
            if (camera == "board")
            {
                pose = LookSheetCamera.Board(_manager.board.center, _manager.player.grid.size, pose.rotation, game.fieldOfView,
                                             game.aspect, Width);
            }

            game.transform.SetPositionAndRotation(pose.position, pose.rotation);
            _manager.look.UpdateFog(StageCalibration.BackgroundFog(pose.position, _manager.board));
            for (int i = 0; i < 3; i++)
            {
                yield return NextFrame();
            }

            Debug.Log($"[LookSheetRun] Camera {camera} draws a cell at the board centre {MeasuredCellPixels():0.0} px wide");
        }

        float MeasuredCellPixels()
        {
            Camera game = _manager.gameCamera;
            Vector3 centre = game.WorldToViewportPoint(_manager.board.center);
            Vector3 across = game.WorldToViewportPoint(_manager.board.center + Vector3.right * _manager.player.grid.size);
            return (across.x - centre.x) * Width;
        }

        int CropSize()
        {
            return Mathf.Max(8, Mathf.RoundToInt(MeasuredCellPixels() * CropCells));
        }

        // Grass and flat frames of the same moment, each written in colour, greyscale and deuteranopia, and cropped
        // into the contact sheet of its ground and camera
        void CaptureSheet(string sheet, string name, string camera, string time)
        {
            string suffix = time == "" ? "" : "-" + time;
            Color32[] grass = Capture(_manager.gameCamera);
            Color32[] flat = CaptureFlat();
            WriteGround(sheet, name, camera, "grass", suffix, grass);
            WriteGround(sheet, name, camera, "flat", suffix, flat);
        }

        void WriteGround(string sheet, string name, string camera, string ground, string suffix, Color32[] colour)
        {
            Color32[] grey = LookSheetImage.Greyscale(colour);
            Color32[] deuteranope = LookSheetImage.Deuteranope(colour);
            string root = Folder + name + "-" + ground + "-" + camera + suffix;
            Write(root + "-colour.png", LookSheetImage.ToTexture(colour, Width, Height));
            Write(root + "-grey.png", LookSheetImage.ToTexture(grey, Width, Height));
            Write(root + "-deuteranopia.png", LookSheetImage.ToTexture(deuteranope, Width, Height));

            int crop = CropSize();
            string key = sheet + "-" + ground + "-" + camera + suffix;
            if (!_contacts.ContainsKey(key))
            {
                _contacts[key] = new LookSheetContact(crop);
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                Vector3 viewport = _manager.gameCamera.WorldToViewportPoint(Centre(_cells[i]));
                int x = Mathf.RoundToInt(viewport.x * Width) - crop / 2;
                int y = Mathf.RoundToInt(viewport.y * Height) - crop / 2;
                _contacts[key].Add(_labels[i], LookSheetImage.Crop(colour, Width, Height, x, y, crop),
                                   LookSheetImage.Crop(grey, Width, Height, x, y, crop),
                                   LookSheetImage.Crop(deuteranope, Width, Height, x, y, crop));
            }
        }

        // The frame as the game camera draws it, rendered into a texture
        static Color32[] Capture(Camera camera)
        {
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
            Color32[] pixels = texture.GetPixels32();
            Object.Destroy(texture);
            return pixels;
        }

        // The same frame without grass: the grass only draws for the game camera, so a copy of it sees the ground,
        // painted for this render in the grass's body colour with its cell grid off
        Color32[] CaptureFlat()
        {
            Camera game = _manager.gameCamera;
            _flatCamera.CopyFrom(game);
            _flatCamera.GetUniversalAdditionalCameraData().renderPostProcessing = game.GetUniversalAdditionalCameraData().renderPostProcessing;
            Renderer ground = _manager.boardGround;
            MaterialPropertyBlock saved = new MaterialPropertyBlock();
            MaterialPropertyBlock flat = new MaterialPropertyBlock();
            if (ground != null)
            {
                ground.GetPropertyBlock(saved);
                ground.GetPropertyBlock(flat);
                flat.SetColor(RenderObjects.BaseColorId, _manager.grass.lookMaterial.GetColor(RenderObjects.BaseColorId));
                flat.SetFloat("_HLGroundGrid", 0f);
                ground.SetPropertyBlock(flat);
            }

            _flatCamera.enabled = true;
            Color32[] pixels = Capture(_flatCamera);
            _flatCamera.enabled = false;
            if (ground != null)
            {
                ground.SetPropertyBlock(saved);
            }
            return pixels;
        }

        // The middle of what the unit draws, the hidden game model aside
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
            return bounds.center;
        }

        void Write(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.Destroy(texture);
            if (!File.Exists(path))
            {
                Debug.LogError($"[LookSheetRun] {path} was not written");
                _failures++;
            }
        }

        void Restore()
        {
            ClearBatch();
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }
            _created.Clear();

            if (_focus != null)
            {
                _focus.enabled = true;
            }

            Pose pose = _manager.overviewPose;
            _manager.gameCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            _manager.look.UpdateFog(StageCalibration.BackgroundFog(pose.position, _manager.board));
        }

        #endregion
    }
}

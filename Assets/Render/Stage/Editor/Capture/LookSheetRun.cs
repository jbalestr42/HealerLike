using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    // Clears the board and lays the sheet's units one per cell, in batches the portrait frame holds, spawned through
    // the game's EntityManager so the RenderManager builds their views. Each batch renders at the board and the
    // portrait camera, on the grass and on a flat ground of the grass's body colour; LookSheetUnitPass and
    // LookSheetEffectPass fill the batches, LookSheetOutput writes what they capture.
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
        public static readonly string Folder = StagePlay.CaptureFolder + "look-sheets/";
        public static readonly string[] Cameras = { "board", "portrait" };
        // Cells stay inside this part of the portrait frame, clear of its edges and the HUD at the top
        static readonly Rect frameCells = new Rect(0.05f, 0.05f, 0.9f, 0.85f);

        bool _isUnits;
        bool _isEffects;
        int _failures;
        Camera _flatCamera;
        BattleFocus _focus;
        readonly List<GameObject> _spawned = new List<GameObject>();

        readonly List<Object> _created = new List<Object>();
        public List<Object> created { get { return _created; } }

        readonly List<Transform> _cells = new List<Transform>();
        public List<Transform> cells { get { return _cells; } }

        readonly List<string> _labels = new List<string>();
        public List<string> labels { get { return _labels; } }

        readonly LookSheetOutput _output = new LookSheetOutput();
        public LookSheetOutput output { get { return _output; } }

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
                yield return new LookSheetUnitPass(this).Run();
                _output.WriteCollisions();
            }

            if (_isEffects)
            {
                yield return new LookSheetEffectPass(this).Run();
            }

            int sheets = _output.WriteContacts();
            Restore();
            int failures = _failures + _output.failures;
            Debug.Log($"[LookSheetRun] Sheets {sheets} failures {failures} in {Folder}");
            StagePlay.Finish(this, failures == 0 && sheets > 0);
        }

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

        public void ClearBatch()
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

        public GameObject Spawn(EntityData data, Entity.EntityType side, Vector3 position, string label, bool isCell)
        {
            GameObject entityGo = null;
            if (data != null)
            {
                entityGo = _manager.entityManager.SpawnEntity(data, position, side);
            }

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

        public void PlaceHealer(Vector3 position, bool isCell)
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
        public void HideHud()
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
        public List<Vector3> Cells(int count)
        {
            GridManager grid = _manager.player.grid;
            Camera camera = _manager.gameCamera;
            camera.aspect = (float)Width / Height;
            camera.transform.SetPositionAndRotation(_manager.overviewPose.position, _manager.overviewPose.rotation);
            Vector3 centre = _manager.board.center;
            float step = Spacing * grid.size;
            int rows = Mathf.CeilToInt((float)count / Columns);
            List<Vector3> cells = new List<Vector3>();
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < Columns && cells.Count < count; column++)
                {
                    Vector3 offset = new Vector3(column - (Columns - 1) * 0.5f, 0f, (rows - 1) * 0.5f - row) * step;
                    // Rounded onto a cell centre first, so no position sits between two cells
                    Vector3 point = centre + offset - _manager.board.min;
                    point.x = (Mathf.Floor(point.x / grid.size) + 0.5f) * grid.size;
                    point.z = (Mathf.Floor(point.z / grid.size) + 0.5f) * grid.size;
                    Vector3 cell = grid.GetNearestWalkablePosition(point + _manager.board.min);
                    Vector3 viewport = camera.WorldToViewportPoint(cell);
                    if (!frameCells.Contains(viewport))
                    {
                        Debug.LogError($"[LookSheetRun] Cell {cell} falls outside the portrait frame at {viewport}");
                    }
                    cells.Add(cell);
                }
            }
            return cells;
        }

        // The camera takes the named pose and holds it a few frames, so the grass culls against it
        public IEnumerator Aim(string camera)
        {
            Camera game = _manager.gameCamera;
            game.aspect = (float)Width / Height;
            Pose pose = _manager.overviewPose;
            if (camera == "board")
            {
                float cellSize = _manager.player.grid.size;
                pose = LookSheetCamera.Board(_manager.board.center, cellSize, pose.rotation, game.fieldOfView,
                                             game.aspect, Width);
            }

            game.transform.SetPositionAndRotation(pose.position, pose.rotation);
            _manager.look.UpdateFog(StageCalibration.BackgroundFog(pose.position, _manager.board));
            for (int i = 0; i < 3; i++)
            {
                yield return NextFrame();
            }

            float cellPixels = MeasuredCellPixels();
            Debug.Log($"[LookSheetRun] Camera {camera} draws a cell at the board centre {cellPixels:0.0} px wide");
        }

        float MeasuredCellPixels()
        {
            return LookSheetCamera.CellPixels(_manager.gameCamera, _manager.board.center, _manager.player.grid.size,
                                              Width);
        }

        public int CropSize()
        {
            return Mathf.Max(8, Mathf.RoundToInt(MeasuredCellPixels() * CropCells));
        }

        public Vector2Int ToPixel(Vector3 world)
        {
            return LookSheetCamera.ToPixel(_manager.gameCamera, world, Width, Height);
        }

        // Grass and flat frames of the same moment, handed to the output for its sheet
        public void CaptureSheet(string sheet, string name, string camera, string time)
        {
            string suffix = time == "" ? "" : "-" + time;
            Color32[] grass = LookSheetCamera.Render(_manager.gameCamera, Width, Height);
            Color32[] flat = CaptureFlat();
            int crop = CropSize();
            List<Vector2Int> centres = new List<Vector2Int>();
            foreach (Transform cell in _cells)
            {
                centres.Add(ToPixel(LookSheetCamera.DrawnCentre(cell)));
            }

            _output.WriteGround(sheet, name, camera, "grass", suffix, grass, crop, centres, _labels);
            _output.WriteGround(sheet, name, camera, "flat", suffix, flat, crop, centres, _labels);
        }

        // The same frame without grass, on the ground in the grass's body colour
        public Color32[] CaptureFlat()
        {
            Color grassColour = _manager.grass.lookMaterial.GetColor(RenderObjects.BaseColorId);
            return LookSheetCamera.RenderFlat(_flatCamera, _manager.gameCamera, _manager.boardGround, grassColour,
                                              Width, Height);
        }

        void Restore()
        {
            ClearBatch();
            foreach (Object item in _created)
            {
                if (item != null)
                {
                    Object.Destroy(item);
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
    }
}

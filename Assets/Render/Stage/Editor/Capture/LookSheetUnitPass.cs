using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stage
{
    // The unit sheets: every roster unit, the proposed and the undesigned ones and the healer, one per cell in
    // batches; each batch is captured at both cameras, then each unit alone for its silhouette mask
    public class LookSheetUnitPass
    {
        // Where the unit's foot sits in its mask window, from the bottom
        public static readonly float FootHeight = 0.4f;
        static readonly float settleSeconds = 1.2f;

        LookSheetRun _run;

        public LookSheetUnitPass(LookSheetRun run)
        {
            _run = run;
        }

        public IEnumerator Run()
        {
            List<string> units = new List<string>(LookSheetUnits.Roster);
            units.AddRange(LookSheetUnits.Proposed);
            units.AddRange(LookSheetUnits.Undesigned);
            units.Add("Healer");
            int batchSize = LookSheetRun.Columns * LookSheetRun.Rows;
            for (int start = 0; start < units.Count; start += batchSize)
            {
                List<string> batch = units.GetRange(start, Mathf.Min(batchSize, units.Count - start));
                List<Vector3> cells = _run.Cells(batch.Count);
                for (int i = 0; i < batch.Count; i++)
                {
                    if (batch[i] == "Healer")
                    {
                        _run.PlaceHealer(cells[i], true);
                    }
                    else
                    {
                        EntityData data = LookSheetUnits.Create(batch[i], _run.created);
                        string label = LookSheetUnits.Label(batch[i]);
                        _run.Spawn(data, LookSheetUnits.Side(batch[i]), cells[i], label, true);
                    }
                }

                _run.HideHud();
                yield return AStageRun.Wait(settleSeconds);
                string name = "units-b" + (start / batchSize + 1);
                foreach (string camera in LookSheetRun.Cameras)
                {
                    yield return _run.Aim(camera);
                    _run.CaptureSheet("units", name, camera, "");
                    Masks(camera);
                }

                _run.ClearBatch();
                yield return _run.NextFrame();
            }
        }

        // Each unit rendered alone on the flat ground, its mask taken against the same frame without it
        void Masks(string camera)
        {
            List<Transform> cells = _run.cells;
            int window = _run.CropSize();
            Dictionary<Renderer, ShadowCastingMode> shown = new Dictionary<Renderer, ShadowCastingMode>();
            foreach (Transform cell in cells)
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

            Color32[] empty = _run.CaptureFlat();
            for (int i = 0; i < cells.Count; i++)
            {
                List<Renderer> own = new List<Renderer>();
                foreach (Renderer renderer in cells[i].GetComponentsInChildren<Renderer>(true))
                {
                    if (shown.ContainsKey(renderer))
                    {
                        renderer.enabled = true;
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        own.Add(renderer);
                    }
                }

                Color32[] alone = _run.CaptureFlat();
                Vector2Int corner = MaskCorner(cells[i].position, window);
                _run.output.AddMask(camera, _run.labels[i], alone, empty, corner, window);
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
            Vector2Int pixel = _run.ToPixel(foot);
            return new Vector2Int(pixel.x - window / 2, pixel.y - Mathf.RoundToInt(window * FootHeight));
        }
    }
}

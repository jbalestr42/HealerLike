using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // What a sheet run writes: each frame in colour, greyscale and deuteranopia, the labelled contact sheets built
    // from their crops, the silhouette masks of the units rendered alone, and the collision text read from them
    public class LookSheetOutput
    {
        public static readonly int MaskTolerance = 24;
        public static readonly int ClosestPairs = 10;

        readonly Dictionary<string, LookSheetContact> _contacts = new Dictionary<string, LookSheetContact>();
        readonly Dictionary<string, List<bool[]>> _masks = new Dictionary<string, List<bool[]>>();
        readonly Dictionary<string, List<string>> _maskLabels = new Dictionary<string, List<string>>();

        int _failures;
        public int failures { get { return _failures; } }

        // One ground of one moment: its three frames, and a crop of each cell into the contact sheet of its ground
        // and camera; centres are where each cell's unit draws, in pixels
        public void WriteGround(string sheet, string name, string camera, string ground, string suffix,
                                Color32[] colour, int crop, List<Vector2Int> centres, List<string> labels)
        {
            int width = LookSheetRun.Width;
            int height = LookSheetRun.Height;
            Color32[] grey = LookSheetImage.Greyscale(colour);
            Color32[] deuteranope = LookSheetImage.Deuteranope(colour);
            string root = LookSheetRun.Folder + name + "-" + ground + "-" + camera + suffix;
            Write(root + "-colour.png", LookSheetImage.ToTexture(colour, width, height));
            Write(root + "-grey.png", LookSheetImage.ToTexture(grey, width, height));
            Write(root + "-deuteranopia.png", LookSheetImage.ToTexture(deuteranope, width, height));

            string key = sheet + "-" + ground + "-" + camera + suffix;
            if (!_contacts.ContainsKey(key))
            {
                _contacts[key] = new LookSheetContact(crop);
            }

            for (int i = 0; i < centres.Count; i++)
            {
                int x = centres[i].x - crop / 2;
                int y = centres[i].y - crop / 2;
                _contacts[key].Add(labels[i], LookSheetImage.Crop(colour, width, height, x, y, crop),
                                   LookSheetImage.Crop(grey, width, height, x, y, crop),
                                   LookSheetImage.Crop(deuteranope, width, height, x, y, crop));
            }
        }

        // Returns how many contact sheets it wrote
        public int WriteContacts()
        {
            foreach (KeyValuePair<string, LookSheetContact> pair in _contacts)
            {
                Texture2D contact = pair.Value.Build(pair.Key.Replace("-", " "));
                Write(LookSheetRun.Folder + pair.Key + "-contact.png", contact);
            }
            return _contacts.Count;
        }

        // The unit's mask in its window: the frame with it alone against the same frame without it
        public void AddMask(string camera, string label, Color32[] alone, Color32[] empty, Vector2Int corner,
                            int window)
        {
            if (!_masks.ContainsKey(camera))
            {
                _masks[camera] = new List<bool[]>();
                _maskLabels[camera] = new List<string>();
            }

            int width = LookSheetRun.Width;
            int height = LookSheetRun.Height;
            Color32[] cellPixels = LookSheetImage.Crop(alone, width, height, corner.x, corner.y, window);
            Color32[] groundPixels = LookSheetImage.Crop(empty, width, height, corner.x, corner.y, window);
            bool[] mask = SilhouetteOverlap.Mask(cellPixels, groundPixels, MaskTolerance);
            _masks[camera].Add(mask);
            _maskLabels[camera].Add(label);
            Debug.Log($"[LookSheetOutput] Mask {camera} {label} {SilhouetteOverlap.Count(mask)} of {mask.Length} px");
        }

        public void WriteCollisions()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine($"# Silhouette pairs above IoU {SilhouetteOverlap.CollisionThreshold}, "
                            + "each unit rendered alone on the flat ground");
            foreach (string camera in LookSheetRun.Cameras)
            {
                if (!_masks.ContainsKey(camera))
                {
                    continue;
                }

                List<bool[]> masks = _masks[camera];
                float threshold = SilhouetteOverlap.CollisionThreshold;
                List<SilhouetteOverlap.Pair> pairs = SilhouetteOverlap.Collisions(masks, threshold);
                List<string> labels = _maskLabels[camera];
                text.AppendLine();
                text.AppendLine($"## {camera} camera, {labels.Count} units, {pairs.Count} pairs");
                foreach (SilhouetteOverlap.Pair pair in pairs)
                {
                    text.AppendLine($"{pair.iou:0.000}  {labels[pair.first]}  {labels[pair.second]}");
                }

                // The nearest pairs under the threshold too, so a list with no collision still says how close it came
                List<SilhouetteOverlap.Pair> closest = SilhouetteOverlap.Collisions(masks, 0f);
                int shown = Mathf.Min(ClosestPairs, closest.Count);
                text.AppendLine($"### {camera} camera, the {shown} closest pairs");
                for (int i = 0; i < shown; i++)
                {
                    text.AppendLine($"{closest[i].iou:0.000}  {labels[closest[i].first]}  {labels[closest[i].second]}");
                }
                Debug.Log($"[LookSheetOutput] Collisions {camera} {pairs.Count}");
            }

            Write(LookSheetRun.Folder + "collisions.txt", text.ToString());
        }

        void Write(string path, Texture2D texture)
        {
            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderObjects.Release(texture);
            }
            Check(path);
        }

        void Write(string path, string text)
        {
            File.WriteAllText(path, text);
            Check(path);
        }

        void Check(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[LookSheetOutput] {path} was not written");
                _failures++;
            }
        }
    }
}

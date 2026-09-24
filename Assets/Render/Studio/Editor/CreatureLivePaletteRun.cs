using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Studio.Editor
{
    // Run against real Main and its placed allies. Frozen time isolates the asset edit from animation; the
    // editor watcher still runs. A held delivery proves that the edit leaves the same source and lease alive.
    public class CreatureLivePaletteRun : AStageRun
    {
        LookPalette _palette;
        string _original;
        float _timeScale = 1f;
        bool _isFrozen;
        GameObject _projectile;
        CreatureBuilder _view;

        protected override IEnumerator Run()
        {
            yield return Wait(0.5f);
            _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
            yield return Wait(0.7f);
            foreach (GameObject entity in _manager.entityManager.GetEntities(Entity.EntityType.Player))
            {
                if (entity)
                {
                    _view = entity.GetComponentInChildren<CreatureBuilder>();
                    if (_view && _view.rig != null)
                    {
                        break;
                    }
                }
            }
            if (!_view || _view.rig == null)
            {
                Debug.LogError("[CreatureLivePaletteRun] No placed real entity has a creature rig.");
                CreatureLivePaletteCapture.Finish(false);
                yield break;
            }

            Texture2D before = null;
            Texture2D after = null;
            bool passed = false;
            try
            {
                _palette = _manager.creatureLooks.vocabulary.palette;
                _original = EditorJsonUtility.ToJson(_palette);
                _timeScale = Time.timeScale;
                _isFrozen = true;
                Time.timeScale = 0f;
                _projectile = new GameObject("Palette capture held projectile");
                _projectile.transform.position = _view.transform.position + Vector3.up * 2f + Vector3.right;
                bool held = _view.BeginDelivery(1818, DeliveryStyle.Direct, _projectile.transform,
                    _projectile.transform.position);
                yield return Wait(0.3f);
                CreatureRig rig = _view.rig;
                Transform root = rig.root;
                int revision = rig.revision;
                Entity owner = _view.GetComponentInParent<Entity>();
                float health = owner.health.Value;
                Vector3 camera = _manager.gameCamera.transform.position;
                string folder = Path.GetFullPath("Logs/CreatureRosterCaptures");
                Directory.CreateDirectory(folder);
                before = StageReadback.Render(_manager.gameCamera, 1080, 1920);
                File.WriteAllBytes(Path.Combine(folder, "live-before.png"), before.EncodeToPNG());
                using (SerializedObject edit = new SerializedObject(_palette))
                {
                    edit.FindProperty("plantBody").colorValue = new Color(0.95f, 0.08f, 0.35f);
                    edit.FindProperty("plantStem").colorValue = new Color(0.9f, 0.28f, 0.06f);
                    edit.ApplyModifiedProperties();
                }
                // No direct rebuild call: this is the same dirty signal as an Inspector edit.
                float deadline = Time.realtimeSinceStartup + 5f;
                while (rig.revision == revision && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                yield return Wait(0.3f);
                after = StageReadback.Render(_manager.gameCamera, 1080, 1920);
                File.WriteAllBytes(Path.Combine(folder, "live-after.png"), after.EncodeToPNG());
                int pixels = ChangedPixels(before, after);
                bool lease = !_view.BeginDelivery(1818, DeliveryStyle.Direct, _projectile.transform,
                    _projectile.transform.position);
                passed = held && lease && _view.rig == rig && rig.root == root && rig.revision > revision
                    && owner.health.Value == health && _manager.gameCamera.transform.position == camera && pixels > 100;
                string report = "{\"passed\":" + passed.ToString().ToLowerInvariant()
                    + ",\"revisionBefore\":" + revision + ",\"revisionAfter\":" + rig.revision
                    + ",\"changedPixels\":" + pixels + ",\"heldDeliveryPreserved\":"
                    + (held && lease).ToString().ToLowerInvariant() + "}";
                File.WriteAllText(Path.Combine(folder, "live-proof.json"), report);
                Debug.Log("[CreatureLivePaletteRun] " + report);
            }
            finally
            {
                Restore();
                if (before)
                {
                    Object.Destroy(before);
                }
                if (after)
                {
                    Object.Destroy(after);
                }
            }
            CreatureLivePaletteCapture.Finish(passed);
        }

        public void Restore()
        {
            if (_palette && _original != null)
            {
                EditorJsonUtility.FromJsonOverwrite(_original, _palette);
                EditorUtility.SetDirty(_palette);
                _original = null;
            }
            if (_isFrozen)
            {
                Time.timeScale = _timeScale;
                _isFrozen = false;
            }
            if (_view)
            {
                _view.EndDelivery(1818);
            }
            if (_projectile)
            {
                Object.Destroy(_projectile);
            }
        }

        static int ChangedPixels(Texture2D before, Texture2D after)
        {
            Color32[] a = before.GetPixels32();
            Color32[] b = after.GetPixels32();
            int count = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b) > 30)
                {
                    count++;
                }
            }
            return count;
        }
    }
}

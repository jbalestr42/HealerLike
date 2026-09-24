using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Studio.Editor
{
    // Run against real Main and its placed allies. Frozen time isolates the asset edit from animation; the
    // editor watcher still runs. A real projectile proves the edit leaves the same source and lease alive.
    public class CreatureLivePaletteRun : AStageRun
    {
        LookPalette _palette;
        string _original;
        float _timeScale = 1f;
        bool _isFrozen;
        Projectile _projectile;
        ProjectileVisualObserver _observer;
        CreatureBuilder _view;
        BattleFocus _focus;
        bool _focusEnabled;

        protected override IEnumerator Run()
        {
            yield return Wait(0.5f);
            _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
            yield return Wait(0.7f);
            _hud.nextWaveButton.onClick.Invoke();
            float shotDeadline = Time.realtimeSinceStartup + 15f;
            while (!_view && Time.realtimeSinceStartup < shotDeadline)
            {
                foreach (ProjectileVisualObserver observer in Object.FindObjectsByType<ProjectileVisualObserver>(
                    FindObjectsSortMode.None))
                {
                    Projectile projectile = observer.GetComponent<Projectile>();
                    Entity entity = projectile && projectile.source ? projectile.source.GetComponent<Entity>() : null;
                    if (entity && entity.entityType == Entity.EntityType.Player && observer.gestureToken != 0
                        && !projectile.ShouldDestroyProjectile())
                    {
                        CreatureBuilder view = entity.GetComponentInChildren<CreatureBuilder>();
                        if (view && view.rig != null)
                        {
                            _view = view;
                            _projectile = projectile;
                            _observer = observer;
                            break;
                        }
                    }
                }
                if (!_view)
                {
                    yield return null;
                }
            }
            if (!_view || _view.rig == null)
            {
                Debug.LogError("[CreatureLivePaletteRun] No real ally projectile retained an active delivery.");
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
                _focus = Object.FindAnyObjectByType<BattleFocus>();
                _focusEnabled = _focus && _focus.enabled;
                if (_focus)
                {
                    _focus.enabled = false;
                }
                int token = _observer.gestureToken;
                yield return Wait(0.3f);
                CreatureRig rig = _view.rig;
                Transform root = rig.root;
                int revision = rig.revision;
                Entity owner = _view.GetComponentInParent<Entity>();
                float health = owner.health.Value;
                Vector3 camera = _manager.gameCamera.transform.position;
                string folder = Path.GetFullPath("Logs/CreatureRosterCaptures");
                Directory.CreateDirectory(folder);
                StageMotionOutput output = new StageMotionOutput(folder);
                yield return output.Capture(_manager.gameCamera, "live-before", "palette");
                if (!output.isCaptured)
                {
                    CreatureLivePaletteCapture.Finish(false);
                    yield break;
                }
                before = output.Load("live-before.png");
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
                yield return output.Capture(_manager.gameCamera, "live-after", "palette");
                if (!output.isCaptured)
                {
                    CreatureLivePaletteCapture.Finish(false);
                    yield break;
                }
                after = output.Load("live-after.png");
                int pixels = ChangedPixels(before, after);
                bool held = _projectile && _observer && _observer.gestureToken == token;
                bool lease = held && !_view.BeginDelivery(token, _observer.deliveryStyle, _projectile.transform,
                    _projectile.transform.position);
                passed = held && lease && _view.rig == rig && rig.root == root && rig.revision > revision
                    && owner.health.Value == health && _manager.gameCamera.transform.position == camera && pixels > 100;
                string report = "{\"passed\":" + passed.ToString().ToLowerInvariant()
                    + ",\"revisionBefore\":" + revision + ",\"revisionAfter\":" + rig.revision
                    + ",\"changedPixels\":" + pixels + ",\"heldDeliveryPreserved\":"
                    + (held && lease).ToString().ToLowerInvariant() + ",\"realProjectile\":true,\"gestureToken\":"
                    + token + ",\"nativeGameViewWithHud\":true}";
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
            if (_focus)
            {
                _focus.enabled = _focusEnabled;
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

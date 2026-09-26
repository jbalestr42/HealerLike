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
        StageGameViewSize _gameViewSize;
        readonly bool _shapes;
        LookVocabulary _vocabulary;
        MassBand _editedMass;
        LookPart _originalBody;
        bool _hasShapeEdit;

        public CreatureLivePaletteRun(bool shapes = false)
        {
            _shapes = shapes;
        }

        protected override IEnumerator Run()
        {
            _gameViewSize = new StageGameViewSize(1080, 1920);
            yield return Wait(0.5f);
            _player.PlaceAllies(_manager, StagePlayer.LoadAllies());
            yield return Wait(0.7f);
            _hud.nextWaveButton.onClick.Invoke();
            float shotDeadline = Time.realtimeSinceStartup + 15f;
            while (!_view && Time.realtimeSinceStartup < shotDeadline)
            {
                foreach (ProjectileVisualObserver observer in Object.FindObjectsByType<ProjectileVisualObserver>())
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
                _vocabulary = _manager.creatureLooks.vocabulary;
                _palette = _vocabulary.palette;
                if (!_shapes)
                {
                    _original = EditorJsonUtility.ToJson(_palette);
                }
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
                Transform[] anchors = (Transform[])rig.budAnchors.Clone();
                Vector3[] vertices = CreatureLiveEditMeasure.BodyVertices(rig);
                Vector3 bodyScale = rig.partTransforms[0].localScale;
                float health = owner.health.Value;
                Vector3 camera = _manager.gameCamera.transform.position;
                string folder = Path.GetFullPath(_shapes ? "Logs/GrowthStoneCaptures" : "Logs/CreatureRosterCaptures");
                Directory.CreateDirectory(folder);
                StageMotionOutput output = new StageMotionOutput(folder);
                string prefix = _shapes ? "live-shape" : "live";
                yield return output.Capture(_manager.gameCamera, prefix + "-before", _shapes ? "shape" : "palette");
                if (!output.isCaptured)
                {
                    CreatureLivePaletteCapture.Finish(false);
                    yield break;
                }
                before = output.Load(prefix + "-before.png");
                string originalProfile = "null";
                string editedProfile = "null";
                if (_shapes)
                {
                    _editedMass = LookDerivation.Channels(owner.data, owner.entityType).mass;
                    _originalBody = _vocabulary.bodies[_editedMass].plant[0];
                    _hasShapeEdit = true;
                    originalProfile = JsonUtility.ToJson(_originalBody.shape);
                    LookPart edit = _originalBody;
                    edit.shape.fullness = 0.24f;
                    edit.shape.taper = 0.82f;
                    edit.shape.bend = 0.42f;
                    _vocabulary.bodies[_editedMass].plant[0] = edit;
                    editedProfile = JsonUtility.ToJson(edit.shape);
                    EditorUtility.SetDirty(_vocabulary);
                }
                else
                {
                    using (SerializedObject edit = new SerializedObject(_palette))
                    {
                        edit.FindProperty("plantBody").colorValue = new Color(0.95f, 0.08f, 0.35f);
                        edit.FindProperty("plantStem").colorValue = new Color(0.9f, 0.28f, 0.06f);
                        edit.ApplyModifiedProperties();
                    }
                }
                // No direct rebuild call: this is the same dirty signal as an Inspector edit.
                float deadline = Time.realtimeSinceStartup + 5f;
                while (rig.revision == revision && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                yield return Wait(0.3f);
                yield return output.Capture(_manager.gameCamera, prefix + "-after", _shapes ? "shape" : "palette");
                if (!output.isCaptured)
                {
                    CreatureLivePaletteCapture.Finish(false);
                    yield break;
                }
                after = output.Load(prefix + "-after.png");
                int pixels = CreatureLiveEditMeasure.ChangedPixels(before, after);
                int changedVertices = CreatureLiveEditMeasure.ChangedVertices(vertices,
                    CreatureLiveEditMeasure.BodyVertices(rig));
                bool anchorsHeld = anchors.Length == rig.budAnchors.Length;
                for (int i = 0; anchorsHeld && i < anchors.Length; i++)
                {
                    anchorsHeld = anchors[i] == rig.budAnchors[i];
                }
                bool held = _projectile && _observer && _observer.gestureToken == token;
                bool lease = held && !_view.BeginDelivery(token, _observer.deliveryStyle, _projectile.transform,
                    _projectile.transform.position);
                passed = held && lease && _view.rig == rig && rig.root == root && rig.revision > revision
                    && owner.health.Value == health && _manager.gameCamera.transform.position == camera && pixels > 100
                    && (!_shapes || (changedVertices > 0 && anchorsHeld
                        && rig.partTransforms[0].localScale == bodyScale));
                int revisionAfter = rig.revision;
                bool profileRestored = true;
                bool meshRestored = true;
                if (_shapes)
                {
                    RestoreShape();
                    deadline = Time.realtimeSinceStartup + 5f;
                    while (rig.revision == revisionAfter && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }
                    profileRestored = JsonUtility.ToJson(_vocabulary.bodies[_editedMass].plant[0].shape)
                        == originalProfile;
                    meshRestored = CreatureLiveEditMeasure.ChangedVertices(vertices,
                        CreatureLiveEditMeasure.BodyVertices(rig)) == 0 && rig.revision > revisionAfter;
                    passed &= profileRestored && meshRestored;
                }
                string report = "{\"passed\":" + passed.ToString().ToLowerInvariant()
                    + ",\"revisionBefore\":" + revision + ",\"revisionAfter\":" + revisionAfter
                    + ",\"changedPixels\":" + pixels + ",\"heldDeliveryPreserved\":"
                    + (held && lease).ToString().ToLowerInvariant() + ",\"realProjectile\":true,\"gestureToken\":"
                    + token + ",\"nativeGameViewWithHud\":true,\"shapeMode\":" + _shapes.ToString().ToLowerInvariant()
                    + ",\"changedVertices\":" + changedVertices
                    + ",\"anchorsPreserved\":" + anchorsHeld.ToString().ToLowerInvariant()
                    + ",\"profileRestored\":" + profileRestored.ToString().ToLowerInvariant()
                    + ",\"meshRestored\":" + meshRestored.ToString().ToLowerInvariant()
                    + ",\"originalProfile\":" + originalProfile + ",\"editedProfile\":" + editedProfile
                    + ",\"blindSpot\":\"Simulation is paused; this verifies watcher propagation during a held delivery, "
                    + "not uninterrupted combat motion.\"}";
                File.WriteAllText(Path.Combine(folder, prefix + "-proof.json"), report);
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
            RestoreShape();
            if (_gameViewSize != null)
            {
                _gameViewSize.Dispose();
                _gameViewSize = null;
            }
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

        void RestoreShape()
        {
            if (_hasShapeEdit && _vocabulary)
            {
                // Odin dictionaries need their exact saved value restored; EditorJsonUtility omits them.
                _vocabulary.bodies[_editedMass].plant[0] = _originalBody;
                EditorUtility.SetDirty(_vocabulary);
                _hasShapeEdit = false;
            }
        }

    }
}

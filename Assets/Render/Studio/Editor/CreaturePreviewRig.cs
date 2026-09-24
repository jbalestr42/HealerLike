using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature preview's rig, built on a private copy of the recipe and moved at a fixed 60 steps a second,
    // so any seek lands on the same pose
    public class CreaturePreviewRig
    {
        static readonly float step = 1f / 60f;
        static readonly FootFrame frame = new FootFrame(Vector3.zero, Vector3.up, 1f);

        CreatureRecipe _working;
        CreatureRig _rig;
        int _steps;

        // Null until a build succeeds
        public CreatureRig rig { get { return _rig; } }

        // The rig under subject, or the reason there is none; Start poses it at time 0
        public string Build(CreatureRecipe source, Transform subject, StudioPreviewScene scene, LookSide side)
        {
            Destroy();
            string error;
            if (!CreatureValidator.TryValidate(source, out error))
            {
                return error;
            }

            _working = Object.Instantiate(source);
            _working.name = "Creature Studio Working Recipe";
            _working.hideFlags = HideFlags.HideAndDontSave;
            // Init mixes its parent's id into the seed; undoing it on the private copy keeps every preview and
            // export of one recipe on the same idle motion and colours
            IdleDefinition idle = _working.idle;
            idle.seed ^= subject.GetEntityId().GetHashCode();
            _working.idle = idle;
            _rig = new CreatureRig();
            if (!_rig.Init(_working, subject, scene.Shared(side), scene.Body(side), scene.meshes, 1f))
            {
                Destroy();
                return "The creature rig could not be built. Check the recipe diagnostics.";
            }

            return null;
        }

        public void Start(Vector3? aim, float health, float charge, float glow)
        {
            _rig.SetReadout(aim, health, charge, glow);
            _rig.Tick(0f, 0f, frame);
        }

        // Aim integrates over whole fixed steps, then the idle motion is set at the exact time
        public void Advance(float time, Vector3? aim, float health, float charge, float glow)
        {
            _rig.SetReadout(aim, health, charge, glow);
            int targetSteps = Mathf.FloorToInt(time * 60f + 0.00001f);
            while (_steps < targetSteps)
            {
                _steps++;
                _rig.Tick(_steps * step, step, frame);
            }
            _rig.Tick(time, 0f, frame);
        }

        // The runtime may defer destruction in play mode; the private scene destroys at once, so no old frame
        // overlaps the new one while scrubbing
        public void Destroy()
        {
            _steps = 0;
            GameObject generated = null;
            if (_rig != null && _rig.root)
            {
                generated = _rig.root.gameObject;
            }

            if (_rig != null)
            {
                _rig.Dispose();
            }

            _rig = null;
            if (generated)
            {
                Object.DestroyImmediate(generated);
            }

            if (_working)
            {
                Object.DestroyImmediate(_working);
            }
            _working = null;
        }
    }
}

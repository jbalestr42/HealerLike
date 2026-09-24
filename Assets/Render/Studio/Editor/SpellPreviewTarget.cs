using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    // The creature a spell preview plays on: an authored recipe, or the shipped healer when none is set, and the side
    // whose palette colours the effect's body parts. The preset's own side stays the caster's rim.
    public class SpellPreviewTarget
    {
        static readonly string healerPath = "Assets/Render/Creatures/Data/Healer.asset";

        CreatureRecipe _recipe;
        LookSide _side = LookSide.Plant;
        bool _isAutomatic = true;
        bool _isDirty = true;
        CreatureRig _rig;
        GameObject _root;
        string _error;

        // Null plays on the shipped healer
        public CreatureRecipe recipe
        {
            get { return _recipe; }
            set
            {
                if (_recipe == value)
                {
                    return;
                }

                _recipe = value;
                _isDirty = true;
            }
        }

        // The side read from the recipe while automatic, else the one set by hand
        public LookSide side
        {
            get
            {
                if (_isAutomatic)
                {
                    return InferSide(_recipe);
                }
                return _side;
            }
            set
            {
                if (_side == value && !_isAutomatic)
                {
                    return;
                }

                _side = value;
                _isAutomatic = false;
                _isDirty = true;
            }
        }

        public bool isAutomatic
        {
            get { return _isAutomatic; }
            set
            {
                if (_isAutomatic == value)
                {
                    return;
                }

                _isAutomatic = value;
                _isDirty = true;
            }
        }

        public bool isDirty { get { return _isDirty; } }

        public GameObject root { get { return _root; } }

        // Set when the recipe fails validation, the preview then shows it instead of the spell
        public string error { get { return _error; } }

        public void Init(StudioPreviewScene scene)
        {
            _root = scene.AddChild("Reference Creature");
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public void Rebuild(StudioPreviewScene scene)
        {
            _isDirty = false;
            _error = null;
            if (_rig != null)
            {
                _rig.Dispose();
            }

            _rig = null;
            CreatureRecipe creature = _recipe;
            if (!creature)
            {
                creature = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(healerPath);
            }

            if (!creature)
            {
                return;
            }

            string problem;
            if (!CreatureValidator.TryValidate(creature, out problem))
            {
                _error = "The reference creature needs repair in Creature Studio: " + problem;
                return;
            }

            _rig = new CreatureRig();
            LookSide targetSide = side;
            Material shared = scene.Shared(targetSide);
            Material body = scene.Body(targetSide);
            if (_rig.Init(creature, _root.transform, shared, body, scene.meshes, 1f))
            {
                _rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
            }
            StudioPreviewScene.HideTree(scene.root);
        }

        // The rig's anchors, or a unit-sized stand-in while no rig is built
        public EffectAnchors GetAnchors()
        {
            EffectAnchors actual;
            if (_rig != null && _rig.TryGetAnchors(out actual))
            {
                return actual;
            }

            EffectAnchors anchors = new EffectAnchors();
            anchors.foot = Vector3.zero;
            anchors.bodyCentre = Vector3.up * 0.3f;
            anchors.bodyRadius = 0.3f;
            anchors.neck = Vector3.up * 0.6f;
            anchors.headCentre = Vector3.up * 0.75f;
            anchors.headRadius = 0.15f;
            return anchors;
        }

        public void Dispose()
        {
            if (_rig != null)
            {
                _rig.Dispose();
            }
            _rig = null;
        }

        // A baked recipe carries no side, so a stone-shaped body reads as stone. An editor default only:
        // the side can still be set by hand for an unusual silhouette.
        public static LookSide InferSide(CreatureRecipe recipe)
        {
            if (!recipe || recipe.parts == null)
            {
                return LookSide.Plant;
            }

            foreach (CreaturePart part in recipe.parts)
            {
                bool isStoneShape = part.primitive == Primitive.Stone || part.primitive == Primitive.Boulder
                    || part.primitive == Primitive.Pyramid;
                if (part.role == PartRole.Body && isStoneShape)
                {
                    return LookSide.Stone;
                }
            }
            return LookSide.Plant;
        }
    }
}

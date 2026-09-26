using System.Collections.Generic;
using HealerLike.Render.Grass;
using NUnit.Framework;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Zones
{

    public abstract class TrampleZoneFixture
    {
        protected GameObject _root;
        protected GameObject _obstacle;
        protected Ground _ground;
        protected TrampleZone _zone;
        protected CreatureRecipe _recipe;
        protected TrampleRigTestHost _host;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("zones");
            _obstacle = new GameObject("obstacle");
            _ground = new Ground();
            _zone = _obstacle.AddComponent<TrampleZone>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null) _host.Clear();
            if (_recipe != null) Object.DestroyImmediate(_recipe);
            Object.DestroyImmediate(_obstacle);
            Object.DestroyImmediate(_root);
            _ground.Dispose();
        }

        // The obstacle's disc on the ground this frame, eased in, or none
        protected bool Disc(out GroundStamp disc)
        {
            _ground.Advance(1f);
            foreach (GroundStamp stamp in GroundProbe.Stamps(_ground))
            {
                if (stamp.kind == GroundStampKind.Disc)
                {
                    disc = stamp;
                    return true;
                }
            }

            disc = default;
            return false;
        }

        protected void BuildRig()
        {
            _host = _obstacle.AddComponent<TrampleRigTestHost>();
            Assert.IsNotNull(_host, "The Editor-only rig helper must be attachable in EditMode.");
            Assert.IsTrue(_host.Build(_recipe, RenderTestAssets.LoadLookMaterial()));
            _host.rig.Tick(0f, 0f, new FootFrame(_obstacle.transform.position, Vector3.up, 1f));
            _zone.InitFootprint(_ground);
        }

        // A root-legged creature standing at the origin, its landing on its first refresh already counted
        protected void BuildRootedCreature()
        {
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.idle = default;
            _recipe.arms = new ArmDefinition[0];
            _recipe.roots.count = 6;
            _recipe.roots.footRadius = 0.8f;
            _recipe.roots.thickness = 0.08f;
            BuildRig();
        }

        // Landing rings playing: out of the feet and round the body
        protected int CountRings()
        {
            return _ground.Playing(_ground.vocabulary.footRing) + _ground.Playing(_ground.vocabulary.bodyRing);
        }

    }
}

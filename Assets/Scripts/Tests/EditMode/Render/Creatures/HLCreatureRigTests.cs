using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLCreatureRigTests
    {
        GameObject _parent;
        Material _material;
        HLCreatureRecipe _recipe;
        HLCreatureRig _rig;

        [SetUp]
        public void Setup()
        {
            _parent = new GameObject("HLTestRig");
            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _recipe = HLCreatureValidatorTests.Recipe();
            _recipe.idle = default;
            _rig = HLCreatureRig.Build(_recipe, _parent.transform, _material);
        }

        [TearDown]
        public void Cleanup()
        {
            _rig.Dispose();
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_recipe);
            HLPrimitiveMeshes.ReleaseAll();
        }

        [Test]
        public void ShortDirectDeliveryDoesNotDrawUnusedBoardLengthAsCoils()
        {
            GameObject host = new GameObject("HLShortDelivery");
            string path = "Assets/Render/Creatures/Data/HLSpiralFern.asset";
            HLCreatureRecipe data = UnityEditor.AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                using (HLCreatureRig body = HLCreatureRig.Build(data, host.transform, material))
                {
                    body.Tick(0f, 0f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
                    Vector3 target = new Vector3(2f, 1.3f, 0f);

                    Assert.IsTrue(body.BeginDelivery(500, HLDeliveryStyle.Direct, null, target));
                    body.ContactDelivery(500, target, null);
                    body.Tick(0.02f, 0.02f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));

                    MeshFilter arm = body.root.Find("HLLianaArm").GetComponent<MeshFilter>();
                    Assert.Less(arm.sharedMesh.bounds.size.magnitude, 4f,
                        "A two-cell real delivery must not loop the unused 24-cell reach around the actor.");
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(material);
                HLPrimitiveMeshes.ReleaseAll();
            }
        }

        [Test]
        public void LastRigReleasesSharedMeshesEvenWhenParentWasDestroyed()
        {
            HLCreatureRig other = HLCreatureRig.Build(_recipe, _parent.transform, _material);
            Mesh mesh = _rig.root.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.AreSame(mesh, other.root.GetComponentInChildren<MeshFilter>().sharedMesh);

            _rig.Dispose();
            _rig.Dispose();
            Assert.IsTrue(mesh);
            Object.DestroyImmediate(_parent);
            other.Dispose();
            other.Dispose();

            Assert.IsFalse(mesh);
        }

        [Test]
        public void GlowUsesLookBaseColourAndPreservesAlpha()
        {
            _rig.Dispose();
            _recipe.parts[0].glow = 2f;
            _recipe.parts[0].colour = new Color(0.2f, 0.4f, 0.1f, 0.7f);
            _rig = HLCreatureRig.Build(_recipe, _parent.transform, _material);
            Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetColor("_BaseColor").g, Is.InRange(1.104f, 1.296f));
            Assert.That(block.GetColor("_BaseColor").a, Is.EqualTo(0.7f).Within(0.0001f));
            _rig.SetReadout(null, 1f, 0f, 1f);
            _rig.Tick(0f, 0f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            renderer.GetPropertyBlock(block);
            Assert.Greater(block.GetColor("_BaseColor").g, 1);
            _rig.SetReadout(null, 1f, 0f, 0f);
            _rig.Tick(0f, 0f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            renderer.GetPropertyBlock(block);
            Assert.Less(block.GetColor("_BaseColor").g, 1);
        }

        [Test]
        public void ChargeSwellsAndBrightensBudAndWiltSuppressesGlow()
        {
            _rig.Dispose();
            _recipe.parts[0].glow = 1f;
            _rig = HLCreatureRig.Build(_recipe, _parent.transform, _material);
            Renderer renderer = _rig.root.GetComponentInChildren<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            HLFootFrame frame = new HLFootFrame(Vector3.zero, Vector3.up, 1f);
            _rig.SetReadout(null, 1f, 0f, 0f);
            _rig.Tick(0f, 0f, frame);
            Vector3 scale = renderer.transform.localScale;
            renderer.GetPropertyBlock(block);
            Color colour = block.GetColor("_BaseColor");

            _rig.SetReadout(null, 1f, 1f, 0f);
            _rig.Tick(0f, 0f, frame);

            Assert.Greater(renderer.transform.localScale.x, scale.x * 1.2f);
            renderer.GetPropertyBlock(block);
            Assert.Greater(block.GetColor("_BaseColor").g, colour.g);
            _rig.SetReadout(null, 0f, 1f, 1f);
            _rig.Tick(0f, 0f, frame);
            renderer.GetPropertyBlock(block);
            Assert.Less(block.GetColor("_BaseColor").g, 0.5f);
        }

        [Test]
        public void WarmBodyTickAllocatesNoManagedMemory()
        {
            HLFootFrame frame = new HLFootFrame(Vector3.zero, Vector3.up, 1f);
            for (int i = 0; i < 20; i++)
            {
                _rig.Tick(i * 0.016f, 0.016f, frame);
            }

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                _rig.Tick(i * 0.016f, 0.016f, frame);
            }

            Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Test]
        public void PoolSaturatesWithoutStealingLeasesAndDisposeIsIdempotent()
        {
            for (int i = 0; i < 8; i++)
            {
                Assert.AreNotEqual(0, _rig.Begin(HLGestureKind.Attack, Vector3.one));
            }

            Assert.AreEqual(0, _rig.Begin(HLGestureKind.Attack, Vector3.one));
            Assert.AreEqual(8, _rig.activeArmCount);
            _rig.Contact(0, Vector3.one);
            _rig.End(0);
            Assert.AreEqual(8, _rig.activeArmCount);
            _rig.CancelAll();
            _rig.Tick(0.79f, 0.05f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            _rig.Tick(1f, 0.21f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            Assert.AreEqual(0, _rig.activeArmCount);
            _rig.Dispose();
            _rig.Dispose();
            Assert.IsFalse(_rig.root);
        }

        [Test]
        public void FrameReplantsRootsAfterTranslationWithoutMovingParent()
        {
            _parent.transform.position = new Vector3(3f, 2f, 4f);
            Vector3 before = _parent.transform.position;

            _rig.Tick(2f, 0.016f, new HLFootFrame(new Vector3(3f, 0.5f, 4f), Vector3.up, 1f));

            Assert.AreEqual(before, _parent.transform.position);
            Assert.AreEqual(new Vector3(3f, 0.5f, 4f), _rig.root.position);
            Transform root = _rig.root.Find("HLRoot");
            Assert.NotNull(root);
            Assert.IsEmpty(_parent.GetComponentsInChildren<Collider>());
        }

        [Test]
        public void ParentMeshDimensionsDoNotScaleChildPivots()
        {
            _rig.Dispose();
            HLPart child = new HLPart
            {
                id = "HLChild",
                parent = 0,
                localPosition = Vector3.up,
                dimensions = Vector3.one,
                colour = Color.green
            };
            _recipe.parts = new HLPart[] { _recipe.parts[0], child };
            _recipe.parts[0].dimensions = Vector3.one * 3f;

            _rig = HLCreatureRig.Build(_recipe, _parent.transform, _material);

            Transform pivot = _rig.root.Find("HLSway/HLBody/HLChild");
            Assert.AreEqual(Vector3.up, pivot.localPosition);
            Assert.AreEqual(Vector3.one, pivot.lossyScale);
        }

        [Test]
        public void SaturatedContactCannotReviveCancelledLease()
        {
            for (int i = 0; i < 8; i++)
            {
                int token = _rig.Begin(HLGestureKind.Attack, Vector3.one);
                _rig.Contact(token, Vector3.one);
            }

            _rig.CancelAll();
            _rig.Tick(0.05f, 0.05f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            _rig.Contact(0, Vector3.one);
            _rig.Tick(0.26f, 0.21f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));

            Assert.AreEqual(0, _rig.activeArmCount);
        }

        [Test]
        public void NonuniformAncestorsAreRejected()
        {
            _parent.transform.localScale = new Vector3(1f, 2f, 1f);

            Assert.Throws<System.ArgumentException>(() => HLCreatureRig.Build(_recipe, _parent.transform, _material));
        }

        [Test]
        public void DeliveryLeasesCapSwarmAndRejectStaleOrUnsupportedTokens()
        {
            Assert.IsFalse(_rig.BeginDelivery(1, HLDeliveryStyle.Thrown, null, Vector3.one));
            for (int i = 1; i <= 4; i++)
            {
                Assert.IsTrue(_rig.BeginDelivery(i, HLDeliveryStyle.Swarm, null, Vector3.one));
            }

            Assert.IsFalse(_rig.BeginDelivery(5, HLDeliveryStyle.Swarm, null, Vector3.one));
            Assert.IsFalse(_rig.BeginDelivery(1, HLDeliveryStyle.Direct, null, Vector3.one));
            _rig.EndDelivery(1);
            _rig.EndDelivery(1);
            Assert.IsTrue(_rig.BeginDelivery(5, HLDeliveryStyle.Swarm, null, Vector3.one));
        }

        [Test]
        public void ChainCollectsContactsAndRetractsTogether()
        {
            Assert.IsTrue(_rig.BeginDelivery(123, HLDeliveryStyle.ChainSync, null, Vector3.one));
            _rig.ContactDelivery(123, Vector3.one, null);
            _rig.ContactDelivery(123, Vector3.right * 2f, null);
            Assert.AreEqual(2, _rig.activeArmCount);
            _rig.Tick(0.1f, 0.1f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            Assert.AreEqual(2, _rig.activeArmCount);
            _rig.EndDelivery(123);
            _rig.Tick(0.4f, 0.3f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            Assert.AreEqual(0, _rig.activeArmCount);
        }

        [Test]
        public void AimDampsAndHealthRecoversWithoutMovingFootFrame()
        {
            _rig.SetReadout(Vector3.right * 4f, 0.2f, 0.8f, 0.8f);
            _rig.Tick(1f, 0.1f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            Assert.Greater(_rig.aim.eulerAngles.y, 0);
            Assert.Less(_rig.aim.eulerAngles.y, 90);
            Transform sway = _rig.root.Find("HLSway");
            Assert.Less(sway.localPosition.y, 0);
            _rig.SetReadout(Vector3.right * 4f, 1f, 0f, 0f);
            _rig.Tick(2f, 1f, new HLFootFrame(Vector3.zero, Vector3.up, 1f));
            Assert.AreEqual(0, sway.localPosition.y);
            Assert.AreEqual(Vector3.zero, _rig.root.position);
        }

        [Test]
        public void HitShakeDecaysAndHealthTintRecovers()
        {
            HLFootFrame frame = new HLFootFrame(Vector3.zero, Vector3.up, 1f);
            _rig.SetReadout(Vector3.forward, 0.1f, 0f, 0f);
            _rig.Hit();
            _rig.Tick(0f, 0.01f, frame);
            Transform sway = _rig.root.Find("HLSway");
            Assert.Greater(Mathf.Abs(sway.localRotation.z), 0.001f);
            Renderer renderer = sway.GetComponentInChildren<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Color hurt = block.GetColor("_BaseColor");
            _rig.SetReadout(Vector3.forward, 1f, 0f, 0f);
            _rig.Tick(1f, 1f, frame);
            renderer.GetPropertyBlock(block);
            Assert.AreNotEqual(hurt, block.GetColor("_BaseColor"));
            Assert.That(Quaternion.Angle(sway.localRotation, Quaternion.identity), Is.LessThan(0.001f));
        }
    }
}

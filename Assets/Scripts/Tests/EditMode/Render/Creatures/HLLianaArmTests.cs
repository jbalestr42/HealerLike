using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLLianaArmTests
    {
        HLCreatureRecipe _recipe;
        HLLianaArm _arm;

        [SetUp]
        public void Setup()
        {
            _recipe = HLCreatureValidatorTests.Recipe();
            _arm = new HLLianaArm(_recipe.arms[0], null, null);
            _arm.Tick(0f, Vector3.zero, Quaternion.identity);
        }

        [TearDown]
        public void Cleanup()
        {
            _arm.Dispose();
            Object.DestroyImmediate(_recipe);
        }

        void Lengths()
        {
            for (int i = 0; i < _arm.segmentCount; i++)
            {
                Assert.That(Vector3.Distance(_arm.Joint(i), _arm.Joint(i + 1)), Is.EqualTo(0.2f).Within(0.00001));
            }
        }

        [Test]
        public void AuthoredArmUsesOneMeshOnlyDuringGesturesAndDisposesIt()
        {
            string path = "Assets/Render/Creatures/Data/HLHealer.asset";
            HLCreatureRecipe authored = UnityEditor.AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            GameObject parent = new GameObject("HLArmFixture");
            Material material = new Material(Shader.Find("HL/Look/Primitive"));
            HLLianaArm rendered = new HLLianaArm(authored.arms[0], parent.transform, material);
            Mesh mesh = null;
            try
            {
                Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
                Assert.AreEqual(1, renderers.Length);
                Assert.IsFalse(renderers[0].enabled);
                rendered.Tick(0f, Vector3.zero, Quaternion.identity);
                Assert.AreEqual(0, rendered.meshRevision);
                Assert.AreEqual(0, rendered.activeLeafCount);
                rendered.Begin(1, HLGestureKind.Attack, Vector3.one);
                rendered.SetTipGoal(1, Vector3.one);
                rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
                Assert.IsTrue(renderers[0].enabled);
                Assert.AreEqual(5, rendered.activeLeafCount);
                Assert.That(Vector3.Distance(rendered.tipMatrix.GetColumn(3), rendered.tip), Is.LessThan(0.00001f));
                for (int i = 0; i < 5; i++)
                {
                    Assert.IsTrue(HLChainSolver.Finite(rendered.LeafMatrix(i).GetColumn(3)));
                }

                for (int i = 0; i < 10; i++)
                {
                    rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
                }

                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 20; i++)
                {
                    rendered.Tick(0.016f, Vector3.zero, Quaternion.identity);
                }

                Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
                mesh = parent.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.Greater(mesh.vertexCount, 0);
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Assert.IsTrue(HLChainSolver.Finite(vertex));
                }

                Assert.That(Vector3.Distance(rendered.tip, Vector3.one), Is.LessThan(0.001f));
                rendered.End(1);
                rendered.Tick(1f, Vector3.zero, Quaternion.identity);
                Assert.IsFalse(renderers[0].enabled);
                int revision = rendered.meshRevision;
                Vector3[] vertices = mesh.vertices;
                rendered.SetVisible(true);
                rendered.Tick(1f, Vector3.one, Quaternion.identity);
                Assert.IsFalse(renderers[0].enabled);
                Assert.AreEqual(revision, rendered.meshRevision);
                CollectionAssert.AreEqual(vertices, mesh.vertices);
            }
            finally
            {
                rendered.Dispose();
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(material);
            }

            Assert.IsFalse(mesh);
        }

        [Test]
        public void RestMeshHasOneNormalPerVertexBeforeAnyGesture()
        {
            string path = "Assets/Render/Creatures/Data/HLHealer.asset";
            HLCreatureRecipe authored = UnityEditor.AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            GameObject parent = new GameObject("HLArmFixture");
            Material material = new Material(Shader.Find("HL/Look/Primitive"));
            HLLianaArm rendered = new HLLianaArm(authored.arms[0], parent.transform, material);
            try
            {
                Mesh mesh = parent.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.Greater(mesh.vertexCount, 0);
                Assert.AreEqual(mesh.vertexCount, mesh.normals.Length,
                    "QuickOutline indexes normals per vertex on Awake");
            }
            finally
            {
                rendered.Dispose();
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ContactBypassesAnticipationAndReturnRestoresExactPose()
        {
            _arm.Begin(1, HLGestureKind.Heal, Vector3.one);
            _arm.Contact(1, Vector3.one);
            _arm.Tick(0.001f, Vector3.zero, Quaternion.identity);
            Assert.Less(Vector3.Distance(_arm.tip, Vector3.one), 0.001f);
            Lengths();
            for (int i = 0; i < 30; i++)
            {
                _arm.Tick(0.01f, Vector3.zero, Quaternion.identity);
                Lengths();
            }

            Assert.AreEqual(HLGesturePhase.Rest, _arm.phase);
            for (int i = 0; i <= _arm.segmentCount; i++)
            {
                Assert.AreEqual(_recipe.arms[0].restJoints[i], _arm.Joint(i));
            }
        }

        [Test]
        public void StaleAndRepeatedEndsDoNotRetractNewGesture()
        {
            _arm.Begin(1, HLGestureKind.Attack, Vector3.one);
            _arm.Begin(2, HLGestureKind.Attack, Vector3.up);
            _arm.End(1);
            _arm.Cancel(1);
            Assert.AreEqual(HLGesturePhase.Extend, _arm.phase);
            _arm.Tick(0.02f, Vector3.zero, Quaternion.identity);
            _arm.Cancel(2);
            _arm.Cancel(2);
            Assert.AreEqual(HLGesturePhase.Retract, _arm.phase);
            _arm.Tick(0.1f, Vector3.zero, Quaternion.identity);
            Lengths();
            _arm.End(2);
            _arm.Tick(0.11f, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(HLGesturePhase.Rest, _arm.phase);
        }

        [Test]
        public void ProjectileGoalIsFollowedWithoutInventedExtension()
        {
            _arm.Begin(1, HLGestureKind.Attack, Vector3.one);
            _arm.SetTipGoal(1, Vector3.up * 2f);

            _arm.Tick(0.001f, Vector3.zero, Quaternion.identity);

            Assert.Less(Vector3.Distance(_arm.tip, Vector3.up * 2f), 0.001f);
            Lengths();
        }

        [Test]
        public void DestructionAfterHitStillDisplaysContactBeforeReturn()
        {
            _arm.Begin(1, HLGestureKind.Attack, Vector3.one);
            _arm.Contact(1, Vector3.one);
            _arm.End(1);

            _arm.Tick(0.016f, Vector3.zero, Quaternion.identity);

            Assert.AreEqual(HLGesturePhase.Contact, _arm.phase);
            Assert.Less(Vector3.Distance(_arm.tip, Vector3.one), 0.001f);
        }

        [TestCase(HLDeliveryStyle.Arc)]
        [TestCase(HLDeliveryStyle.Rigid)]
        [TestCase(HLDeliveryStyle.Bounce)]
        public void DeliveryProfilesFollowLiveEndpoint(HLDeliveryStyle style)
        {
            _arm.style = style;
            _arm.deliveryProfile = true;
            _arm.Begin(1, HLGestureKind.Attack, Vector3.right * 2f);
            _arm.SetTipGoal(1, Vector3.right * 2f);
            _arm.Tick(0.016f, Vector3.zero, Quaternion.identity);
            Assert.That(Vector3.Distance(_arm.tip, Vector3.right * 2f), Is.LessThan(0.001f));
            if (style == HLDeliveryStyle.Arc)
            {
                Assert.Greater(_arm.Joint(_arm.segmentCount / 2).y, 0.1f);
            }
            else
            {
                Assert.AreEqual(0, _arm.Joint(_arm.segmentCount / 2).y);
            }

            _arm.Contact(1, Vector3.right * 2f);
            _arm.SetTipGoal(1, Vector3.forward);
            _arm.Tick(0.016f, Vector3.zero, Quaternion.identity);
            Assert.That(Vector3.Distance(_arm.tip, Vector3.forward), Is.LessThan(0.001f));
        }
    }
}

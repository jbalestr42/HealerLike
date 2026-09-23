using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class HLSpellPrimitivesTests
    {
        static void DestroyHost(GameObject go)
        {
            if (!go)
            {
                return;
            }
            foreach (HLSpellEffect effect in go.GetComponentsInChildren<HLSpellEffect>(true))
            {
                TestHelpers.InvokePrivate(effect, "OnDestroy");
            }
            foreach (HLSpellVisualSink sink in go.GetComponentsInChildren<HLSpellVisualSink>(true))
            {
                TestHelpers.InvokePrivate(sink, "OnDestroy");
            }
            Object.DestroyImmediate(go);
        }

        [Test]
        public void BeautyMeshesArePlanarSpikyAndFlatShaded()
        {
            Mesh star = HLSpellPrimitives.Star;
            foreach (Vector3 vertex in star.vertices)
            {
                Assert.AreEqual(0, vertex.z);
            }
            Assert.AreEqual(34, star.vertexCount);
            Assert.Greater(star.bounds.size.x, 1.9f);
            foreach (Vector3 normal in star.normals)
            {
                Assert.Greater(normal.sqrMagnitude, 0.99f);
            }
            Mesh boulder = HLSpellPrimitives.Boulder;
            Assert.AreEqual(24, boulder.vertexCount);
            Vector3[] normals = boulder.normals;
            for (int i = 0; i < normals.Length; i += 3)
            {
                Assert.AreEqual(normals[i], normals[i + 1]);
                Assert.AreEqual(normals[i], normals[i + 2]);
            }
            HLSpellPrimitives.Release();
        }

        [Test]
        public void HealingHasOneThinGroundedStalkPerSphere()
        {
            GameObject go = new GameObject("HLHeal");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = HLSpellEffectKind.Heal;
                fx.Initialize();
                Assert.AreEqual(fx.parts.Length, fx.stalks.Length);
                for (int i = 0; i < fx.parts.Length; i++)
                {
                    Assert.Less(fx.stalks[i].localScale.x, 0.015f);
                    Assert.AreEqual(
                        fx.parts[i].localPosition.y,
                        fx.stalks[i].localPosition.y + fx.stalks[i].localScale.y,
                        0.00001f
                    );
                }
            }
            finally
            {
                DestroyHost(go);
            }
        }

        [Test]
        public void ReleaseDoesNotDestroyPersistentMesh()
        {
            HLSpellPrimitives.Release();
            Mesh mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Render/Spells/Data/HLTorus.asset");
            Assert.IsNotNull(mesh);
            // Model an external authoring tool persisting the public cached mesh.
            typeof(HLSpellPrimitives)
                .GetField("_torus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .SetValue(null, mesh);
            HLSpellPrimitives.Release();
            Assert.IsTrue(mesh);
            Assert.IsTrue(UnityEditor.EditorUtility.IsPersistent(mesh));
            Assert.AreNotSame(mesh, HLSpellPrimitives.Torus);
            HLSpellPrimitives.Release();
        }

        [Test]
        public void LastSinkReleasesMeshesAndFallbackButOtherSinkKeepsThemAlive()
        {
            GameObject a = new GameObject("HLFirstSink");
            GameObject b = new GameObject("HLSecondSink");
            try
            {
                HLSpellVisualSink first = a.AddComponent<HLSpellVisualSink>();
                HLSpellVisualSink second = b.AddComponent<HLSpellVisualSink>();
                TestHelpers.InvokePrivate(first, "OnEnable");
                TestHelpers.InvokePrivate(second, "OnEnable");
                first.ShowImpact(null, b, HLResourceKind.Health, -1, false);
                Mesh torus = HLSpellPrimitives.Torus;
                Mesh cone = HLSpellPrimitives.Cone;
                Material fallback = a.GetComponentInChildren<Renderer>().sharedMaterial;
                DestroyHost(a);
                Assert.IsTrue(torus);
                Assert.IsTrue(cone);
                Assert.IsTrue(fallback);
                DestroyHost(b);
                Assert.IsFalse(torus);
                Assert.IsFalse(cone);
                Assert.IsFalse(fallback);
                HLSpellPrimitives.Release(); // Idempotent.
                Assert.IsTrue(HLSpellPrimitives.Torus);
                Assert.AreNotSame(torus, HLSpellPrimitives.Torus);
            }
            finally
            {
                if (a)
                {
                    DestroyHost(a);
                }
                if (b)
                {
                    DestroyHost(b);
                }
                HLSpellPrimitives.Release();
            }
        }

        [Test]
        public void StandaloneEffectKeepsCacheAliveAfterSinkDestruction()
        {
            GameObject host = new GameObject("HLSink");
            GameObject effectHost = new GameObject("HLEffect");
            try
            {
                TestHelpers.InvokePrivate(host.AddComponent<HLSpellVisualSink>(), "OnEnable");
                effectHost.AddComponent<HLSpellEffect>().Initialize();
                Mesh mesh = HLSpellPrimitives.Torus;
                DestroyHost(host);
                Assert.IsTrue(mesh);
                DestroyHost(effectHost);
                Assert.IsFalse(mesh);
            }
            finally
            {
                if (host)
                {
                    DestroyHost(host);
                }
                if (effectHost)
                {
                    DestroyHost(effectHost);
                }
            }
        }

        [TestCase(
            HLOperation.Resource,
            AttributeType.HealthMax,
            HLSign.Positive,
            HLTempo.Immediate,
            HLSpellEffectKind.Heal
        )]
        [TestCase(
            HLOperation.Resource,
            AttributeType.HealthMax,
            HLSign.Negative,
            HLTempo.HandlerTick,
            HLSpellEffectKind.Drip
        )]
        [TestCase(
            HLOperation.Attribute,
            AttributeType.FlatArmor,
            HLSign.Positive,
            HLTempo.Continuous,
            HLSpellEffectKind.Shield
        )]
        [TestCase(
            HLOperation.Attribute,
            AttributeType.AttackRate,
            HLSign.Conditional,
            HLTempo.Continuous,
            HLSpellEffectKind.Buff
        )]
        public void SemanticAxesChooseShape(
            HLOperation operation,
            AttributeType attribute,
            HLSign sign,
            HLTempo tempo,
            HLSpellEffectKind expected
        ) =>
            Assert.AreEqual(
                expected,
                HLSpellPrimitives.Kind(
                    new HLSpellSignature
                    {
                        operation = operation,
                        attribute = attribute,
                        hasAttribute = true,
                        sign = sign,
                        tempo = tempo,
                    }
                )
            );

        [TestCase(HLSpellEffectKind.Litter, 5)]
        [TestCase(HLSpellEffectKind.Drip, 5)]
        [TestCase(HLSpellEffectKind.Area, 1)]
        [TestCase(HLSpellEffectKind.Buff, 3)]
        [TestCase(HLSpellEffectKind.Shield, 6)]
        [TestCase(HLSpellEffectKind.Heal, 7)]
        [TestCase(HLSpellEffectKind.Impact, 5)]
        [TestCase(HLSpellEffectKind.Chain, 32)]
        public void AssembliesArePrimitiveOnlyAndHaveNoColliders(HLSpellEffectKind kind, int count)
        {
            GameObject go = new GameObject("HLPrimitiveTest");
            try
            {
                HLSpellEffect fx = go.AddComponent<HLSpellEffect>();
                fx.kind = kind;
                fx.Initialize();
                Assert.AreEqual(count, fx.parts.Length);
                Assert.IsEmpty(go.GetComponentsInChildren<Collider>());
                foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
                {
                    Assert.Greater(filter.sharedMesh.vertexCount, 0);
                }
            }
            finally
            {
                DestroyHost(go);
            }
        }
    }
}

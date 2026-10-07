using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Deliveries
{
    public class DeliveryFragmentTests
    {
        GameObject _parent;
        DeliveryVocabulary _vocabulary;
        DeliveryTip _tip;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("DeliveryFragmentTest");
            _vocabulary = ScriptableObject.CreateInstance<DeliveryVocabulary>();
            _tip = new DeliveryTip();
        }

        [TearDown]
        public void TearDown()
        {
            _tip.Release();
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_vocabulary);
        }

        static LookPart Part(string id, ShapeProfile profile) => new LookPart
        {
            id = id, primitive = Primitive.Sphere, shape = profile,
            role = PartRole.Tip, colour = ColourRole.Accent, size = Vector3.one
        };

        void Draw()
        {
            _tip.Draw(_parent.transform, DeliveryTip.Frame(Vector3.zero, Vector3.forward, 1f),
                RenderTestAssets.LoadLookMaterial(), Color.red, Color.green);
        }

        [Test]
        public void SetStyle_ResolvesProceduralAttachmentsAndPivotsWithoutMutatingVocabulary()
        {
            LookPart stem = Part("Stem", ShapeProfile.Segment());
            stem.size = new Vector3(0.4f, 1.5f, 0.4f);
            stem.euler = new Vector3(0f, 0f, 25f);
            LookPart tip = Part("Leaf", ShapeProfile.Leaf());
            tip.attachTo = "Stem";
            tip.attachAt = ShapeAnchor.Top;
            tip.pivot = ShapeAnchor.Bottom;
            _vocabulary.tips[DeliveryStyle.Direct] = new[] { stem, tip };
            Assert.IsTrue(FragmentPlacement.TryResolve(_vocabulary.GetTip(DeliveryStyle.Direct), CountBand.Many,
                0, 0, out LookPart[] expected, out string error), error);
            _tip.SetStyle(DeliveryStyle.Direct, _vocabulary, RenderTestAssets.LoadMeshes());
            Draw();
            Assert.AreEqual(expected[1].position, _tip.Part(1).position);
            Transform leaf = _parent.transform.Find("DeliveryTip/Leaf");
            Assert.AreEqual(expected[1].position, leaf.localPosition);
            Assert.AreNotSame(RenderTestAssets.LoadMeshes().sphere, leaf.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual(Vector3.zero, _vocabulary.GetTip(DeliveryStyle.Direct)[1].position);
        }

        [Test]
        public void StyleReplacementAndRelease_DestroyOnlyOwnedProceduralMeshes()
        {
            _vocabulary.tips[DeliveryStyle.Direct] = new[] { Part("Bulb", ShapeProfile.Bulb()) };
            _tip.SetStyle(DeliveryStyle.Direct, _vocabulary, RenderTestAssets.LoadMeshes());
            Draw();
            Mesh first = _parent.GetComponentInChildren<MeshFilter>().sharedMesh;
            _vocabulary.tips[DeliveryStyle.Direct] = new[] { Part("Leaf", ShapeProfile.Leaf()) };
            _tip.SetStyle(DeliveryStyle.Direct, _vocabulary, RenderTestAssets.LoadMeshes());
            Assert.IsTrue(first == null);
            Draw();
            Mesh second = _parent.GetComponentInChildren<MeshFilter>().sharedMesh;
            Assert.IsTrue(second);
            _tip.Release();
            Assert.IsTrue(second == null);
            Assert.IsTrue(RenderTestAssets.LoadMeshes().sphere);
            Assert.DoesNotThrow(Draw);
        }

        [Test]
        public void InvalidAttachment_IsRejectedBeforeMeshAllocation()
        {
            LookPart part = Part("Leaf", ShapeProfile.Leaf());
            part.attachTo = "Absent";
            _vocabulary.tips[DeliveryStyle.Direct] = new[] { part };
            TestHelpers.WithLoggingDisabled(() =>
                _tip.SetStyle(DeliveryStyle.Direct, _vocabulary, RenderTestAssets.LoadMeshes()));
            Draw();
            Assert.AreEqual(0, _tip.partCount);
            Assert.AreEqual(0, _parent.transform.childCount);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures.Tests
{
    public class CreatureSourceTests
    {
        GameObject _root;
        GameObject _body;
        GameObject _tip;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("root");
            _body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _body.transform.SetParent(_root.transform, false);
            _tip.transform.SetParent(_root.transform, false);
            _body.transform.localPosition = Vector3.zero;
            _tip.transform.localPosition = new Vector3(0.2f, 2f, 0.1f);
            _tip.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_body);
            Object.DestroyImmediate(_tip);
        }

        [Test]
        public void ExplicitOutlets_FollowTheirOwnRotatedPartAndRetainAllSources()
        {
            CreaturePart[] parts =
            {
                new CreaturePart { id = "body", parent = -1, role = PartRole.Body },
                new CreaturePart { id = "left", parent = 0, role = PartRole.Tip, isSource = true },
            };
            Renderer[] renderers = { _body.GetComponent<Renderer>(), _tip.GetComponent<Renderer>() };
            Assert.IsTrue(PartAnchors.TryMeasure(
                parts, Vector3.up, new Vector3[0], _root.transform, _root.transform, renderers, 1f,
                out EffectAnchors anchors));

            Vector3 expected = renderers[1].transform.TransformPoint(renderers[1].localBounds.max);
            Assert.AreEqual(1, anchors.castSources.Length);
            Assert.Less(Vector3.Distance(expected, anchors.castSources[0]), 0.00001f);
            Assert.AreEqual(anchors.castSources[0], anchors.castPoint);

            _tip.transform.localPosition += Vector3.right;
            Assert.IsTrue(PartAnchors.TryMeasure(
                parts, Vector3.up, new Vector3[0], _root.transform, _root.transform, renderers, 1f,
                out EffectAnchors moved));
            Assert.Greater(Vector3.Distance(anchors.castPoint, moved.castPoint), 0.5f);
        }

        [Test]
        public void LegacyRecipe_UsesMeasuredHeadWhenNoOutletMetadataExists()
        {
            CreaturePart[] parts =
            {
                new CreaturePart { id = "body", parent = -1, role = PartRole.Body },
                new CreaturePart { id = "head", parent = 0, role = PartRole.Head },
            };
            Renderer[] renderers = { _body.GetComponent<Renderer>(), _tip.GetComponent<Renderer>() };
            Assert.IsTrue(PartAnchors.TryMeasure(
                parts, Vector3.up, new Vector3[0], _root.transform, _root.transform, renderers, 1f,
                out EffectAnchors anchors));
            Assert.AreEqual(1, anchors.castSources.Length);
            Assert.AreEqual(anchors.castSources[0], anchors.castPoint);
        }
    }
}

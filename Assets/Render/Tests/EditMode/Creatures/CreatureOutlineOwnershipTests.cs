using System.Collections.Generic;
using HealerLike.Render.Stones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureOutlineOwnershipTests : CreatureMeshOwnershipFixture
    {
        [Test]
        public void LegacyOutline_CannotReachRenderGeometryOrChangeItsMaterials()
        {
            Tick(0f);
            StoneGroundDisc shadow = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
            shadow.GetComponent<MeshFilter>().sharedMesh = _source;
            shadow.Attach(_attachment);
            shadow.Init(new Bounds(Vector3.zero, Vector3.one), Vector3.up, null);
            shadow.Show(true);
            Renderer renderer = _rig.partTransforms[0].GetComponent<Renderer>();
            Material[] originalMaterials = renderer.sharedMaterials;
            GameObject legacy = new GameObject("Legacy", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            legacy.transform.SetParent(_owner.transform, false);
            Mesh legacyMesh = Object.Instantiate(_source);
            legacy.GetComponent<MeshFilter>().sharedMesh = legacyMesh;
            Renderer legacyRenderer = legacy.GetComponent<Renderer>();
            legacyRenderer.sharedMaterials = originalMaterials;
            int[] sourceTriangles = _source.triangles;
            Outline outline = _owner.AddComponent<Outline>();
            Material mask = null;
            Material fill = null;
            bool applied = false;
            try
            {
                // EditMode fixtures explicitly drive the same callbacks as the player loop.
                TestHelpers.InvokePrivate(outline, "Awake");
                TestHelpers.InvokePrivate(outline, "OnEnable");
                applied = true;
                Material[] selectedMaterials = legacyRenderer.sharedMaterials;
                mask = selectedMaterials[selectedMaterials.Length - 2];
                fill = selectedMaterials[selectedMaterials.Length - 1];
                Assert.AreEqual(3, legacyMesh.subMeshCount, "The real legacy Outline executed its scan.");
                Assert.AreEqual(originalMaterials.Length + 2, selectedMaterials.Length);
                Assert.AreSame(_source, BodyMesh());
                Assert.AreEqual(2, _source.subMeshCount);
                CollectionAssert.AreEqual(sourceTriangles, _source.triangles);
                List<Vector3> uvs = new List<Vector3>();
                _source.GetUVs(3, uvs);
                CollectionAssert.AreEqual(_sourceUVs, uvs);
                CollectionAssert.AreEqual(originalMaterials, renderer.sharedMaterials);
                Assert.IsFalse(shadow.transform.IsChildOf(_owner.transform));
                Assert.IsTrue(shadow.gameObject.activeInHierarchy);
                Assert.AreSame(_source, shadow.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsTrue(legacy.GetComponent<BoxCollider>().enabled);
                Assert.IsTrue(legacy.transform.IsChildOf(_owner.transform));
                foreach (MeshFilter filter in _rig.root.GetComponentsInChildren<MeshFilter>())
                {
                    Assert.IsFalse(filter.transform.IsChildOf(_owner.transform));
                }

                TestHelpers.InvokePrivate(outline, "OnDisable");
                applied = false;
                CollectionAssert.AreEqual(originalMaterials, renderer.sharedMaterials);
                CollectionAssert.AreEqual(originalMaterials, legacyRenderer.sharedMaterials);
            }
            finally
            {
                if (applied)
                {
                    TestHelpers.InvokePrivate(outline, "OnDisable");
                }
                TestHelpers.WithLoggingDisabled(() => Object.DestroyImmediate(outline));
                Object.DestroyImmediate(mask);
                Object.DestroyImmediate(fill);
                Object.DestroyImmediate(legacyMesh);
            }
        }
    }
}

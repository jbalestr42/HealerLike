using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureOutlineOwnershipTests : CreatureMeshOwnershipFixture
    {
        [Test]
        public void LegacyOutline_MutatesInstanceAndSelectionMaterialsButLeavesBorrowedSourceUntouched()
        {
            Mesh copy = BodyMesh();
            Renderer renderer = _rig.partTransforms[0].GetComponent<Renderer>();
            Material[] originalMaterials = renderer.sharedMaterials;
            // This fixture aliases one two-submesh source across body and roots; every use needs both slots.
            foreach (MeshFilter filter in _rig.root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == copy)
                {
                    filter.GetComponent<Renderer>().sharedMaterials = originalMaterials;
                }
            }

            int[] sourceTriangles = _source.triangles;
            int sourceSubmeshes = _source.subMeshCount;
            Outline outline = _owner.AddComponent<Outline>();
            Material mask = null;
            Material fill = null;
            bool applied = false;
            try
            {
                // Normal player-loop messages do not run automatically in an EditMode fixture.
                TestHelpers.InvokePrivate(outline, "Awake");
                TestHelpers.InvokePrivate(outline, "OnEnable");
                applied = true;
                Material[] selectedMaterials = renderer.sharedMaterials;
                mask = selectedMaterials[selectedMaterials.Length - 2];
                fill = selectedMaterials[selectedMaterials.Length - 1];
                Assert.AreEqual(sourceSubmeshes + 1, copy.subMeshCount);
                CollectionAssert.AreEqual(sourceTriangles, copy.GetTriangles(copy.subMeshCount - 1));
                Assert.AreEqual(sourceSubmeshes, _source.subMeshCount);
                CollectionAssert.AreEqual(sourceTriangles, _source.triangles);
                List<Vector3> uvs = new List<Vector3>();
                _source.GetUVs(3, uvs);
                CollectionAssert.AreEqual(_sourceUVs, uvs);
                copy.GetUVs(3, uvs);
                CollectionAssert.AreNotEqual(_sourceUVs, uvs);

                Assert.AreEqual(originalMaterials.Length + 2, selectedMaterials.Length);
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    Assert.AreSame(originalMaterials[i], selectedMaterials[i]);
                }

                TestHelpers.InvokePrivate(outline, "OnDisable");
                applied = false;
                CollectionAssert.AreEqual(originalMaterials, renderer.sharedMaterials);
                Assert.AreEqual(sourceSubmeshes + 1, copy.subMeshCount,
                    "Containment deliberately retains the legacy instance mutation.");
            }
            finally
            {
                if (applied)
                {
                    TestHelpers.InvokePrivate(outline, "OnDisable");
                }

                // QuickOutline uses play-mode Destroy in OnDestroy; the fixture releases its materials itself.
                TestHelpers.WithLoggingDisabled(() => Object.DestroyImmediate(outline));
                Object.DestroyImmediate(mask);
                Object.DestroyImmediate(fill);
            }
        }
    }
}

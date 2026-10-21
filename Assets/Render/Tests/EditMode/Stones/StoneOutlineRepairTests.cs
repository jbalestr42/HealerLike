using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stones
{
    public class StoneOutlineRepairTests
    {
        Mesh _mesh;

        [SetUp]
        public void SetUp()
        {
            _mesh = StoneVariantBaker.CreateVariant(17);
            _mesh.name = "Hand tuned stone";
            Vector3[] vertices = _mesh.vertices;
            vertices[0] += new Vector3(0.014f, -0.032f, 0.025f);
            _mesh.vertices = vertices;
            List<Vector4> uv = new List<Vector4>();
            for (int i = 0; i < vertices.Length; i++)
            {
                uv.Add(new Vector4(i / 60f, 0.14f, 0.37f, -0.51f));
            }
            _mesh.SetUVs(0, uv);
            _mesh.SetUVs(7, uv);
            _mesh.bounds = new Bounds(new Vector3(2f, 3f, 4f), new Vector3(5f, 6f, 7f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mesh);
        }

        void AddOutlineDuplicate()
        {
            int[] surface = _mesh.triangles;
            _mesh.subMeshCount = 3;
            _mesh.SetTriangles(surface, 2, false);
        }

        [Test]
        public void TryRepair_ExactOutlineDuplicate_PreservesAuthoredStreamsAndTwoSubmeshes()
        {
            Vector3[] vertices = _mesh.vertices;
            Vector3[] normals = _mesh.normals;
            List<Vector3> outline = new List<Vector3>();
            List<Vector4> uv = new List<Vector4>();
            _mesh.GetUVs(3, outline);
            _mesh.GetUVs(7, uv);
            int[] grey = _mesh.GetIndices(0, false);
            int[] ochre = _mesh.GetIndices(1, false);
            SubMeshDescriptor first = _mesh.GetSubMesh(0);
            SubMeshDescriptor second = _mesh.GetSubMesh(1);
            Bounds bounds = _mesh.bounds;
            AddOutlineDuplicate();
            // Appending the synthetic submesh can reset mesh bounds in Unity. The repair must preserve
            // authored bounds present on its input, including a custom box restored after that setup.
            _mesh.bounds = bounds;
            string authored = StoneMeshFingerprint.Authored(_mesh);

            Assert.IsTrue(StoneOutlineRepair.TryRepair(_mesh, out string reason), reason);

            Assert.AreEqual(2, _mesh.subMeshCount);
            CollectionAssert.AreEqual(vertices, _mesh.vertices);
            CollectionAssert.AreEqual(normals, _mesh.normals);
            CollectionAssert.AreEqual(grey, _mesh.GetIndices(0, false));
            CollectionAssert.AreEqual(ochre, _mesh.GetIndices(1, false));
            Assert.AreEqual(first, _mesh.GetSubMesh(0));
            Assert.AreEqual(second, _mesh.GetSubMesh(1));
            Assert.AreEqual(bounds, _mesh.bounds);
            Assert.AreEqual("Hand tuned stone", _mesh.name);
            List<Vector3> afterOutline = new List<Vector3>();
            List<Vector4> afterUv = new List<Vector4>();
            _mesh.GetUVs(3, afterOutline);
            _mesh.GetUVs(7, afterUv);
            CollectionAssert.AreEqual(outline, afterOutline);
            CollectionAssert.AreEqual(uv, afterUv);
            Assert.AreEqual(authored, StoneMeshFingerprint.Authored(_mesh));
        }

        [Test]
        public void TryRepair_CleanMeshAndRepeatedRepair_LeaveAuthoredDataUntouched()
        {
            string before = StoneMeshFingerprint.Authored(_mesh);
            Assert.IsTrue(StoneOutlineRepair.TryRepair(_mesh, out _));
            Assert.IsTrue(StoneOutlineRepair.TryRepair(_mesh, out _));
            Assert.AreEqual(StoneOutlineState.Clean, StoneOutlineRepair.Inspect(_mesh, out _));
            Assert.AreEqual(before, StoneMeshFingerprint.Authored(_mesh));
            Assert.AreEqual(2, _mesh.subMeshCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TryRepair_PartialOrReorderedThirdSurface_RefusesWithoutMutation(bool reordered)
        {
            int[] surface = _mesh.triangles;
            _mesh.subMeshCount = 3;
            if (reordered)
            {
                int first = surface[0];
                surface[0] = surface[1];
                surface[1] = first;
            }
            else
            {
                System.Array.Resize(ref surface, surface.Length - 3);
            }
            _mesh.SetTriangles(surface, 2, false);
            string before = StoneMeshFingerprint.Authored(_mesh);

            Assert.IsFalse(StoneOutlineRepair.TryRepair(_mesh, out _));

            Assert.AreEqual(3, _mesh.subMeshCount);
            CollectionAssert.AreEqual(surface, _mesh.GetIndices(2));
            Assert.AreEqual(before, StoneMeshFingerprint.Authored(_mesh));
        }

        [Test]
        public void TryRepair_AdditionalSubmesh_RefusesWithoutRemovingIt()
        {
            AddOutlineDuplicate();
            _mesh.subMeshCount = 4;
            _mesh.SetTriangles(new[] { 0, 1, 2 }, 3, false);
            Assert.IsFalse(StoneOutlineRepair.TryRepair(_mesh, out _));
            Assert.AreEqual(4, _mesh.subMeshCount);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _mesh.GetIndices(3));
        }

        [Test]
        public void TryRepair_OverlappingAuthoredSubmeshes_RefusesAmbiguousSurface()
        {
            int[] ochre = _mesh.GetIndices(1);
            ochre[0] = _mesh.GetIndices(0)[0];
            _mesh.SetTriangles(ochre, 1, false);
            AddOutlineDuplicate();
            Assert.IsFalse(StoneOutlineRepair.TryRepair(_mesh, out _));
            Assert.AreEqual(3, _mesh.subMeshCount);
            CollectionAssert.AreEqual(ochre, _mesh.GetIndices(1));
        }

        [Test]
        public void TryRepair_NonTriangleDuplicate_RefusesWithoutReinterpretingIndices()
        {
            int[] surface = _mesh.triangles;
            _mesh.subMeshCount = 3;
            _mesh.SetIndices(surface, MeshTopology.Lines, 2, false);
            Assert.IsFalse(StoneOutlineRepair.TryRepair(_mesh, out _));
            Assert.AreEqual(MeshTopology.Lines, _mesh.GetTopology(2));
        }

        [Test]
        public void TryRepair_UnreadableMesh_RefusesBeforeReadingOrChangingBuffers()
        {
            AddOutlineDuplicate();
            _mesh.UploadMeshData(true);
            Assert.IsFalse(StoneOutlineRepair.TryRepair(_mesh, out _));
            Assert.AreEqual(3, _mesh.subMeshCount);
        }
    }
}

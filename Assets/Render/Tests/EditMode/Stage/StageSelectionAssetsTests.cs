using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public class StageSelectionAssetsTests
    {
        GameObject _root;
        Mesh _mesh;
        Material _material;
        Renderer _renderer;
        StageSelectionAssets _guard;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Selection asset observation fixture");
            _mesh = new Mesh();
            _mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            _mesh.triangles = new[] { 0, 1, 2 };
            _mesh.SetUVs(3, new List<Vector4> { Vector4.one, Vector4.one, Vector4.one });
            _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _material = new Material(RenderTestAssets.LoadLookMaterial());
            _renderer = _root.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _guard = new StageSelectionAssets(new[] { _renderer });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_mesh);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void Verify_PropertyBlockHighlight_LeavesBorrowedAssetsUnchanged()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Color.cyan);
            block.SetFloat("_HLOutlineWidthMultiplier", 4f);
            _renderer.SetPropertyBlock(block);

            Assert.DoesNotThrow(_guard.Verify);
        }

        [Test]
        public void Verify_OutlineNormalsChanged_RejectsSharedMeshMutation()
        {
            _mesh.SetUVs(3, new List<Vector4> { Vector4.zero, Vector4.one, Vector4.one });

            Assert.Throws<InvalidOperationException>(_guard.Verify);
        }

        [Test]
        public void Verify_SubmeshAppended_RejectsQuickOutlineStyleMutation()
        {
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(new[] { 0, 1, 2 }, 1);

            Assert.Throws<InvalidOperationException>(_guard.Verify);
        }

        [Test]
        public void Verify_TriangleOrderChanged_RejectsTopologyMutationWithoutCountChange()
        {
            _mesh.triangles = new[] { 1, 0, 2 };

            Assert.Throws<InvalidOperationException>(_guard.Verify);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Verify_SharedMaterialPaintChanged_RejectsMutation(bool outline)
        {
            if (outline)
            {
                _material.SetFloat("_HLOutlineWidthMultiplier", 9f);
            }
            else
            {
                _material.SetColor("_BaseColor", Color.magenta);
            }

            Assert.Throws<InvalidOperationException>(_guard.Verify);
        }

        [Test]
        public void Verify_MaterialSlotChanged_RejectsBindingMutation()
        {
            _renderer.sharedMaterials = new[] { _material, _material };

            Assert.Throws<InvalidOperationException>(_guard.Verify);
        }
    }
}

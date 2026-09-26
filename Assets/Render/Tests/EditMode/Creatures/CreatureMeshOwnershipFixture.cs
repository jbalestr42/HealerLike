using System.Collections.Generic;
using HealerLike.Render.Deliveries;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public abstract class CreatureMeshOwnershipFixture
    {
        protected GameObject _owner;
        protected Mesh _source;
        protected PrimitiveMeshes _meshes;
        protected Material _material;
        protected CreatureRecipe _recipe;
        protected CreatureRig _rig;
        protected ArmPool _pool;
        protected List<Vector3> _sourceUVs;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("MeshOwnership");
            _source = new Mesh
            {
                name = "MeshOwnershipSource",
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.one },
                normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward }
            };
            _source.subMeshCount = 2;
            _source.SetTriangles(new[] { 0, 1, 2 }, 0);
            _source.SetTriangles(new[] { 1, 3, 2 }, 1);
            _sourceUVs = new List<Vector3>();
            for (int i = 0; i < _source.vertexCount; i++)
            {
                _sourceUVs.Add(new Vector3(0.2f, 0.3f, 0.4f));
            }

            _source.SetUVs(3, _sourceUVs);
            _meshes = ScriptableObject.CreateInstance<PrimitiveMeshes>();
            _meshes.sphere = _source;
            _meshes.cylinder = _source;
            _meshes.cone = _source;
            _recipe = RenderTestAssets.CreateRecipe();
            _recipe.idle = default;
            _material = new Material(RenderTestAssets.LoadLookMaterial());
            _material.enableInstancing = false;
            _rig = new CreatureRig();
            Assert.IsTrue(_rig.Init(_recipe, _owner.transform, _material, _material, _meshes, 1f, true));
            _pool = new ArmPool();
            _pool.Init(_rig, _material, _meshes, null, true);
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Dispose();
            _rig.Dispose();
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_meshes);
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_recipe);
        }

        protected Mesh BodyMesh()
        {
            return _rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
        }

        protected Mesh TipMesh()
        {
            foreach (MeshFilter filter in _rig.root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.transform.parent.name == "DeliveryTip")
                {
                    return filter.sharedMesh;
                }
            }

            return null;
        }

        protected void Tick(float deltaTime)
        {
            _rig.Tick(0f, deltaTime, new FootFrame(Vector3.zero, Vector3.up, 1f));
            _pool.Tick(deltaTime);
        }

        protected void EditSource()
        {
            Vector3[] vertices = _source.vertices;
            vertices[0] += Vector3.right * 0.3f;
            _source.vertices = vertices;
            _source.RecalculateBounds();
        }
    }
}

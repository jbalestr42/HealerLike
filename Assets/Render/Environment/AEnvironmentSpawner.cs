using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Stones;
using UnityEngine;

namespace HealerLike.Render.Environment
{
    // What the scatter, the foreground and the ridge share: one root of spawned parts under this object,
    // the stone meshes they generate and own, and the property block their colours go through
    public abstract class AEnvironmentSpawner : MonoBehaviour
    {
        [SerializeField] protected Material _plantMaterial;
        [SerializeField] protected Material _stoneMaterial;

        protected PrimitiveMeshes _meshes;

        readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        MaterialPropertyBlock _properties;

        Transform _root;
        public Transform root { get { return _root; } }

        void OnEnable()
        {
            if (_root)
            {
                _root.gameObject.SetActive(true);
            }
        }

        void OnDisable()
        {
            if (_root)
            {
                _root.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            Clear();
        }

        public void Clear()
        {
            if (_root)
            {
                _root.gameObject.SetActive(false);
                RenderObjects.Release(_root.gameObject);
            }

            _root = null;
            foreach (Mesh mesh in _ownedMeshes)
            {
                RenderObjects.Release(mesh);
            }

            _ownedMeshes.Clear();
        }

        // Drops the last build and opens an empty root for the next one
        protected Transform CreateRoot(string name)
        {
            Clear();
            _root = new GameObject(name).transform;
            _root.SetParent(transform, false);
            _properties = new MaterialPropertyBlock();
            return _root;
        }

        // A seeded stone mesh this spawner releases on its next clear
        protected Mesh CreateStone(uint seed, StoneSettings shape, string name)
        {
            Mesh mesh = StoneMesh.CreateMesh(seed, shape);
            mesh.name = name;
            _ownedMeshes.Add(mesh);
            return mesh;
        }

        // Places the part so the lowest point of its mesh sits on bottom, along the part's own up axis
        protected MeshRenderer Part(Transform parent, Mesh mesh, Material material, Vector3 bottom,
                                    Quaternion rotation, Vector3 scale, Color colour, string name)
        {
            GameObject partGo = new GameObject(name);
            partGo.transform.SetParent(parent, false);
            partGo.transform.localRotation = rotation;
            partGo.transform.localScale = scale;
            partGo.transform.localPosition = bottom + rotation * new Vector3(0f, -mesh.bounds.min.y * scale.y, 0f);
            return PrimitiveMeshes.Geometry(partGo, mesh, material, colour, 0f, _properties);
        }
    }
}

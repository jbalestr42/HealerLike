using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Selection may change property blocks, never a renderer's shared meshes or materials.
    public class StageSelectionAssets
    {
        class MeshState
        {
            public Mesh mesh;
            public int vertices;
            public readonly List<int[]> indices = new List<int[]>();
            public readonly List<MeshTopology> topology = new List<MeshTopology>();
            public readonly List<Vector4> normals = new List<Vector4>();
        }

        class RendererState
        {
            public Renderer renderer;
            public Mesh mesh;
            public Material[] materials;
        }

        class MaterialState
        {
            public Material material;
            public Shader shader;
            public bool hasColour;
            public bool hasWidth;
            public Color colour;
            public float width;
        }

        readonly List<MeshState> _meshes = new List<MeshState>();
        readonly List<RendererState> _renderers = new List<RendererState>();
        readonly List<MaterialState> _materials = new List<MaterialState>();

        public StageSelectionAssets(IEnumerable<Renderer> renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                _renderers.Add(new RendererState { renderer = renderer, mesh = filter.sharedMesh,
                    materials = materials });
                AddMesh(filter.sharedMesh);
                foreach (Material material in materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    MaterialState state = new MaterialState { material = material, shader = material.shader,
                        hasColour = material.HasProperty("_BaseColor"),
                        hasWidth = material.HasProperty("_HLOutlineWidthMultiplier") };
                    if (state.hasColour)
                    {
                        state.colour = material.GetColor("_BaseColor");
                    }

                    if (state.hasWidth)
                    {
                        state.width = material.GetFloat("_HLOutlineWidthMultiplier");
                    }

                    _materials.Add(state);
                }
            }
        }

        public void AddMesh(Mesh mesh)
        {
            if (mesh == null || _meshes.Exists(state => state.mesh == mesh))
            {
                return;
            }

            MeshState snapshot = new MeshState { mesh = mesh, vertices = mesh.vertexCount };
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                snapshot.indices.Add(mesh.GetIndices(i));
                snapshot.topology.Add(mesh.GetTopology(i));
            }

            mesh.GetUVs(3, snapshot.normals);
            _meshes.Add(snapshot);
        }

        public void Verify()
        {
            foreach (RendererState state in _renderers)
            {
                if (state.renderer == null || state.renderer.GetComponent<MeshFilter>().sharedMesh != state.mesh
                    || !Equal(state.materials, state.renderer.sharedMaterials))
                {
                    throw new InvalidOperationException("Selection changed shared renderer mesh/material bindings.");
                }
            }

            foreach (MaterialState state in _materials)
            {
                if (state.material == null || state.material.shader != state.shader
                    || (state.hasColour && state.material.GetColor("_BaseColor") != state.colour)
                    || (state.hasWidth && state.material.GetFloat("_HLOutlineWidthMultiplier") != state.width))
                {
                    throw new InvalidOperationException("Selection changed a shared material colour or outline.");
                }
            }

            foreach (MeshState state in _meshes)
            {
                Mesh mesh = state.mesh;
                if (mesh == null || mesh.vertexCount != state.vertices || mesh.subMeshCount != state.indices.Count)
                {
                    throw new InvalidOperationException("Selection changed shared mesh vertex/submesh counts.");
                }

                List<Vector4> normals = new List<Vector4>();
                mesh.GetUVs(3, normals);
                if (!Equal(state.normals, normals))
                {
                    throw new InvalidOperationException("Selection changed shared mesh outline normals in UV3.");
                }

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    if (state.topology[i] != mesh.GetTopology(i) || !Equal(state.indices[i], mesh.GetIndices(i)))
                    {
                        throw new InvalidOperationException("Selection changed shared mesh indices or topology.");
                    }
                }
            }
        }

        static bool Equal<Item>(IList<Item> first, IList<Item> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (!EqualityComparer<Item>.Default.Equals(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

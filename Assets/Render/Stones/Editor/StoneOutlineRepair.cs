using System;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public enum StoneOutlineState
    {
        Refused,
        Clean,
        Duplicate
    }

    // Only the known flat, twenty-face stone layout can be repaired without interpreting authored geometry.
    public static class StoneOutlineRepair
    {
        public static StoneOutlineState Inspect(Mesh mesh, out string reason)
        {
            reason = "Expected a readable stone mesh with two or three submeshes.";
            if (mesh == null || !mesh.isReadable || (mesh.subMeshCount != 2 && mesh.subMeshCount != 3))
            {
                return StoneOutlineState.Refused;
            }

            reason = "Expected sixty flat-face vertices without skinning or blend shapes.";
            if (mesh.vertexCount != 60 || mesh.blendShapeCount != 0 || mesh.bindposes.Length != 0)
            {
                return StoneOutlineState.Refused;
            }

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (mesh.GetTopology(i) != MeshTopology.Triangles)
                {
                    reason = "Every submesh must contain triangles.";
                    return StoneOutlineState.Refused;
                }
            }

            int[] first = mesh.GetIndices(0);
            int[] second = mesh.GetIndices(1);
            reason = "The two authored submeshes must partition exactly twenty flat triangles.";
            if (first.Length == 0 || second.Length == 0 || first.Length + second.Length != 60
                || first.Length % 3 != 0 || second.Length % 3 != 0)
            {
                return StoneOutlineState.Refused;
            }

            int[] surface = new int[60];
            Array.Copy(first, surface, first.Length);
            Array.Copy(second, 0, surface, first.Length, second.Length);
            bool[] used = new bool[mesh.vertexCount];
            foreach (int index in surface)
            {
                if (index < 0 || index >= used.Length || used[index])
                {
                    return StoneOutlineState.Refused;
                }
                used[index] = true;
            }

            if (mesh.subMeshCount == 2)
            {
                reason = "Already has only the two authored submeshes.";
                return StoneOutlineState.Clean;
            }

            reason = "Third submesh must exactly repeat the first two index sequences, in order.";
            int[] duplicate = mesh.GetIndices(2);
            if (mesh.GetBaseVertex(2) != 0 || mesh.GetSubMesh(0).indexStart != 0
                || mesh.GetSubMesh(1).indexStart != first.Length || mesh.GetSubMesh(2).indexStart != surface.Length
                || duplicate.Length != surface.Length)
            {
                return StoneOutlineState.Refused;
            }

            for (int i = 0; i < surface.Length; i++)
            {
                if (duplicate[i] != surface[i])
                {
                    return StoneOutlineState.Refused;
                }
            }

            reason = "Third submesh is the exact full-surface duplicate appended by QuickOutline.";
            return StoneOutlineState.Duplicate;
        }

        public static bool TryRepair(Mesh mesh, out string reason)
        {
            StoneOutlineState state = Inspect(mesh, out reason);
            if (state == StoneOutlineState.Refused)
            {
                return false;
            }

            if (state == StoneOutlineState.Clean)
            {
                return true;
            }

            string before = StoneMeshFingerprint.Authored(mesh);
            Mesh backup = UnityEngine.Object.Instantiate(mesh);
            try
            {
                backup.name = mesh.name;
                mesh.subMeshCount = 2;
                mesh.bounds = backup.bounds;
                if (StoneMeshFingerprint.Authored(mesh) != before)
                {
                    EditorUtility.CopySerialized(backup, mesh);
                    reason = "Removing the duplicate changed authored mesh data; refused the repair.";
                    return false;
                }

                reason = "Removed only the proven duplicate third submesh.";
                return true;
            }
            catch
            {
                EditorUtility.CopySerialized(backup, mesh);
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(backup);
            }
        }
    }
}

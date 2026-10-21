using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stones
{
    // Fingerprints the data the repair must preserve, including all raw vertex streams and both authored spans.
    public static class StoneMeshFingerprint
    {
        public static string FileHash(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 hash = SHA256.Create())
            {
                return Hex(hash.ComputeHash(stream));
            }
        }

        public static string Authored(Mesh mesh)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(mesh.name);
                writer.Write((int)mesh.indexFormat);
                writer.Write(mesh.vertexCount);
                Write(writer, mesh.bounds);
                VertexAttributeDescriptor[] attributes = mesh.GetVertexAttributes();
                writer.Write(attributes.Length);
                foreach (VertexAttributeDescriptor attribute in attributes)
                {
                    writer.Write((int)attribute.attribute);
                    writer.Write((int)attribute.format);
                    writer.Write(attribute.dimension);
                    writer.Write(attribute.stream);
                }

                using (Mesh.MeshDataArray data = Mesh.AcquireReadOnlyMeshData(mesh))
                {
                    writer.Write(mesh.vertexBufferCount);
                    for (int i = 0; i < mesh.vertexBufferCount; i++)
                    {
                        byte[] bytes = data[0].GetVertexData<byte>(i).ToArray();
                        writer.Write(mesh.GetVertexBufferStride(i));
                        writer.Write(bytes.Length);
                        writer.Write(bytes);
                    }
                }

                for (int i = 0; i < 2; i++)
                {
                    SubMeshDescriptor descriptor = mesh.GetSubMesh(i);
                    writer.Write(descriptor.indexStart);
                    writer.Write(descriptor.indexCount);
                    writer.Write((int)descriptor.topology);
                    writer.Write(descriptor.baseVertex);
                    writer.Write(descriptor.firstVertex);
                    writer.Write(descriptor.vertexCount);
                    Write(writer, descriptor.bounds);
                    int[] indices = mesh.GetIndices(i, false);
                    writer.Write(indices.Length);
                    foreach (int index in indices)
                    {
                        writer.Write(index);
                    }
                }

                writer.Flush();
                using (SHA256 hash = SHA256.Create())
                {
                    return Hex(hash.ComputeHash(stream.ToArray()));
                }
            }
        }

        static void Write(BinaryWriter writer, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            writer.Write(center.x);
            writer.Write(center.y);
            writer.Write(center.z);
            writer.Write(extents.x);
            writer.Write(extents.y);
            writer.Write(extents.z);
        }

        static string Hex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}

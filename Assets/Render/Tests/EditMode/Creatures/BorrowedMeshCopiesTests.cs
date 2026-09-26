using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class BorrowedMeshCopiesTests
    {
        [Test]
        public void Generations_CacheWithinOwnerAndReleaseIndependentlyAfterAnEdit()
        {
            Mesh source = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up } };
            source.triangles = new[] { 0, 1, 2 };
            using (BorrowedMeshCopies first = new BorrowedMeshCopies())
            using (BorrowedMeshCopies second = new BorrowedMeshCopies())
            {
                try
                {
                    Assert.IsNull(first.Get(null));
                    Mesh originalCopy = first.Get(source);
                    Assert.AreSame(originalCopy, first.Get(source));
                    Assert.AreNotSame(source, originalCopy);
                    source.vertices = new[] { Vector3.forward, Vector3.right, Vector3.up };
                    Mesh editedCopy = second.Get(source);
                    Assert.AreNotSame(originalCopy, editedCopy);
                    Assert.AreEqual(Vector3.zero, originalCopy.vertices[0]);
                    Assert.AreEqual(Vector3.forward, editedCopy.vertices[0]);
                    second.Dispose();
                    Assert.IsFalse(editedCopy);
                    Assert.IsTrue(originalCopy);
                    Assert.IsTrue(source);
                    first.Dispose();
                    Assert.IsFalse(originalCopy);
                    Assert.IsTrue(source);
                }
                finally
                {
                    Object.DestroyImmediate(source);
                }
            }
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class VertexStorageBuffersTests
{
    [TearDown]
    public void TearDown()
    {
        VertexStorageBuffers.isForcedUnavailable = false;
    }

    [Test]
    public void IsAvailable_FollowsTheDevicesVertexBufferInputs()
    {
        Assert.AreEqual(SystemInfo.maxComputeBufferInputsVertex >= 1, VertexStorageBuffers.isAvailable);
    }

    [Test]
    public void IsAvailable_ForcedUnavailable_IsFalseOnAnyDevice()
    {
        VertexStorageBuffers.isForcedUnavailable = true;

        Assert.IsFalse(VertexStorageBuffers.isAvailable);
    }
}

}

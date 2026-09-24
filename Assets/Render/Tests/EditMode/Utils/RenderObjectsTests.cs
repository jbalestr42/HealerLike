using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render
{

public class RenderObjectsTests
{
    GameObject _go;

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
        {
            Object.DestroyImmediate(_go);
        }
    }

    [Test]
    public void Release_EditMode_DestroysAtOnce()
    {
        _go = new GameObject("Released");

        RenderObjects.Release(_go);

        Assert.IsTrue(_go == null);
    }

    [Test]
    public void Release_Null_DoesNothing()
    {
        Assert.DoesNotThrow(() => RenderObjects.Release(null));
    }

    [Test]
    public void BaseColorId_MatchesThePropertyName()
    {
        Assert.AreEqual(Shader.PropertyToID("_BaseColor"), RenderObjects.BaseColorId);
    }
}

}

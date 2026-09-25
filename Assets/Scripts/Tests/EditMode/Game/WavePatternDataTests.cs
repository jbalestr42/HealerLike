using NUnit.Framework;
using UnityEngine;

namespace Game
{

public class WavePatternDataTests
{
    WavePatternData _wave;

    [SetUp]
    public void SetUp()
    {
        _wave = ScriptableObject.CreateInstance<WavePatternData>();
        _wave.width = 3;
        _wave.height = 2;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_wave);
    }

    [Test]
    public void GetSlotPosition_FirstSlot_IsOffsetByHalfThePatternSize()
    {
        Vector3 center = new Vector3(5f, 0.5f, 0f);

        Vector3 position = _wave.GetSlotPosition(center, 0, 0);

        Assert.AreEqual(new Vector3(3.5f, 0.5f, -1f), position);
    }

    [Test]
    public void GetSlotPosition_NextSlots_AreOneUnitApartOnXAndZ()
    {
        Vector3 origin = _wave.GetSlotPosition(Vector3.zero, 0, 0);

        Assert.AreEqual(origin + new Vector3(2f, 0f, 1f), _wave.GetSlotPosition(Vector3.zero, 2, 1));
    }

    [Test]
    public void GetSlotPosition_KeepsTheCenterHeight()
    {
        Assert.AreEqual(2f, _wave.GetSlotPosition(new Vector3(0f, 2f, 0f), 1, 1).y);
    }
}

}

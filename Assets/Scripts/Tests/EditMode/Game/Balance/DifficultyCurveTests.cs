using NUnit.Framework;

namespace Game.Balance
{

public class DifficultyCurveTests
{
    [Test]
    public void Floor0_IsAtTheThreatOfFloor0()
    {
        Assert.AreEqual(500f, new DifficultyCurve(500f, 0.15f).GetThreat(0f), 0.001f);
    }

    [Test]
    public void EachFloor_MultipliesTheThreatByTheGrowth()
    {
        DifficultyCurve curve = new DifficultyCurve(500f, 0.2f);

        Assert.AreEqual(600f, curve.GetThreat(1f), 0.001f);
        Assert.AreEqual(720f, curve.GetThreat(2f), 0.001f);
        Assert.AreEqual(curve.GetThreat(6f) * 1.2f, curve.GetThreat(7f), 0.01f);
    }

    [Test]
    public void Doubling_TheThreatEveryFloor()
    {
        Assert.AreEqual(500f * 1024f, new DifficultyCurve(500f, 1f).GetThreat(10f), 0.1f);
    }

    [Test]
    public void GetFloor_IsTheFloorWhereTheCurveReachesTheThreat()
    {
        DifficultyCurve curve = new DifficultyCurve(500f, 0.2f);

        Assert.AreEqual(0f, curve.GetFloor(500f), 0.0001f);
        Assert.AreEqual(2f, curve.GetFloor(720f), 0.0001f);
        Assert.AreEqual(4.5f, curve.GetFloor(curve.GetThreat(4.5f)), 0.0001f);
    }

    [Test]
    public void GetFloor_BelowTheThreatOfFloor0_IsNegative()
    {
        Assert.AreEqual(-1f, new DifficultyCurve(500f, 0.25f).GetFloor(400f), 0.0001f);
    }

    [Test]
    public void WithoutGrowth_TheCurveIsFlat_AndEveryThreatIsOnFloor0()
    {
        DifficultyCurve curve = new DifficultyCurve(500f, 0f);

        Assert.AreEqual(500f, curve.GetThreat(9f), 0.001f);
        Assert.AreEqual(0f, curve.GetFloor(2000f), 0.0001f);
    }

    [Test]
    public void ByDefault_Floor0IsAround500()
    {
        Assert.AreEqual(500f, new DifficultyCurve().GetThreat(0f), 0.001f);
    }
}

}

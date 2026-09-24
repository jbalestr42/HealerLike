using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{

public class GrassBoundsTests
{
    [Test]
    public void Envelope_OneCell_ReachesTheTallestTuftAndTheTallestSpike()
    {
        float tallestTuft = GrassLayout.TuftHeight * GrassLayout.MaxScale * GrassLayout.HealLift;
        float tuftHalfWidth = GrassLayout.TuftWidth * GrassLayout.MaxScale * 0.5f;

        float envelope = GrassBounds.Envelope(1f);

        Assert.That(envelope, Is.GreaterThanOrEqualTo(tallestTuft + tuftHalfWidth));
        Assert.That(envelope, Is.GreaterThanOrEqualTo(GrassLayout.SpikeHeight + GrassLayout.SpikeHalfWidth));
    }

    [Test]
    public void Envelope_LargeCells_GrowsWithTheTuft()
    {
        float tallestTuft = GrassLayout.TuftHeight * GrassLayout.MaxScale * GrassLayout.HealLift * 4f;

        Assert.That(GrassBounds.Envelope(4f), Is.GreaterThan(tallestTuft));
        Assert.That(GrassBounds.Envelope(4f), Is.GreaterThan(GrassBounds.Envelope(1f)));
    }

    [Test]
    public void Calculate_MainAndShiftedGrids_GrowByTheEnvelope()
    {
        Bounds main = GrassBounds.Calculate(16, 16, 1f, Vector3.zero, 0.5f);
        Bounds shifted = GrassBounds.Calculate(3, 7, 2f, new Vector3(4f, 9f, -5f), 3f);

        float mainEnvelope = GrassBounds.Envelope(1f);
        Assert.AreEqual(new Vector3(0f, 0.505f, 0f), main.center);
        Assert.AreEqual(16f + 2f * mainEnvelope, main.size.x, 0.0001f);
        Assert.AreEqual(2f * mainEnvelope, main.size.y, 0.0001f);
        Assert.AreEqual(16f + 2f * mainEnvelope, main.size.z, 0.0001f);
        float shiftedEnvelope = GrassBounds.Envelope(2f);
        Assert.AreEqual(new Vector3(4f, 3.005f, -5f), shifted.center);
        Assert.AreEqual(6f + 2f * shiftedEnvelope, shifted.size.x, 0.0001f); // 3 cells of 2
        Assert.AreEqual(14f + 2f * shiftedEnvelope, shifted.size.z, 0.0001f); // 7 cells of 2
    }

    [Test]
    public void Calculate_NegativeWidth_LogsAndReturnsEmptyBounds()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"^\[GrassBounds\] Rejected a -1 x 1 footprint"));

        Bounds bounds = GrassBounds.Calculate(-1, 1, 1f, Vector3.zero, 0f);

        Assert.AreEqual(new Bounds(), bounds);
    }
}

}

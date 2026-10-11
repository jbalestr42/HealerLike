using NUnit.Framework;

namespace Game
{

public class WaveScoreTests
{
    [Test]
    public void Threat_IsTheDamageDealtBeforeDying()
    {
        Assert.AreEqual(300f, WaveScore.GetThreat(20f, 15f), 0.0001f);
    }

    [Test]
    public void EffectiveHealth_IsTheDamageTheBalanceTeamDealsDuringTheSurvivalTime()
    {
        Assert.AreEqual(1000f, WaveScore.GetEffectiveHealth(20f, 50f), 0.0001f);
    }

    [Test]
    public void Fingerprint_IsTheSameForTheSameContents()
    {
        Assert.AreEqual(WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 3" }), WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 3" }));
    }

    [Test]
    public void Fingerprint_ChangesWhenADataTheWaveDependsOnChanges()
    {
        Assert.AreNotEqual(WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 3" }), WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 4" }));
    }

    [Test]
    public void NeverMeasured_IsNotUpToDate()
    {
        WaveScore score = new WaveScore();

        Assert.IsFalse(score.measured);
        Assert.IsFalse(score.IsUpToDate(WaveScore.ComputeFingerprint(new[] { "wave" })));
    }

    [Test]
    public void IsUpToDate_OnlyWithTheFingerprintOfTheMeasure()
    {
        string measuredFingerprint = WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 3" });
        WaveScore score = new WaveScore { fingerprint = measuredFingerprint };

        Assert.IsTrue(score.IsUpToDate(measuredFingerprint));
        Assert.IsFalse(score.IsUpToDate(WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 4" })));
    }

    [Test]
    public void FormatThreat_NeverMeasured_IsADash()
    {
        Assert.AreEqual("-", new WaveScore { threat = 300f }.FormatThreat(WaveScore.ComputeFingerprint(new[] { "wave" })));
    }

    [Test]
    public void FormatThreat_UpToDate_IsTheRoundedThreat()
    {
        string fingerprint = WaveScore.ComputeFingerprint(new[] { "wave" });
        WaveScore score = new WaveScore { threat = 512.6f, fingerprint = fingerprint };

        Assert.AreEqual("513", score.FormatThreat(fingerprint));
    }

    [Test]
    public void FormatThreat_TimedOut_IsOnlyALowerBound()
    {
        string fingerprint = WaveScore.ComputeFingerprint(new[] { "wave" });
        WaveScore score = new WaveScore { threat = 400f, timedOut = true, fingerprint = fingerprint };

        Assert.AreEqual("≥400", score.FormatThreat(fingerprint));
    }

    [Test]
    public void FormatThreat_DataChangedSinceTheMeasure_IsMarkedOutOfDate()
    {
        WaveScore score = new WaveScore { threat = 400f, fingerprint = WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 3" }) };

        Assert.AreEqual("400*", score.FormatThreat(WaveScore.ComputeFingerprint(new[] { "wave", "enemy damage: 4" })));
    }
}

}

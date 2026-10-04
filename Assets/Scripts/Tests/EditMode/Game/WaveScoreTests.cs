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
}

}

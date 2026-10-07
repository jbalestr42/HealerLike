using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    public class StageRestObservationTests
    {
        [Test]
        public void IsInjured_ValueBelowMax_True()
        {
            Assert.That(StageRestObservation.IsInjured(50f, 100f), Is.True);
        }

        [Test]
        public void IsInjured_ValueAtMax_False()
        {
            Assert.That(StageRestObservation.IsInjured(100f, 100f), Is.False);
        }

        [Test]
        public void IsInjured_ValueWithinRoundingOfMax_False()
        {
            Assert.That(StageRestObservation.IsInjured(99.999f, 100f), Is.False);
        }

        [Test]
        public void IsInjured_ZeroMax_False()
        {
            Assert.That(StageRestObservation.IsInjured(0f, 0f), Is.False);
        }
    }
}

using System;
using System.Collections;
using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    public class GrassJitterRunTests
    {
        class FixtureRun : GrassJitterRun
        {
            public Exception failure;
            public bool startsGame { get { return shouldStartGame; } }

            public IEnumerator Measurements()
            {
                return Run();
            }

            protected override void OnFailed(Exception error)
            {
                failure = error;
            }
        }

        [Test]
        public void Run_SkippedGameInitialization_FailsBeforeMeasurement()
        {
            FixtureRun run = new FixtureRun();
            Assert.IsTrue(run.startsGame, "The real start/map route must prepare Character.Init and a combat room.");
            try
            {
                run.Begin(run.Measurements());
                run.Step();
                Assert.IsTrue(run.hasFailed);
                Assert.IsInstanceOf<InvalidOperationException>(run.failure);
                StringAssert.Contains("initialized character", run.failure.Message);
            }
            finally
            {
                run.Stop();
            }
        }
    }
}

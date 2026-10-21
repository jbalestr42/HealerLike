using System;
using System.Collections;
using System.IO;
using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    public class StageRunFailureTests
    {
        [Test]
        public void NestedFailure_DisposesParentsAndNeverResumesPassingPath()
        {
            FailedRun run = new FailedRun();
            run.Begin(run.Parent());
            run.Step();
            Assert.That(run.hasFailed, Is.False);
            run.Step();
            run.Step();
            Assert.That(run.hasFailed, Is.True);
            Assert.That(run.disposed, Is.True);
            Assert.That(run.resumed, Is.False);
            Assert.That(run.failures, Is.EqualTo(1));
            Assert.That(run.failure.Message, Is.EqualTo("child failure"));
        }

        [Test]
        public void Manifest_FailureRemainsFailedEvenIfLaterWrittenAsPassed()
        {
            string folder = Path.Combine(Path.GetTempPath(), "render-interface-failure-" + Guid.NewGuid().ToString("N"));
            try
            {
                StageInterfaceOutput output = new StageInterfaceOutput(folder);
                Assert.Throws<InvalidOperationException>(() => output.Check(false, "failed check"));
                output.Write(true);
                Assert.That(output.manifest.failures, Does.Contain("failed check"));
                Assert.That(output.manifest.isPassed, Is.False);
                Assert.That(File.ReadAllText(Path.Combine(folder, "interface.json")), Does.Contain("\"isPassed\": false"));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        sealed class FailedRun : AStageRun
        {
            public bool disposed;
            public bool resumed;
            public int failures;
            public Exception failure;

            public IEnumerator Parent()
            {
                try
                {
                    yield return Child();
                    resumed = true;
                }
                finally
                {
                    disposed = true;
                }
            }

            IEnumerator Child()
            {
                yield return null;
                throw new InvalidOperationException("child failure");
            }

            protected override IEnumerator Run()
            {
                return Parent();
            }

            protected override void OnFailed(Exception error)
            {
                failures++;
                failure = error;
            }
        }
    }
}

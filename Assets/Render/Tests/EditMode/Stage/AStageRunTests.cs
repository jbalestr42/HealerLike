using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace HealerLike.Render.Stage
{
    public class AStageRunTests
    {
        class FixtureRun : AStageRun
        {
            public Exception error;

            protected override IEnumerator Run()
            {
                yield break;
            }

            protected override void OnFailed(Exception failure)
            {
                error = failure;
            }
        }

        FixtureRun _run;
        readonly List<string> _released = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _run = new FixtureRun();
            _released.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _run.Stop();
        }

        [Test]
        public void Begin_ReplacesSuspendedNestedRun_DisposesInnerThenOuter()
        {
            _run.Begin(Outer(false));
            _run.Step();

            _run.Begin(Empty());
            _run.Step();

            CollectionAssert.AreEqual(new[] { "inner", "outer" }, _released);
        }

        [Test]
        public void Stop_RepeatedAfterNestedSuspend_DisposesEveryScopeOnce()
        {
            _run.Begin(Outer(false));
            _run.Step();

            _run.Stop();
            _run.Stop();
            _run.Step();

            CollectionAssert.AreEqual(new[] { "inner", "outer" }, _released);
        }

        [Test]
        public void Step_ChildThrows_UnwindsParentAndRetainsFailure()
        {
            _run.Begin(Outer(true));

            _run.Step();

            Assert.IsTrue(_run.hasFailed);
            Assert.IsInstanceOf<InvalidOperationException>(_run.error);
            CollectionAssert.AreEqual(new[] { "inner", "outer" }, _released);
        }

        [Test]
        public void Stop_RequestedInsideMoveNext_DoesNotResumeTheParent()
        {
            _run.Begin(StopInside());

            _run.Step();
            _run.Step();

            CollectionAssert.AreEqual(new[] { "stopped" }, _released);
        }

        IEnumerator Outer(bool fail)
        {
            try
            {
                yield return Inner(fail);
                _released.Add("resumed");
            }
            finally
            {
                _released.Add("outer");
            }
        }

        IEnumerator Inner(bool fail)
        {
            try
            {
                if (fail)
                {
                    throw new InvalidOperationException("Child failure");
                }

                yield return null;
            }
            finally
            {
                _released.Add("inner");
            }
        }

        IEnumerator StopInside()
        {
            try
            {
                _run.Stop();
                yield return null;
                _released.Add("resumed");
            }
            finally
            {
                _released.Add("stopped");
            }
        }

        static IEnumerator Empty()
        {
            yield break;
        }
    }
}

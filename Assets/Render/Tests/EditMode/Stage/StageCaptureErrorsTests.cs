using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Stage
{
    public class StageCaptureErrorsTests
    {
        [Test]
        public void CaptureRecordsExpectedEngineErrorAndUnsubscribesOnDisposal()
        {
            StageCaptureErrors errors = new StageCaptureErrors();
            try
            {
                LogAssert.Expect(LogType.Error, "Native capture error fixture");
                Debug.LogError("Native capture error fixture");
                Assert.That(errors.count, Is.EqualTo(1));
                Assert.That(errors.messages[0], Does.Contain("Native capture error fixture"));
                errors.Dispose();
                LogAssert.Expect(LogType.Error, "Outside capture fixture");
                Debug.LogError("Outside capture fixture");
                Assert.That(errors.count, Is.EqualTo(1));
            }
            finally { errors.Dispose(); }
        }
    }
}

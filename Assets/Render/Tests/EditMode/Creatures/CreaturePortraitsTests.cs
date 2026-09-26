using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Creatures
{
    public class CreaturePortraitsTests
    {
        class FakeCapture : ICreaturePortraitCapture
        {
            public int calls;
            public int releases;
            public bool fail;
            public Texture2D Capture(EntityData data, Entity.EntityType side)
            {
                calls++;
                if (fail)
                {
                    throw new InvalidOperationException("no target");
                }

                return new Texture2D(2, 2);
            }

            public void Dispose()
            {
                releases++;
            }
        }

        FakeCapture _capture;
        CreaturePortraits _portraits;
        EntityData _data;
        [SetUp]
        public void SetUp()
        {
            _capture = new FakeCapture();
            _portraits = new CreaturePortraits(_capture);
            _data = ScriptableObject.CreateInstance<EntityData>();
            _data.name = "portrait test";
        }

        [TearDown]
        public void TearDown()
        {
            _portraits.Dispose();
            UnityEngine.Object.DestroyImmediate(_data);
        }

        [Test]
        public void GetCreatureIcon_ReusesTheImageAcrossCardsAndKeepsSidesSeparate()
        {
            Texture2D plant = _portraits.GetCreatureIcon(_data, Entity.EntityType.Player);
            Assert.AreSame(plant, _portraits.GetCreatureIcon(_data, Entity.EntityType.Player));
            Texture2D stone = _portraits.GetCreatureIcon(_data, Entity.EntityType.Computer);
            Assert.AreNotSame(plant, stone);
            Assert.AreEqual(2, _capture.calls);
        }

        [Test]
        public void Invalidate_ReleasesTheOldImageBeforeVisibleUiRequestsItsReplacement()
        {
            Texture2D before = _portraits.GetCreatureIcon(_data, Entity.EntityType.Player);
            Texture2D after = null;
            _portraits.Changed += () =>
            {
                if (_portraits.isDisposed)
                {
                    return;
                }

                Assert.IsFalse(before);
                after = _portraits.GetCreatureIcon(_data, Entity.EntityType.Player);
            };
            _portraits.Invalidate();
            Assert.IsNotNull(after);
            Assert.AreEqual(2, _capture.calls);
            Assert.AreEqual(1, _portraits.cachedCount);
        }

        [Test]
        public void Dispose_ReleasesImagesAndCaptureOnce_AndDoesNotAllowRecreation()
        {
            Texture2D image = _portraits.GetCreatureIcon(_data, Entity.EntityType.Player);
            _portraits.Dispose();
            _portraits.Dispose();
            _portraits.Invalidate();
            Assert.IsFalse(image);
            Assert.IsNull(_portraits.GetCreatureIcon(_data, Entity.EntityType.Player));
            Assert.AreEqual(1, _capture.releases);
            Assert.AreEqual(1, _capture.calls);
            Assert.AreEqual(0, _portraits.cachedCount);
        }

        [Test]
        public void Dispose_ThrowingListener_StillReleasesImagesAndCannotRunAgain()
        {
            Texture2D image = _portraits.GetCreatureIcon(_data, Entity.EntityType.Player);
            int calls = 0;
            _portraits.Changed += () =>
            {
                calls++;
                throw new InvalidOperationException("subscriber failed");
            };
            Assert.Throws<InvalidOperationException>(() => _portraits.Dispose());
            Assert.DoesNotThrow(() => _portraits.Dispose());
            Assert.IsFalse(image);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, _capture.releases);
            Assert.AreEqual(0, _portraits.cachedCount);
        }

        [Test]
        public void Capacity_DoesNotEvictAnImageAlreadyBorrowedByVisibleUi()
        {
            _portraits.Dispose();
            _portraits = new CreaturePortraits(_capture, 1);
            Texture2D first = _portraits.GetCreatureIcon(_data, Entity.EntityType.Player);
            Assert.IsNull(_portraits.GetCreatureIcon(_data, Entity.EntityType.Computer));
            Assert.IsTrue(first);
            Assert.AreSame(first, _portraits.GetCreatureIcon(_data, Entity.EntityType.Player));
            Assert.AreEqual(1, _portraits.cachedCount);
        }

        [Test]
        public void FailedCapture_IsNotRetriedByEveryRefresh_AndCanRetryAfterInvalidation()
        {
            _capture.fail = true;
            LogAssert.Expect(LogType.Warning, "[CreaturePortraits] Could not capture portrait test: no target");
            Assert.IsNull(_portraits.GetCreatureIcon(_data, Entity.EntityType.Player));
            Assert.IsNull(_portraits.GetCreatureIcon(_data, Entity.EntityType.Player));
            Assert.AreEqual(1, _capture.calls);
            _capture.fail = false;
            _portraits.Invalidate();
            Assert.IsNotNull(_portraits.GetCreatureIcon(_data, Entity.EntityType.Player));
            Assert.AreEqual(2, _capture.calls);
        }
    }
}

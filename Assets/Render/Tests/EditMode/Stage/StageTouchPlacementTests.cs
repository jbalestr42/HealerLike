using NUnit.Framework;
using UnityEngine;
using static HealerLike.Render.Stage.StageTouchFixture;

namespace HealerLike.Render.Stage
{
    public class StageTouchPlacementTests
    {
        StageTouchFixture _fixture;
        [SetUp]
        public void SetUp()
        {
            _fixture = new StageTouchFixture();
            _fixture.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void PlacementHeldTouch_PreviewsBeforeReleaseAndActivatesExactlyOnce()
        {
            _fixture.WithPlacement((input, placement) =>
            {
                UpdateTouch(input, TouchPhase.Began, Vector2.right);
                UpdateTouch(input, TouchPhase.Stationary, Vector2.right);
                UpdateTouch(input, TouchPhase.Moved, Vector2.right * 2f);
                Assert.That(placement.previews, Is.EqualTo(3));
                Assert.That(placement.clicks, Is.Zero);
                UpdateTouch(input, TouchPhase.Ended, Vector2.right * 2f);
                Assert.That(placement.previews, Is.EqualTo(4));
                Assert.That(placement.clicks, Is.EqualTo(1));
            });
        }

        [Test]
        public void PlacementUiOwnedTouch_CannotMovePreviewOrActivateAndCancelNeverClicks()
        {
            _fixture.WithPlacement((input, placement) =>
            {
                UpdateTouch(input, TouchPhase.Began, Vector2.left);
                UpdateTouch(input, TouchPhase.Moved, Vector2.right);
                UpdateTouch(input, TouchPhase.Ended, Vector2.right);
                Assert.That(placement.previews, Is.Zero);
                Assert.That(placement.clicks, Is.Zero);
                input.captureTouches = System.Array.Empty<Touch>();
                UpdateInput(input);
                UpdateTouch(input, TouchPhase.Began, Vector2.right);
                UpdateTouch(input, TouchPhase.Canceled, Vector2.right);
                UpdateTouch(input, TouchPhase.Ended, Vector2.right);
                Assert.That(placement.previews, Is.EqualTo(1));
                Assert.That(placement.clicks, Is.Zero);
            });
        }
    }
}

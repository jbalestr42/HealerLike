using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitWorldPressTests
    {
        ToolkitTouchFixture _fixture;
        ToolkitTouchFixture.FixtureDraggable _draggable;
        float _now;
        ToolkitWorldPress _press;
        [SetUp]
        public void Setup()
        {
            _fixture = new ToolkitTouchFixture(); _fixture.Init();
            TestHelpers.WithLoggingDisabled(() => _fixture.target.AddComponent<Entity>());
            _draggable = _fixture.target.AddComponent<ToolkitTouchFixture.FixtureDraggable>();
            _now = 0; _press = new ToolkitWorldPress(() => _now);
        }
        [TearDown]
        public void Teardown() { _press.Cancel(); _fixture.Dispose(); }
        [Test]
        public void WorldCreatureHoldConsumesPressAndCannotBecomeMoveOrCastOnRelease()
        {
            _press.Begin(1, Vector2.zero, _fixture.hit, true);
            Assert.That(_draggable.calls, Is.Empty);
            _now = 0.41f;
            _press.Move(1, Vector2.zero, _fixture.hit, null, Vector2.zero);
            Assert.That(_press.consumed, Is.True);
            _press.Move(1, Vector2.down * 100, _fixture.hit, null, Vector2.zero);
            _press.End(_fixture.hit);
            Assert.That(_draggable.calls, Is.Empty);
            Assert.That(_press.consumed, Is.True);
        }
        [Test]
        public void WorldMovementWaitsForSlopThenUsesExistingDragPath()
        {
            _press.Begin(1, Vector2.zero, _fixture.hit, true);
            _press.Move(1, Vector2.one, _fixture.hit, null, Vector2.zero);
            Assert.That(_draggable.calls, Is.Empty);
            _press.Move(1, Vector2.right * 40, _fixture.hit, null, Vector2.zero);
            _press.End(_fixture.hit);
            CollectionAssert.AreEqual(new[] { "start", "drag", "end" }, _draggable.calls);
        }
        [Test]
        public void CancellationDisposesDragWithoutCommitting()
        {
            _press.Begin(1, Vector2.zero, _fixture.hit, true);
            _press.Move(1, Vector2.right * 40, _fixture.hit, null, Vector2.zero);
            _press.Cancel(); _press.End(_fixture.hit);
            CollectionAssert.AreEqual(new[] { "start", "drag", "cancel" }, _draggable.calls);
        }
    }
}

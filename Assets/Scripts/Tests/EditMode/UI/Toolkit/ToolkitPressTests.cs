using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitPressTests
    {
        [Test]
        public void ShortReleaseIsTapButHeldReleaseNeverIs()
        {
            var press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, false);
            Assert.That(press.End(1, Vector2.zero, .1f), Is.EqualTo(ToolkitPress.Owner.Tap));
            press.Begin(1, Vector2.zero, 1, false);
            Assert.That(press.End(1, Vector2.zero, 1.5f), Is.EqualTo(ToolkitPress.Owner.Hold));
        }

        [Test]
        public void HorizontalScrollCannotBecomeDeployment()
        {
            var press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, true);
            press.Move(1, new Vector2(20, -2), .1f);
            Assert.That(press.End(1, new Vector2(20, -100), .2f), Is.EqualTo(ToolkitPress.Owner.Scroll));
        }

        [Test]
        public void FirstResolvedHoldOrDragOwnsTheWholePress()
        {
            var press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, true);
            press.Move(1, Vector2.zero, .41f);
            Assert.That(press.End(1, Vector2.down * 100, .6f), Is.EqualTo(ToolkitPress.Owner.Hold));
            press.Begin(1, Vector2.zero, 1, true);
            press.Move(1, Vector2.down * 20, 1.1f);
            Assert.That(press.Move(1, Vector2.zero, 2), Is.EqualTo(ToolkitPress.Owner.Drag));
        }

        [Test]
        public void MovementCancelsHoldAndOnlyUndeployedRosterCanDrag()
        {
            var press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, false);
            Assert.That(press.Move(1, Vector2.down * 20, .1f), Is.EqualTo(ToolkitPress.Owner.Cancelled));
            Assert.That(press.End(1, Vector2.zero, 1), Is.EqualTo(ToolkitPress.Owner.Cancelled));
        }

        [Test]
        public void PointerLossAndSecondPointerCannotActivate()
        {
            var press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, true);
            Assert.That(press.Begin(2, Vector2.zero, 0, true), Is.False);
            Assert.That(press.End(2, Vector2.zero, .1f), Is.EqualTo(ToolkitPress.Owner.None));
            press.Cancel();
            Assert.That(press.End(1, Vector2.zero, .1f), Is.EqualTo(ToolkitPress.Owner.None));
        }
    }
}

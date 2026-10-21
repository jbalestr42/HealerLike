using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitPressTests
    {
        [Test]
        public void ShortReleaseIsTapButHeldReleaseNeverIs()
        {
            ToolkitPress press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, false);
            Assert.That(press.End(1, Vector2.zero, 0.1f), Is.EqualTo(ToolkitPress.Owner.Tap));
            press.Begin(1, Vector2.zero, 1, false);
            Assert.That(press.End(1, Vector2.zero, 1.5f), Is.EqualTo(ToolkitPress.Owner.Hold));
        }

        [Test]
        public void HorizontalScrollCannotBecomeDeployment()
        {
            ToolkitPress press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, true);
            press.Move(1, new Vector2(20, -2), 0.1f);
            Assert.That(press.End(1, new Vector2(20, -100), 0.2f), Is.EqualTo(ToolkitPress.Owner.Scroll));
        }

        [Test]
        public void FirstResolvedHoldOrDragOwnsTheWholePress()
        {
            ToolkitPress press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, true);
            press.Move(1, Vector2.zero, 0.41f);
            Assert.That(press.End(1, Vector2.down * 100, 0.6f), Is.EqualTo(ToolkitPress.Owner.Hold));
            press.Begin(1, Vector2.zero, 1, true);
            press.Move(1, Vector2.down * 20, 1.1f);
            Assert.That(press.Move(1, Vector2.zero, 2), Is.EqualTo(ToolkitPress.Owner.Drag));
        }

        [Test]
        public void MovementCancelsHoldAndOnlyUndeployedRosterCanDrag()
        {
            ToolkitPress press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, false);
            Assert.That(press.Move(1, Vector2.down * 20, 0.1f), Is.EqualTo(ToolkitPress.Owner.Cancelled));
            Assert.That(press.End(1, Vector2.zero, 1), Is.EqualTo(ToolkitPress.Owner.Cancelled));
        }

        [Test]
        public void PointerLossAndSecondPointerCannotActivate()
        {
            ToolkitPress press = new ToolkitPress();
            press.Begin(1, Vector2.zero, 0, true);
            Assert.That(press.Begin(2, Vector2.zero, 0, true), Is.False);
            Assert.That(press.End(2, Vector2.zero, 0.1f), Is.EqualTo(ToolkitPress.Owner.None));
            press.Cancel();
            Assert.That(press.End(1, Vector2.zero, 0.1f), Is.EqualTo(ToolkitPress.Owner.None));
        }
    }
}

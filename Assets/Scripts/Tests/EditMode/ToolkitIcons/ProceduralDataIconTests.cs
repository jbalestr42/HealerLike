using System;
using NUnit.Framework;
namespace HealerLike.UI.Toolkit.Icons
{
    public class ProceduralDataIconTests
    {
        [Test] public void RenderIsDeterministic()
        {
            var descriptor = new DataIconDescriptor("spell:heal", "Heal", DataIconKind.Spell);
            CollectionAssert.AreEqual(ProceduralDataIcon.Render(descriptor, 32), ProceduralDataIcon.Render(descriptor, 32));
        }
        [Test] public void DistinctKeysProduceDistinctArtwork()
        {
            CollectionAssert.AreNotEqual(
                ProceduralDataIcon.Render(new DataIconDescriptor("heal", "Heal", DataIconKind.Spell), 32),
                ProceduralDataIcon.Render(new DataIconDescriptor("fire", "Fire", DataIconKind.Spell), 32));
        }
        [TestCase(DataIconKind.Spell)] [TestCase(DataIconKind.Creature)]
        [TestCase(DataIconKind.Character)] [TestCase(DataIconKind.Data)]
        public void EveryKindHasTransparentCornersAndOpaqueCenter(DataIconKind kind)
        {
            var pixels = ProceduralDataIcon.Render(new DataIconDescriptor("key", "Label", kind), 32);
            Assert.That(pixels.Length, Is.EqualTo(1024));
            Assert.That(pixels[0].a, Is.EqualTo(0));
            Assert.That(pixels[16 * 32 + 16].a, Is.EqualTo(255));
        }
        [TestCase(0)] [TestCase(2048)]
        public void InvalidSizeRejected(int size)
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("out of range"));
            Assert.That(ProceduralDataIcon.Render(new DataIconDescriptor("key", "Label", DataIconKind.Data), size), Is.Null);
        }
    }
}

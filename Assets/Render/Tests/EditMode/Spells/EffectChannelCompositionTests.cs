using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class EffectChannelCompositionTests
    {
        EffectVocabulary _vocabulary;
        readonly List<Object> _created = new List<Object>();
        static EffectChannels Channels => new EffectChannels { operation = EffectOperation.Boon,
            family = EffectFamily.Boon, tempo = EffectTempo.ForDuration, reach = EffectReach.Group,
            delivery = EffectDelivery.Link, trigger = EffectTrigger.OnHit, side = EffectSide.Opposing,
            origin = EffectOrigin.Item };

        [SetUp]
        public void SetUp()
        {
            _vocabulary = ScriptableObject.CreateInstance<EffectVocabulary>();
            _created.Add(_vocabulary);
            _vocabulary.palette = RenderTestAssets.LoadPalette();
            _vocabulary.elements[EffectElement.Orbit] = Entry();
            _vocabulary.table[new EffectCell(EffectOperation.Boon, EffectAspect.Offence)] =
                new EffectCellEntry(EffectElement.Orbit, EffectElement.Orbit, false);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        static ElementEntry Entry(int count = 1)
        {
            var parts = new LookPart[count];
            for (int i = 0; i < count; i++)
                parts[i] = new LookPart { id = "part" + i, primitive = Primitive.Sphere,
                    role = PartRole.Body, size = Vector3.one * .1f };
            return new ElementEntry { parts = parts, socket = EffectSocket.Feet,
                motion = EffectMotionKind.Grow, cycleSeconds = .9f };
        }

        void Select(int channel, ElementEntry entry)
        {
            switch (channel)
            {
                case 0: _vocabulary.reach[Channels.reach] = entry; break;
                case 1: _vocabulary.delivery[Channels.delivery] = entry; break;
                case 2: _vocabulary.trigger[Channels.trigger] = entry; break;
                case 3: _vocabulary.side[Channels.side] = entry; break;
                case 4: _vocabulary.origin[Channels.origin] = entry; break;
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void EachChannel_AppendsItsOwnAuthoredSocketAndMotion(int channel)
        {
            ElementEntry addition = Entry();
            addition.socket = EffectSocket.AboveHead;
            addition.motion = EffectMotionKind.Press;
            Select(channel, addition);
            EffectRecipe result = EffectComposer.Compose(_vocabulary, Channels, 1, 0);
            Assert.IsNotNull(result);
            Assert.AreSame(_vocabulary.elements[EffectElement.Orbit], result.entry);
            Assert.AreEqual(1, result.additions.Length);
            Assert.AreSame(addition, result.additions[0].entry);
            Assert.AreEqual(EffectSocket.AboveHead, result.additions[0].socket);
            Assert.AreEqual(EffectMotionKind.Press, result.additions[0].motion);
            Assert.IsEmpty(result.additions[0].additions, "An additive piece must not recursively compose channels.");
        }

        [Test]
        public void AllChannels_StackInDeclarationOrderEvenOnTheSameSocket()
        {
            var entries = new ElementEntry[5];
            for (int i = 0; i < entries.Length; i++) Select(i, entries[i] = Entry());
            EffectRecipe result = EffectComposer.Compose(_vocabulary, Channels, 1, 0);
            Assert.AreEqual(5, result.additions.Length);
            for (int i = 0; i < entries.Length; i++) Assert.AreSame(entries[i], result.additions[i].entry);
            Assert.IsTrue(EffectValidator.TryValidate(result, out _));
        }

        [Test]
        public void EmptySlots_KeepTheExistingCoreAppearance()
        {
            EffectRecipe legacy = EffectComposer.Compose(_vocabulary, EffectElement.Orbit, EffectFamily.Boon,
                EffectTempo.ForDuration, 0f, 2, 0f, 0f);
            EffectRecipe result = EffectComposer.Compose(_vocabulary, Channels, 2, 0);
            Assert.IsEmpty(result.additions);
            Assert.AreSame(legacy.entry, result.entry);
            Assert.AreEqual(legacy.scale, result.scale);
            Assert.AreEqual(legacy.colour, result.colour);
            Assert.AreEqual(legacy.count, result.count);
            Assert.AreEqual(legacy.cycleSeconds, result.cycleSeconds);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidSelectedEntryOrAggregateBudget_IsRejectedAtomically(bool overBudget)
        {
            Select(0, overBudget ? Entry(EffectValidator.MaxParts) : null);
            Assert.IsFalse(EffectCompositionValidator.TryValidate(Channels, _vocabulary, out _));
            TestHelpers.WithLoggingDisabled(() =>
                Assert.IsNull(EffectComposer.Compose(_vocabulary, Channels, 1, 0)));
        }

        [Test]
        public void MultiBuffBudget_IncludesEveryChannelsChildren()
        {
            SpellLooks looks = ScriptableObject.CreateInstance<SpellLooks>();
            _created.Add(looks);
            BuffHandlerFactory handler = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
            handler.data.buffFactoryList.Add(handler.data.buffFactoryList[0]);
            _vocabulary.reach[EffectReach.Single] = Entry(130);
            Assert.IsEmpty(looks.Compose(_vocabulary, handler, null, null));
        }

        [Test]
        public void PoolIdentity_DistinguishesSelectedAdditionsAndDescendantClocks()
        {
            ElementEntry first = Entry();
            Select(0, first);
            EffectRecipe a = EffectComposer.Compose(_vocabulary, Channels, 1, 0);
            EffectRecipe equivalent = EffectComposer.Compose(_vocabulary, Channels, 1, 0);
            Select(0, Entry());
            EffectRecipe different = EffectComposer.Compose(_vocabulary, Channels, 1, 0);
            Assert.IsTrue(new StatusKey(null, a).Equals(new StatusKey(null, equivalent)));
            Assert.IsFalse(new StatusKey(null, a).Equals(new StatusKey(null, different)));
            BuffHandlerFactory ownerA = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
            BuffHandlerFactory ownerB = SpellSinkFixture.Modifier(AttributeType.Damage, 2f, _created);
            a.additions[0].tempo = EffectTempo.PerPeriod;
            Assert.IsFalse(new StatusKey(null, a, 0, ownerA).Equals(new StatusKey(null, a, 0, ownerB)));
        }
    }
}

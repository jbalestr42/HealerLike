using System;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class HLZonePackerTests
    {
        static HLZone Raw(Vector3 position, float radius, HLZoneKind kind, float strength, float age = 1f,
            uint reserved = 0u)
        {
            return new HLZone
            {
                position = position,
                radius = radius,
                kind = (int)kind,
                strength = strength,
                age = age,
                reserved = reserved
            };
        }

        [TestCase(HLZoneKind.Range, 3)] [TestCase(HLZoneKind.Bruise, 4)] [TestCase(HLZoneKind.Launch, 5)] [TestCase(HLZoneKind.Trample, 6)]
        public void V3KindsPreserveAbi(HLZoneKind kind, int value)
        {
            Assert.AreEqual(value, (int)kind);
            Assert.IsTrue(HLZonePacker.TryCreate(Vector3.zero, 1, kind, 1, 0, out _));
        }
        [Test] public void LaunchHeadingSurvivesPackingAndOtherKindsClearIt()
        {
            Assert.AreEqual(0u, HLZonePacker.EncodeDirection(Vector3.right));
            Assert.AreEqual(1073741824u, HLZonePacker.EncodeDirection(Vector3.forward));
            Assert.AreEqual(2147483648u, HLZonePacker.EncodeDirection(Vector3.left));
            Assert.AreEqual(3221225472u, HLZonePacker.EncodeDirection(Vector3.back));
            var source = new[] { Raw(Vector3.zero, 2, HLZoneKind.Launch, 1, reserved: 1073741824u), Raw(Vector3.zero, 2, HLZoneKind.Range, 1, reserved: 123u) };
            var dest = new HLZone[2]; HLZonePacker.Pack(source, dest, out _, out _);
            Assert.AreEqual(1073741824u, dest[0].reserved); Assert.AreEqual(0u, dest[1].reserved);
        }
        [Test]
        public void StrideIsThirtyTwoBytes()
        {
            Assert.AreEqual(32, HLZone.Stride);
            Assert.AreEqual(32, System.Runtime.InteropServices.Marshal.SizeOf<HLZone>());
        }

        [Test]
        public void FieldOffsetsMatchTheWireLayout()
        {
            HLZone zone = Raw(new Vector3(1f, 2f, 3f), 4f, HLZoneKind.Hostile, 0.5f, 6f);
            byte[] bytes = new byte[HLZone.Stride];
            IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(HLZone.Stride);
            try
            {
                System.Runtime.InteropServices.Marshal.StructureToPtr(zone, buffer, false);
                System.Runtime.InteropServices.Marshal.Copy(buffer, bytes, 0, HLZone.Stride);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }

            Assert.AreEqual(1f, BitConverter.ToSingle(bytes, 0), 0f, "position.x at byte 0");
            Assert.AreEqual(2f, BitConverter.ToSingle(bytes, 4), 0f, "position.y at byte 4");
            Assert.AreEqual(3f, BitConverter.ToSingle(bytes, 8), 0f, "position.z at byte 8");
            Assert.AreEqual(4f, BitConverter.ToSingle(bytes, 12), 0f, "radius at byte 12");
            Assert.AreEqual(2, BitConverter.ToInt32(bytes, 16), "kind at byte 16");
            Assert.AreEqual(0.5f, BitConverter.ToSingle(bytes, 20), 0f, "strength at byte 20");
            Assert.AreEqual(6f, BitConverter.ToSingle(bytes, 24), 0f, "age at byte 24");
            Assert.AreEqual(0u, BitConverter.ToUInt32(bytes, 28), "reserved at byte 28");
        }

        [Test]
        public void TryCreateCanonicalizesAValidZone()
        {
            bool created = HLZonePacker.TryCreate(new Vector3(3f, 1f, -2f), 2.5f, HLZoneKind.Heal,
                0.25f, 4f, out HLZone zone);

            Assert.IsTrue(created);
            Assert.AreEqual(new Vector3(3f, 1f, -2f), zone.position);
            Assert.AreEqual(2.5f, zone.radius);
            Assert.AreEqual((int)HLZoneKind.Heal, zone.kind);
            Assert.AreEqual(0.25f, zone.strength);
            Assert.AreEqual(4f, zone.age);
            Assert.AreEqual(0u, zone.reserved);
        }

        [Test]
        public void TryCreateClampsStrengthAndAgeAndClearsReserved()
        {
            Assert.IsTrue(HLZonePacker.TryCreate(Vector3.zero, 1f, HLZoneKind.Heal, 7f, -3f, out HLZone high));
            Assert.AreEqual(1f, high.strength, "strength clamps to 1");
            Assert.AreEqual(0f, high.age, "negative age clamps to 0");
            Assert.AreEqual(0u, high.reserved);

            Assert.IsTrue(HLZonePacker.TryCreate(Vector3.zero, 1f, HLZoneKind.Heal, -2f, 0f, out HLZone low));
            Assert.AreEqual(0f, low.strength, "strength clamps to 0");
        }

        [Test]
        public void TryCreateRejectsNonPositiveRadius()
        {
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, 0f, HLZoneKind.Heal, 1f, 0f, out _));
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, -1f, HLZoneKind.Heal, 1f, 0f, out _));
        }

        [Test]
        public void TryCreateRejectsNoneAndUnknownKinds()
        {
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, 1f, HLZoneKind.None, 1f, 0f, out _));
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, 1f, (HLZoneKind)99, 1f, 0f, out _));
        }

        [Test]
        public void TryCreateRejectsNonFiniteFields()
        {
            Assert.IsFalse(HLZonePacker.TryCreate(new Vector3(float.NaN, 0f, 0f), 1f, HLZoneKind.Heal, 1f, 0f, out _),
                "NaN position");
            Assert.IsFalse(HLZonePacker.TryCreate(new Vector3(0f, 0f, float.PositiveInfinity), 1f, HLZoneKind.Heal, 1f, 0f, out _),
                "infinite position");
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, float.NaN, HLZoneKind.Heal, 1f, 0f, out _), "NaN radius");
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, float.PositiveInfinity, HLZoneKind.Heal, 1f, 0f, out _),
                "infinite radius");
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, 1f, HLZoneKind.Heal, float.NaN, 0f, out _), "NaN strength");
            Assert.IsFalse(HLZonePacker.TryCreate(Vector3.zero, 1f, HLZoneKind.Heal, 1f, float.NaN, out _), "NaN age");
        }

        [Test]
        public void TryCreateOutputsDefaultOnRejection()
        {
            Assert.IsFalse(HLZonePacker.TryCreate(new Vector3(5f, 5f, 5f), -1f, HLZoneKind.Heal, 1f, 2f, out HLZone zone));
            Assert.AreEqual(default(HLZone), zone);
        }

        [Test]
        public void PackPreservesRegistrationOrder()
        {
            HLZone[] source =
            {
                Raw(new Vector3(1f, 0f, 0f), 1f, HLZoneKind.Heal, 1f),
                Raw(new Vector3(2f, 0f, 0f), 1f, HLZoneKind.Hostile, 1f),
                Raw(new Vector3(3f, 0f, 0f), 1f, HLZoneKind.Heal, 1f)
            };
            HLZone[] destination = new HLZone[8];

            int written = HLZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(3, written);
            Assert.AreEqual(0, rejected);
            Assert.AreEqual(0, overflow);
            Assert.AreEqual(1f, destination[0].position.x);
            Assert.AreEqual(2f, destination[1].position.x);
            Assert.AreEqual(3f, destination[2].position.x);
        }

        [Test]
        public void PackCanonicalizesEachItem()
        {
            HLZone[] source = { Raw(Vector3.zero, 2f, HLZoneKind.Heal, 5f, -1f, reserved: 0xDEADBEEF) };
            HLZone[] destination = new HLZone[4];

            Assert.AreEqual(1, HLZonePacker.Pack(source, destination, out _, out _));
            Assert.AreEqual(1f, destination[0].strength);
            Assert.AreEqual(0f, destination[0].age);
            Assert.AreEqual(0u, destination[0].reserved, "reserved is always cleared on the wire");
        }

        [Test]
        public void PackCountsInvalidItemsAsRejected()
        {
            HLZone[] source =
            {
                Raw(Vector3.zero, 1f, HLZoneKind.Heal, 1f),
                Raw(Vector3.zero, 0f, HLZoneKind.Heal, 1f),
                Raw(Vector3.zero, 1f, HLZoneKind.None, 1f),
                Raw(new Vector3(float.NaN, 0f, 0f), 1f, HLZoneKind.Heal, 1f)
            };
            HLZone[] destination = new HLZone[8];

            int written = HLZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(1, written);
            Assert.AreEqual(3, rejected);
            Assert.AreEqual(0, overflow);
        }

        [Test]
        public void PackOmitsZeroStrengthItemsWithoutCallingThemRejected()
        {
            HLZone[] source =
            {
                Raw(Vector3.zero, 1f, HLZoneKind.Heal, 0f),
                Raw(Vector3.zero, 1f, HLZoneKind.Heal, -0.5f),
                Raw(Vector3.zero, 1f, HLZoneKind.Heal, 0.1f)
            };
            HLZone[] destination = new HLZone[8];

            int written = HLZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(1, written);
            Assert.AreEqual(0, rejected, "an inactive zone is not an error");
            Assert.AreEqual(0, overflow);
            Assert.AreEqual(0.1f, destination[0].strength);
        }

        [Test]
        public void PackStopsAtSixtyFourAndReportsOverflowSeparately()
        {
            HLZone[] source = new HLZone[70];
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = Raw(new Vector3(i, 0f, 0f), 1f, HLZoneKind.Heal, 1f);
            }
            HLZone[] destination = new HLZone[HLZonePacker.MaxZones];

            int written = HLZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(64, written);
            Assert.AreEqual(0, rejected);
            Assert.AreEqual(6, overflow);
            Assert.AreEqual(0f, destination[0].position.x, "the first valid entries win");
            Assert.AreEqual(63f, destination[63].position.x);
        }

        [Test]
        public void PackNeverWritesPastASmallDestination()
        {
            HLZone[] source = new HLZone[10];
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = Raw(new Vector3(i, 0f, 0f), 1f, HLZoneKind.Hostile, 1f);
            }
            HLZone[] destination = new HLZone[4];

            int written = HLZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(4, written);
            Assert.AreEqual(0, rejected);
            Assert.AreEqual(6, overflow);
        }

        [Test]
        public void PackZeroesTheUnusedTail()
        {
            HLZone[] destination = new HLZone[4];
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = Raw(new Vector3(9f, 9f, 9f), 9f, HLZoneKind.Hostile, 1f, 9f);
            }
            HLZone[] source = { Raw(Vector3.one, 1f, HLZoneKind.Heal, 1f) };

            int written = HLZonePacker.Pack(source, destination, out _, out _);

            Assert.AreEqual(1, written);
            for (int i = written; i < destination.Length; i++)
            {
                Assert.AreEqual(default(HLZone), destination[i], "stale slot " + i + " was not cleared");
            }
        }

        [Test]
        public void PackOfAnEmptySourcePublishesCountZero()
        {
            HLZone[] destination = new HLZone[2];
            destination[0] = Raw(Vector3.one, 1f, HLZoneKind.Heal, 1f);

            int written = HLZonePacker.Pack(ReadOnlySpan<HLZone>.Empty, destination, out int rejected,
                out int overflow);

            Assert.AreEqual(0, written);
            Assert.AreEqual(0, rejected);
            Assert.AreEqual(0, overflow);
            Assert.AreEqual(default(HLZone), destination[0]);
        }

        [Test]
        public void PackIsDeterministicForTheSameInput()
        {
            HLZone[] source =
            {
                Raw(new Vector3(1f, 0f, 1f), 2f, HLZoneKind.Hostile, 0.7f, 3f),
                Raw(new Vector3(0f, 0f, 0f), 0f, HLZoneKind.Heal, 1f),
                Raw(new Vector3(4f, 0f, 2f), 1f, HLZoneKind.Heal, 0.3f, 1f)
            };
            HLZone[] first = new HLZone[8];
            HLZone[] second = new HLZone[8];

            int a = HLZonePacker.Pack(source, first, out int rejectedA, out int overflowA);
            int b = HLZonePacker.Pack(source, second, out int rejectedB, out int overflowB);

            Assert.AreEqual(a, b);
            Assert.AreEqual(rejectedA, rejectedB);
            Assert.AreEqual(overflowA, overflowB);
            for (int i = 0; i < first.Length; i++) Assert.AreEqual(first[i], second[i], "slot " + i);
        }
    }
}

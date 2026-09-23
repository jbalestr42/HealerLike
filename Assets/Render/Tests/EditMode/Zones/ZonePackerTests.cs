using System;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{
    public class ZonePackerTests
    {
        static Zone Raw(Vector3 position, float radius, ZoneKind kind, float strength, float age = 1f,
                          uint reserved = 0u)
        {
            return new Zone
            {
                position = position,
                radius = radius,
                kind = (int)kind,
                strength = strength,
                age = age,
                reserved = reserved
            };
        }

        [TestCase(ZoneKind.Range, 3)]
        [TestCase(ZoneKind.Bruise, 4)]
        [TestCase(ZoneKind.Launch, 5)]
        [TestCase(ZoneKind.Trample, 6)]
        public void V3KindsPreserveAbi(ZoneKind kind, int value)
        {
            Assert.AreEqual(value, (int)kind);
            Assert.IsTrue(ZonePacker.TryCreate(Vector3.zero, 1f, kind, 1f, 0f, out _));
        }

        [Test]
        public void LaunchHeadingSurvivesPackingAndOtherKindsClearIt()
        {
            Assert.AreEqual(0u, ZonePacker.EncodeDirection(Vector3.right));
            Assert.AreEqual(1073741824u, ZonePacker.EncodeDirection(Vector3.forward));
            Assert.AreEqual(2147483648u, ZonePacker.EncodeDirection(Vector3.left));
            Assert.AreEqual(3221225472u, ZonePacker.EncodeDirection(Vector3.back));
            Zone[] source =
            {
                Raw(Vector3.zero, 2f, ZoneKind.Launch, 1f, reserved: 1073741824u),
                Raw(Vector3.zero, 2f, ZoneKind.Range, 1f, reserved: 123u)
            };
            Zone[] destination = new Zone[2];

            ZonePacker.Pack(source, destination, out _, out _);

            Assert.AreEqual(1073741824u, destination[0].reserved);
            Assert.AreEqual(0u, destination[1].reserved);
        }
        [Test]
        public void StrideIsThirtyTwoBytes()
        {
            Assert.AreEqual(32, Zone.Stride);
            Assert.AreEqual(32, System.Runtime.InteropServices.Marshal.SizeOf<Zone>());
        }

        [Test]
        public void FieldOffsetsMatchTheWireLayout()
        {
            Zone zone = Raw(new Vector3(1f, 2f, 3f), 4f, ZoneKind.Hostile, 0.5f, 6f);
            byte[] bytes = new byte[Zone.Stride];
            IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(Zone.Stride);
            try
            {
                System.Runtime.InteropServices.Marshal.StructureToPtr(zone, buffer, false);
                System.Runtime.InteropServices.Marshal.Copy(buffer, bytes, 0, Zone.Stride);
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
            bool created = ZonePacker.TryCreate(new Vector3(3f, 1f, -2f), 2.5f, ZoneKind.Heal, 0.25f, 4f,
                                                  out Zone zone);

            Assert.IsTrue(created);
            Assert.AreEqual(new Vector3(3f, 1f, -2f), zone.position);
            Assert.AreEqual(2.5f, zone.radius);
            Assert.AreEqual((int)ZoneKind.Heal, zone.kind);
            Assert.AreEqual(0.25f, zone.strength);
            Assert.AreEqual(4f, zone.age);
            Assert.AreEqual(0u, zone.reserved);
        }

        [Test]
        public void TryCreateClampsStrengthAndAgeAndClearsReserved()
        {
            Assert.IsTrue(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, 7f, -3f, out Zone high));
            Assert.AreEqual(1f, high.strength, "strength clamps to 1");
            Assert.AreEqual(0f, high.age, "negative age clamps to 0");
            Assert.AreEqual(0u, high.reserved);

            Assert.IsTrue(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, -2f, 0f, out Zone low));
            Assert.AreEqual(0f, low.strength, "strength clamps to 0");
        }

        [Test]
        public void TryCreateRejectsNonPositiveRadius()
        {
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 0f, ZoneKind.Heal, 1f, 0f, out _));
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, -1f, ZoneKind.Heal, 1f, 0f, out _));
        }

        [Test]
        public void TryCreateRejectsNoneAndUnknownKinds()
        {
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.None, 1f, 0f, out _));
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, (ZoneKind)99, 1f, 0f, out _));
        }

        [Test]
        public void TryCreateRejectsNonFiniteFields()
        {
            Vector3 nanPosition = new Vector3(float.NaN, 0f, 0f);
            Vector3 infinitePosition = new Vector3(0f, 0f, float.PositiveInfinity);
            float infinity = float.PositiveInfinity;

            Assert.IsFalse(ZonePacker.TryCreate(nanPosition, 1f, ZoneKind.Heal, 1f, 0f, out _), "NaN position");
            Assert.IsFalse(ZonePacker.TryCreate(infinitePosition, 1f, ZoneKind.Heal, 1f, 0f, out _),
                           "infinite position");
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, float.NaN, ZoneKind.Heal, 1f, 0f, out _),
                           "NaN radius");
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, infinity, ZoneKind.Heal, 1f, 0f, out _),
                           "infinite radius");
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, float.NaN, 0f, out _),
                           "NaN strength");
            Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, 1f, float.NaN, out _), "NaN age");
        }

        [Test]
        public void TryCreateOutputsDefaultOnRejection()
        {
            bool created = ZonePacker.TryCreate(new Vector3(5f, 5f, 5f), -1f, ZoneKind.Heal, 1f, 2f,
                                                  out Zone zone);

            Assert.IsFalse(created);
            Assert.AreEqual(default(Zone), zone);
        }

        [Test]
        public void PackPreservesRegistrationOrder()
        {
            Zone[] source =
            {
                Raw(new Vector3(1f, 0f, 0f), 1f, ZoneKind.Heal, 1f),
                Raw(new Vector3(2f, 0f, 0f), 1f, ZoneKind.Hostile, 1f),
                Raw(new Vector3(3f, 0f, 0f), 1f, ZoneKind.Heal, 1f)
            };
            Zone[] destination = new Zone[8];

            int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

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
            Zone[] source = { Raw(Vector3.zero, 2f, ZoneKind.Heal, 5f, -1f, reserved: 0xDEADBEEF) };
            Zone[] destination = new Zone[4];

            Assert.AreEqual(1, ZonePacker.Pack(source, destination, out _, out _));
            Assert.AreEqual(1f, destination[0].strength);
            Assert.AreEqual(0f, destination[0].age);
            Assert.AreEqual(0u, destination[0].reserved, "reserved is always cleared on the wire");
        }

        [Test]
        public void PackCountsInvalidItemsAsRejected()
        {
            Zone[] source =
            {
                Raw(Vector3.zero, 1f, ZoneKind.Heal, 1f),
                Raw(Vector3.zero, 0f, ZoneKind.Heal, 1f),
                Raw(Vector3.zero, 1f, ZoneKind.None, 1f),
                Raw(new Vector3(float.NaN, 0f, 0f), 1f, ZoneKind.Heal, 1f)
            };
            Zone[] destination = new Zone[8];

            int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(1, written);
            Assert.AreEqual(3, rejected);
            Assert.AreEqual(0, overflow);
        }

        [Test]
        public void PackOmitsZeroStrengthItemsWithoutCallingThemRejected()
        {
            Zone[] source =
            {
                Raw(Vector3.zero, 1f, ZoneKind.Heal, 0f),
                Raw(Vector3.zero, 1f, ZoneKind.Heal, -0.5f),
                Raw(Vector3.zero, 1f, ZoneKind.Heal, 0.1f)
            };
            Zone[] destination = new Zone[8];

            int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(1, written);
            Assert.AreEqual(0, rejected, "an inactive zone is not an error");
            Assert.AreEqual(0, overflow);
            Assert.AreEqual(0.1f, destination[0].strength);
        }

        [Test]
        public void PackStopsAtSixtyFourAndReportsOverflowSeparately()
        {
            Zone[] source = new Zone[70];
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = Raw(new Vector3(i, 0f, 0f), 1f, ZoneKind.Heal, 1f);
            }
            Zone[] destination = new Zone[ZonePacker.MaxZones];

            int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(64, written);
            Assert.AreEqual(0, rejected);
            Assert.AreEqual(6, overflow);
            Assert.AreEqual(0f, destination[0].position.x, "the first valid entries win");
            Assert.AreEqual(63f, destination[63].position.x);
        }

        [Test]
        public void PackNeverWritesPastASmallDestination()
        {
            Zone[] source = new Zone[10];
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = Raw(new Vector3(i, 0f, 0f), 1f, ZoneKind.Hostile, 1f);
            }
            Zone[] destination = new Zone[4];

            int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

            Assert.AreEqual(4, written);
            Assert.AreEqual(0, rejected);
            Assert.AreEqual(6, overflow);
        }

        [Test]
        public void PackZeroesTheUnusedTail()
        {
            Zone[] destination = new Zone[4];
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = Raw(new Vector3(9f, 9f, 9f), 9f, ZoneKind.Hostile, 1f, 9f);
            }
            Zone[] source = { Raw(Vector3.one, 1f, ZoneKind.Heal, 1f) };

            int written = ZonePacker.Pack(source, destination, out _, out _);

            Assert.AreEqual(1, written);
            for (int i = written; i < destination.Length; i++)
            {
                Assert.AreEqual(default(Zone), destination[i], "stale slot " + i + " was not cleared");
            }
        }

        [Test]
        public void PackOfAnEmptySourcePublishesCountZero()
        {
            Zone[] destination = new Zone[2];
            destination[0] = Raw(Vector3.one, 1f, ZoneKind.Heal, 1f);

            int written = ZonePacker.Pack(ReadOnlySpan<Zone>.Empty, destination, out int rejected,
                                            out int overflow);

            Assert.AreEqual(0, written);
            Assert.AreEqual(0, rejected);
            Assert.AreEqual(0, overflow);
            Assert.AreEqual(default(Zone), destination[0]);
        }

        [Test]
        public void PackIsDeterministicForTheSameInput()
        {
            Zone[] source =
            {
                Raw(new Vector3(1f, 0f, 1f), 2f, ZoneKind.Hostile, 0.7f, 3f),
                Raw(new Vector3(0f, 0f, 0f), 0f, ZoneKind.Heal, 1f),
                Raw(new Vector3(4f, 0f, 2f), 1f, ZoneKind.Heal, 0.3f, 1f)
            };
            Zone[] first = new Zone[8];
            Zone[] second = new Zone[8];

            int a = ZonePacker.Pack(source, first, out int rejectedA, out int overflowA);
            int b = ZonePacker.Pack(source, second, out int rejectedB, out int overflowB);

            Assert.AreEqual(a, b);
            Assert.AreEqual(rejectedA, rejectedB);
            Assert.AreEqual(overflowA, overflowB);
            for (int i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i], second[i], "slot " + i);
            }
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public sealed class StageMapFixtureTests
    {
        GameObject _host;
        AscensionGameType _ascension;
        MapGenerationSettings _settings;
        static readonly FieldInfo Settings = typeof(AscensionGameType).GetField("_mapSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo Seed = typeof(AscensionGameType).GetField("_seed",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Map capture fixture test");
            _host.SetActive(false);
            _ascension = _host.AddComponent<AscensionGameType>();
            _settings = ScriptableObject.CreateInstance<MapGenerationSettings>();
            Settings.SetValue(_ascension, _settings);
            Seed.SetValue(_ascension, 42);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void ShortFixture_GeneratesSpecialRoomRouteAtSupportedEliteFloor_WithoutChangingSource()
        {
            string before = JsonUtility.ToJson(_settings);
            using (StageMapFixture fixture = new StageMapFixture(_ascension, true))
            {
                Assert.That(fixture.settings, Is.Not.SameAs(_settings));
                RunMap map = MapGenerator.Generate(fixture.settings, StageMapFixture.Seed);
                MapNodeType[] expected = { MapNodeType.Combat, MapNodeType.Treasure, MapNodeType.Combat,
                    MapNodeType.Elite, MapNodeType.Rest };
                Assert.That(map.floorCount, Is.EqualTo(expected.Length));
                for (int floor = 0; floor < map.floorCount; floor++)
                {
                    Assert.That(map.floors[floor], Is.Not.Empty);
                    foreach (MapNode node in map.floors[floor])
                        Assert.That(node.type, Is.EqualTo(expected[floor]), "Floor " + floor);
                }
                Assert.That(map.boss.type, Is.EqualTo(MapNodeType.Boss));
                Assert.That(JsonUtility.ToJson(_settings), Is.EqualTo(before));
            }
            Assert.That(Settings.GetValue(_ascension), Is.SameAs(_settings));
            Assert.That(Seed.GetValue(_ascension), Is.EqualTo(42));
            Assert.That(JsonUtility.ToJson(_settings), Is.EqualTo(before));
        }

        [Test]
        public void DefaultFixture_RetainsEveryAuthoredGenerationValue_AndRestoresOriginalOnDispose()
        {
            _settings.floorCount = 13;
            _settings.columnCount = 5;
            _settings.treasureFloor = 6;
            string before = JsonUtility.ToJson(_settings);
            StageMapFixture fixture = new StageMapFixture(_ascension, false);
            // Unity's object name and hide flags are intentionally capture-owned; generation values stay equal.
            Assert.That(fixture.settings.floorCount, Is.EqualTo(_settings.floorCount));
            Assert.That(fixture.settings.columnCount, Is.EqualTo(_settings.columnCount));
            Assert.That(fixture.settings.treasureFloor, Is.EqualTo(_settings.treasureFloor));
            RunMap authored = MapGenerator.Generate(_settings, StageMapFixture.Seed);
            RunMap captured = MapGenerator.Generate(fixture.settings, StageMapFixture.Seed);
            foreach (MapNode node in authored.GetAllNodes())
            {
                MapNode copy = captured.GetNode(node.floor, node.column);
                Assert.That(copy, Is.Not.Null);
                Assert.That(copy.type, Is.EqualTo(node.type));
                Assert.That(copy.next.Count, Is.EqualTo(node.next.Count));
            }
            fixture.Dispose();
            fixture.Dispose();
            Assert.That(fixture.settings, Is.Null);
            Assert.That(Settings.GetValue(_ascension), Is.SameAs(_settings));
            Assert.That(Seed.GetValue(_ascension), Is.EqualTo(42));
            Assert.That(JsonUtility.ToJson(_settings), Is.EqualTo(before));
        }
    }
}

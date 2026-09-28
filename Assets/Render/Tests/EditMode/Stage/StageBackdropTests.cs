using System.Collections.Generic;
using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    // The menu's establishing shot: which scene gets it, what stands in it, where the camera rests and drifts, and
    // that everything it spawned goes when it is cleared
    public class StageBackdropTests
    {
        static readonly string[] classPaths =
        {
            "Assets/Data/Characters/ClericCharacter/ClericCharacter.asset",
            "Assets/Data/Characters/DruidCharacter/DruidCharacter.asset",
            "Assets/Data/Characters/WarlockCharacter/WarlockCharacter.asset",
        };

        readonly List<Object> _created = new List<Object>();
        StageBackdrop _backdrop;

        [SetUp]
        public void SetUp()
        {
            _backdrop = new StageBackdrop();
        }

        [TearDown]
        public void TearDown()
        {
            _backdrop.Clear();
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        T Create<T>() where T : ScriptableObject
        {
            T created = ScriptableObject.CreateInstance<T>();
            _created.Add(created);
            return created;
        }

        CharacterData Character(params EntityData[] entities)
        {
            CharacterData character = Create<CharacterData>();
            character.entities = new List<EntityData>(entities);
            return character;
        }

        static GameData ShippedClasses(out List<CharacterData> classes)
        {
            classes = new List<CharacterData>();
            foreach (string path in classPaths)
            {
                CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                Assert.IsNotNull(character, path);
                classes.Add(character);
            }

            GameData data = ScriptableObject.CreateInstance<GameData>();
            data.characters = classes;
            return data;
        }

        [Test]
        public void Hosts_OnlyTheToolkitMenu()
        {
            Assert.IsTrue(StageBackdrop.Hosts(StageTarget.MenuPath));
            Assert.IsFalse(StageBackdrop.Hosts(StageTarget.MainPath));
            Assert.IsFalse(StageBackdrop.Hosts(StageTarget.SandboxPath));
            Assert.IsFalse(StageBackdrop.Hosts(""));
            Assert.IsFalse(StageBackdrop.Hosts(null));
        }

        [Test]
        public void Signatures_EachClass_IsTheLastUnitOnlyItDeploys()
        {
            EntityData normal = Create<EntityData>();
            EntityData zealot = Create<EntityData>();
            EntityData treant = Create<EntityData>();
            GameData data = Create<GameData>();
            data.characters = new List<CharacterData>
            {
                Character(normal, zealot), null, Character(treant, normal, null),
            };

            CollectionAssert.AreEqual(new[] { zealot, treant }, StageBackdrop.Signatures(data));
        }

        [Test]
        public void Signatures_ClassWithOnlySharedUnits_TakesItsLastOneNotYetStandingAndCapsTheCount()
        {
            EntityData a = Create<EntityData>();
            EntityData b = Create<EntityData>();
            EntityData c = Create<EntityData>();
            EntityData d = Create<EntityData>();
            GameData data = Create<GameData>();
            data.characters = new List<CharacterData>
            {
                Character(a), Character(b, a), Character(c), Character(d),
            };

            List<EntityData> signatures = StageBackdrop.Signatures(data);

            // a is shared, so the first class falls back to it; the second then finds b, its own
            CollectionAssert.AreEqual(new[] { a, b, c }, signatures);
            Assert.AreEqual(StageBackdrop.MaxCreatures, signatures.Count);
        }

        [Test]
        public void Signatures_NoData_IsEmpty()
        {
            CollectionAssert.IsEmpty(StageBackdrop.Signatures(null));
        }

        // Each shipped class stands for itself: one of its own units, deployed by no other class
        [Test]
        public void Signatures_ShippedClasses_OneUnitOwnToEachClass()
        {
            GameData data = ShippedClasses(out List<CharacterData> classes);
            _created.Add(data);

            List<EntityData> signatures = StageBackdrop.Signatures(data);

            Assert.AreEqual(classes.Count, signatures.Count);
            for (int i = 0; i < classes.Count; i++)
            {
                Assert.Contains(signatures[i], classes[i].entities, classes[i].name);
                for (int other = 0; other < classes.Count; other++)
                {
                    if (other != i)
                    {
                        CollectionAssert.DoesNotContain(classes[other].entities, signatures[i],
                            $"{signatures[i].name} of {classes[i].name} is also {classes[other].name}'s.");
                    }
                }
            }
        }

        [Test]
        public void RestPose_LooksAlongThePortraitHeadingDownAtTheBoard()
        {
            Bounds board = StageBackdrop.Board;

            Pose pose = StageBackdrop.RestPose(board);

            Vector3 euler = pose.rotation.eulerAngles;
            Assert.AreEqual(StageBackdrop.Pitch, euler.x, 0.01f);
            Assert.AreEqual(StageCalibration.PortraitYaw, euler.y, 0.01f);
            Assert.AreEqual(board.max.y + StageBackdrop.Height, pose.position.y, 0.001f);
            Vector3 toBoard = board.center - pose.position;
            Assert.Greater(Vector3.Dot(toBoard, pose.rotation * Vector3.forward), 0f, "The board is in front.");
            Assert.Less(pose.position.x, board.min.x, "The camera stands off the allied edge, not over the board.");
        }

        [Test]
        public void Feet_EveryCreature_StandsOnTheBoardInsideThePortraitView()
        {
            Bounds board = StageBackdrop.Board;
            Pose pose = StageBackdrop.RestPose(board);
            Rect frame = Rect.MinMaxRect(0.05f, 0.25f, 0.95f, 0.75f);

            for (int i = 0; i < StageBackdrop.MaxCreatures; i++)
            {
                Vector3 foot = StageBackdrop.Foot(board, i, StageCalibration.CellSize);
                Assert.IsTrue(board.min.x <= foot.x && foot.x <= board.max.x && board.min.z <= foot.z
                    && foot.z <= board.max.z, $"Foot {i} at {foot}");
                Assert.AreEqual(board.max.y, foot.y, 0.0001f);
                // A body a cell tall from the foot fits the middle band the title and the actions leave open
                Bounds body = new Bounds(foot + Vector3.up * 0.5f, new Vector3(0.6f, 1f, 0.6f));
                Assert.IsTrue(StageCalibration.Contains(body, pose, StageCalibration.PortraitFov,
                    StageCalibration.PortraitAspect, frame), $"Creature {i} leaves the open middle of the screen.");
            }
        }

        [Test]
        public void DriftPose_StartsAtRestKeepsTheViewAndStaysUnderTheReframeDistance()
        {
            Pose rest = StageBackdrop.RestPose(StageBackdrop.Board);

            Pose start = StageBackdrop.DriftPose(rest, 0f);
            Assert.AreEqual(0f, Vector3.Distance(rest.position, start.position), 0.0001f);
            float widest = 0f;
            for (float time = 0f; time < StageBackdrop.DriftPeriod; time += 0.25f)
            {
                Pose pose = StageBackdrop.DriftPose(rest, time);
                Assert.AreEqual(0f, Quaternion.Angle(rest.rotation, pose.rotation), 0.001f);
                widest = Mathf.Max(widest, Vector3.Distance(rest.position, pose.position));
            }

            Assert.Greater(widest, 0.05f, "The camera does drift.");
            // StageEnvironment reframes the ring past half a unit; the drift must never trigger that
            Assert.Less(widest, 0.5f);
            Pose later = StageBackdrop.DriftPose(rest, StageBackdrop.DriftPeriod);
            Assert.AreEqual(0f, Vector3.Distance(start.position, later.position), 0.001f, "It loops.");
        }

        [Test]
        public void Init_ShippedClasses_StandsTheirSignatureCreaturesUnderOneHostInTheScene()
        {
            RenderManager prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageSceneFixture.PrefabPath)
                .GetComponent<RenderManager>();
            GameData data = ShippedClasses(out List<CharacterData> classes);
            _created.Add(data);
            Scene scene = SceneManager.GetActiveScene();

            _backdrop.Init(scene, prefab.creatureLooks, prefab.meshes, null, StageBackdrop.Board,
                StageCalibration.CellSize, StageBackdrop.Signatures(data));

            Assert.IsTrue(_backdrop.isAttached);
            Assert.AreEqual(StageBackdrop.HostName, _backdrop.host.name);
            Assert.AreEqual(scene, _backdrop.host.scene);
            Assert.AreEqual(3, _backdrop.creatureCount);
            Assert.Greater(_backdrop.host.GetComponentsInChildren<Renderer>(true).Length, 0);
            Assert.AreEqual(0, _backdrop.host.GetComponentsInChildren<Camera>(true).Length);
            Assert.AreEqual(0, _backdrop.host.GetComponentsInChildren<AudioListener>(true).Length);
        }

        [Test]
        public void Clear_AfterInit_DestroysTheHostAndEveryCreature()
        {
            RenderManager prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageSceneFixture.PrefabPath)
                .GetComponent<RenderManager>();
            GameData data = ShippedClasses(out List<CharacterData> classes);
            _created.Add(data);
            _backdrop.Init(SceneManager.GetActiveScene(), prefab.creatureLooks, prefab.meshes, null,
                StageBackdrop.Board, StageCalibration.CellSize, StageBackdrop.Signatures(data));
            GameObject host = _backdrop.host;

            _backdrop.Clear();

            Assert.IsFalse(host, "The host is destroyed.");
            Assert.IsFalse(_backdrop.isAttached);
            Assert.AreEqual(0, _backdrop.creatureCount);
        }

        [Test]
        public void Tick_WithCamera_MovesItAlongTheDriftAndNeverTurnsIt()
        {
            GameObject cameraGo = new GameObject("Backdrop Camera");
            _created.Add(cameraGo);
            Camera camera = cameraGo.AddComponent<Camera>();
            Pose rest = StageBackdrop.RestPose(StageBackdrop.Board);
            cameraGo.transform.SetPositionAndRotation(rest.position, rest.rotation);
            _backdrop.Init(SceneManager.GetActiveScene(), null, null, camera, StageBackdrop.Board,
                StageCalibration.CellSize, null);

            _backdrop.Tick(StageBackdrop.DriftPeriod * 0.25f, 0.1f);

            Pose expected = StageBackdrop.DriftPose(rest, StageBackdrop.DriftPeriod * 0.25f);
            Assert.AreEqual(0f, Vector3.Distance(expected.position, cameraGo.transform.position), 0.0001f);
            Assert.AreEqual(0f, Quaternion.Angle(rest.rotation, cameraGo.transform.rotation), 0.001f);
            Assert.AreEqual(0, _backdrop.creatureCount, "No looks, no creatures, and no failure.");
        }
    }
}

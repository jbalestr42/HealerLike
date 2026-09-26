using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HealerLike.Render.Stage
{
    public class StageMapRun : AStageRun
    {
        [Serializable]
        public class BattleEvidence
        {
            public int floor;
            public string roomType;
            public string authoredWave;
            public int alliesBeforeDeployment;
            public int alliesAtBattleStart;
            public int enemiesAtBattleStart;
        }

        [Serializable]
        public class SpellEvidence
        {
            public int floor;
            public string spell;
            public string target;
            public bool isHealing;
            public bool targetVerifiedByRaycast;
            public Vector2 targetScreenPoint;
            public bool releaseTargetVerifiedByRaycast;
            public Vector2 releaseScreenPoint;
            public Vector3 cameraBeforeTargeting;
            public Vector3 cameraAtPress;
            public Vector3 cameraAtRelease;
            public Rect viewportBeforeTargeting;
            public Rect viewportAtPress;
            public Rect viewportAtRelease;
            public bool worldTapEndedInteraction;
            public bool worldTapCanceled;
            public float positiveHealth;
            public float negativeHealth;
            public float manaConsumed;
            public int affectedEntities;
        }

        [Serializable]
        public class MapFrame
        {
            public string name;
            public string scenario;
            public int width;
            public int height;
            public Rect dialog;
            public Rect viewport;
            public Rect graph;
            public int currentFloor;
            public int visited;
            public int expectedEdges;
            public int drawnEdges;
            public List<Room> rooms = new List<Room>();
        }

        [Serializable]
        public class Room
        {
            public int floor;
            public int column;
            public string type;
            public string state;
            public bool enabled;
            public Rect bounds;
        }

        [Serializable]
        public class Manifest
        {
            public string revision = StagePlay.ReadRevision();
            public bool isPassed;
            public string input
                = "Multi-frame synthetic Touch samples through StandaloneInputModule and StageTouchInput; no "
                + "Android OS input";
            public List<string> interventions = new List<string>();
            public List<string> unobserved = new List<string>();
            public List<MapFrame> maps = new List<MapFrame>();
            public int roomSelections;
            public int roundsStarted;
            public int battlesStarted;
            public int restHealingEvents;
            public List<BattleEvidence> battles = new List<BattleEvidence>();
            public List<SpellEvidence> spells = new List<SpellEvidence>();
        }

        readonly string _folder = System.IO.Path.Combine(StagePlay.CaptureFolder, "expedition-map");
        readonly Manifest _manifest = new Manifest();
        readonly StageInterfaceOutput _output;
        StageMapSession _session;
        public StageMapRun()
        {
            _output = new StageInterfaceOutput(_folder);
        }

        protected override bool shouldStartGame
        {
            get
            {
                return false;
            }
        }

        protected override void OnFailed(Exception error)
        {
            _output.Fail(error.ToString());
            Write(false);
            base.OnFailed(error);
        }

        protected override IEnumerator Run()
        {
            bool passed = false;
            _session = new StageMapSession(_manager, _output, _manifest);
            try
            {
                _session.Init();
                yield return new StageMapScenario(_session).Run();
                passed = true;
            }
            finally
            {
                _session.Dispose();
                _output.Write(passed);
                Write(passed);
                StagePlay.Finish(this, passed);
            }
        }

        void Write(bool passed)
        {
            _manifest.isPassed = passed && _output.manifest.failures.Count == 0;
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "map.json"), JsonUtility.ToJson(_manifest, true));
        }
    }
}

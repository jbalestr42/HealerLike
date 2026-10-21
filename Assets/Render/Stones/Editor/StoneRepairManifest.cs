using System;
using System.Collections.Generic;

namespace HealerLike.Render.Stones
{
    [Serializable]
    public class StoneRepairEntry
    {
        public string path;
        public string guid;
        public string assetHash;
        public string metaHash;
        public string authoredHash;
        public string state;
        public string reason;
        public int subMeshCount;
        public string afterAssetHash;
        public string afterAuthoredHash;
    }

    [Serializable]
    public class StoneRepairManifest
    {
        public int version = 1;
        public string project;
        public string unityVersion;
        public string sourceRevision;
        public string createdUtc;
        public string status;
        public int repaired;
        public bool rolledBack;
        public List<string> errors = new List<string>();
        public List<StoneRepairEntry> entries = new List<StoneRepairEntry>();
    }
}

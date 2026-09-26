namespace HealerLike.Render.Stage
{
    [System.Serializable]
    public class AndroidBuildReport
    {
        public string revision;
        public string unityVersion;
        public string target;
        public string backend;
        public string abi;
        public string[] sceneList;
        public string applicationId;
        public string applicationLabel;
        public string version;
        public int versionCode;
        public int activeInputHandler;
        public string inputBackend;
        public string uiInputModule;
        public string requiredPlayerInputDefine;
        public string excludedPlayerInputDefine;
        public bool inputDefinesValidatedByCompilation;
        public string buildResult;
        public long apkSizeBytes;
        public ulong buildSizeBytes;
        public double durationSeconds;
        public string outputPath;
    }

}

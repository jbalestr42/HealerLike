using System;
using System.IO;
using NUnit.Framework;

namespace HealerLike.Render.Stones
{
    public class StoneRepairBackupTests
    {
        string _folder;
        StoneRepairManifest _report;
        StoneRepairBackup _backup;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "StoneRepair-" + Guid.NewGuid().ToString("N"));
            _report = new StoneRepairManifest();
            _report.project = Path.Combine(_folder, "project");
            _report.status = "inspected";
            _report.unityVersion = "fixture";
            for (int i = 0; i < StoneRepairScope.AssetCount; i++)
            {
                string path = StoneRepairScope.AssetPath(i);
                string file = Path.Combine(_report.project, path);
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, "Authored vertices, UVs and tuning " + i);
                File.WriteAllText(file + ".meta", "guid: stable-" + i);
                StoneRepairEntry entry = new StoneRepairEntry();
                entry.path = path;
                entry.guid = "stable-" + i;
                entry.assetHash = StoneMeshFingerprint.FileHash(file);
                entry.metaHash = StoneMeshFingerprint.FileHash(file + ".meta");
                entry.authoredHash = "authored-" + i;
                entry.state = StoneOutlineState.Duplicate.ToString();
                _report.entries.Add(entry);
            }
            _backup = new StoneRepairBackup(_report, Path.Combine(_folder, "backup"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        string Source(int index)
        {
            return Path.Combine(_report.project, StoneRepairScope.AssetPath(index));
        }

        [Test]
        public void Restore_InterruptedRepair_RestoresExactAssetAndMetaBytesOnlyInChangedScope()
        {
            _backup.Create();
            File.WriteAllText(Source(0), "Interrupted rewrite");
            File.WriteAllText(Source(0) + ".meta", "Interrupted metadata");
            File.WriteAllText(Source(1), "Independent later edit");

            _backup.Restore(new[] { 0 });

            _backup.VerifyRestored(new[] { 0 });
            Assert.AreEqual(_report.entries[0].assetHash, StoneMeshFingerprint.FileHash(Source(0)));
            Assert.AreEqual(_report.entries[0].metaHash, StoneMeshFingerprint.FileHash(Source(0) + ".meta"));
            Assert.AreEqual("Independent later edit", File.ReadAllText(Source(1)));
        }

        [Test]
        public void Restore_TamperedBackup_RefusesBeforeOverwritingCurrentAsset()
        {
            _backup.Create();
            File.WriteAllText(Path.Combine(_folder, "backup", StoneRepairScope.AssetPath(0)), "Not the backup");
            File.WriteAllText(Source(0), "Current asset");

            Assert.Throws<IOException>(() => _backup.Restore(new[] { 0 }));

            Assert.AreEqual("Current asset", File.ReadAllText(Source(0)));
        }

        [Test]
        public void Restore_LaterBackupIsCorrupt_ValidatesEveryBackupBeforeWritingAnyAsset()
        {
            _backup.Create();
            File.WriteAllText(Source(0), "First current asset");
            File.WriteAllText(Source(1), "Second current asset");
            File.WriteAllText(Path.Combine(_folder, "backup", StoneRepairScope.AssetPath(1)), "Invalid backup");

            Assert.Throws<IOException>(() => _backup.Restore(new[] { 0, 1 }));

            Assert.AreEqual("First current asset", File.ReadAllText(Source(0)));
            Assert.AreEqual("Second current asset", File.ReadAllText(Source(1)));
        }

        [Test]
        public void Create_StaleInspection_RefusesAndLeavesEverySourceFileUntouched()
        {
            File.WriteAllText(Source(0), "New saved art");
            Assert.Throws<IOException>(() => _backup.Create());
            Assert.AreEqual("New saved art", File.ReadAllText(Source(0)));
            Assert.AreEqual(_report.entries[1].assetHash, StoneMeshFingerprint.FileHash(Source(1)));
        }

        [Test]
        public void Create_ExistingBackup_NeverOverwritesEarlierEvidence()
        {
            _backup.Create();
            Assert.Throws<IOException>(() => _backup.Create());
            _backup.VerifyRestored(new[] { 0, 11 });
        }

        [Test]
        public void Matches_ChangedMeshHashOrScope_RefusesPreviouslyInspectedState()
        {
            StoneRepairManifest current = UnityEngine.JsonUtility.FromJson<StoneRepairManifest>(
                UnityEngine.JsonUtility.ToJson(_report));
            Assert.IsTrue(StoneRepairScope.Matches(_report, current, out _));
            current.entries[3].assetHash = "new saved bytes";
            Assert.IsFalse(StoneRepairScope.Matches(_report, current, out _));
            current.entries[3].assetHash = _report.entries[3].assetHash;
            current.entries[3].path = "Assets/Unrelated.asset";
            Assert.IsFalse(StoneRepairScope.Matches(_report, current, out _));
        }
    }
}

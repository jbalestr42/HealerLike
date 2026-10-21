using System;
using System.Collections.Generic;
using System.IO;

namespace HealerLike.Render.Stones
{
    // Exact asset and meta bytes are secured before any mesh is changed. Rollback never regenerates artwork.
    public class StoneRepairBackup
    {
        readonly StoneRepairManifest _report;
        readonly string _folder;

        public StoneRepairBackup(StoneRepairManifest report, string folder)
        {
            _report = report;
            _folder = folder;
        }

        public void Create()
        {
            if (_report.entries == null || _report.entries.Count != StoneRepairScope.AssetCount)
            {
                throw new InvalidOperationException("Backup requires the complete twelve-variant scope.");
            }

            if (Directory.Exists(_folder))
            {
                throw new IOException("Backup folder already exists: " + _folder);
            }

            Directory.CreateDirectory(_folder);
            for (int i = 0; i < _report.entries.Count; i++)
            {
                StoneRepairEntry entry = Entry(i);
                string source = Path.Combine(_report.project, entry.path);
                string backup = Path.Combine(_folder, entry.path);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(source, backup, false);
                File.Copy(source + ".meta", backup + ".meta", false);
                Verify(backup, entry);
            }
        }

        public void Restore(IEnumerable<int> changed)
        {
            List<int> indices = new List<int>(changed);
            foreach (int index in indices)
            {
                StoneRepairEntry entry = Entry(index);
                Verify(Path.Combine(_folder, entry.path), entry);
            }

            foreach (int index in indices)
            {
                StoneRepairEntry entry = Entry(index);
                string source = Path.Combine(_report.project, entry.path);
                string backup = Path.Combine(_folder, entry.path);
                File.Copy(backup, source, true);
                File.Copy(backup + ".meta", source + ".meta", true);
                Verify(source, entry);
            }
        }

        public void VerifyRestored(IEnumerable<int> changed)
        {
            foreach (int index in changed)
            {
                StoneRepairEntry entry = Entry(index);
                Verify(Path.Combine(_report.project, entry.path), entry);
            }
        }

        StoneRepairEntry Entry(int index)
        {
            if (index < 0 || index >= StoneRepairScope.AssetCount || index >= _report.entries.Count)
            {
                throw new InvalidOperationException("Backup index is outside the fixed stone variant scope.");
            }

            StoneRepairEntry entry = _report.entries[index];
            if (entry == null || entry.path != StoneRepairScope.AssetPath(index))
            {
                throw new InvalidOperationException("Backup entry is outside the fixed stone variant scope.");
            }

            return entry;
        }

        static void Verify(string path, StoneRepairEntry entry)
        {
            if (StoneMeshFingerprint.FileHash(path) != entry.assetHash
                || StoneMeshFingerprint.FileHash(path + ".meta") != entry.metaHash)
            {
                throw new IOException("Backup hash does not match inspection: " + entry.path);
            }
        }
    }
}

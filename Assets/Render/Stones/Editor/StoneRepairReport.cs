using System.IO;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class StoneRepairReport
    {
        public static void Write(string path, StoneRepairManifest report)
        {
            string temporary = path + "." + System.Guid.NewGuid().ToString("N") + ".pending";
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(JsonUtility.ToJson(report, true));
                }

                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }
    }
}

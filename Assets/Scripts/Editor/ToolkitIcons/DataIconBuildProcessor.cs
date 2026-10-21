using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Bakes the missing icons before a player build
public class DataIconBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        DataIconBaker.Bake(false);
    }
}

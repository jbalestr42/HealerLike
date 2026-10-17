using NUnit.Framework;
using UnityEditor;

namespace Game
{

// The statuses of the game (Assets/Data/Statuses) and the stacks the givers apply of them
public static class StatusesTestData
{
    public static StatusData LoadStatus(string name)
    {
        StatusData status = AssetDatabase.LoadAssetAtPath<StatusData>($"Assets/Data/Statuses/{name}Status/{name}Status.asset");
        Assert.IsNotNull(status, name);
        return status;
    }

    // The effect gives the stacks of the status (e.g. 2 Vulnerable)
    public static void AssertStacks(ABuffHandlerFactory effect, string statusName, int stacks)
    {
        StatusStacksFactory statusStacks = effect as StatusStacksFactory;
        Assert.IsNotNull(statusStacks, $"stacks of {statusName}");
        Assert.AreSame(LoadStatus(statusName), statusStacks.data.status);
        Assert.AreEqual(stacks, statusStacks.data.stacks, statusName);
    }
}

}

using NUnit.Framework;
using UnityEditor;

namespace Game.Balance
{

// The units and items made for the simulations are tagged Simulation and never met in a run
public class SimulationDataTests
{
    [Test]
    public void Simulation_IsAChildOfBalance()
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/GameData.asset");

        GameplayTag tag = data.tags.Find(registered => registered != null && registered.name == TagNames.Simulation);

        Assert.IsNotNull(tag);
        Assert.IsNotNull(tag.parent);
        Assert.AreEqual(TagNames.Balance, tag.parent.name);
    }

    [TestCase("PunchingBagEntity")]
    [TestCase("BalanceDummyEntity")]
    [TestCase("BalanceSniperEntity")]
    [TestCase("BalanceGunnerEntity")]
    [TestCase("BalanceBouncerEntity")]
    [TestCase("BalanceMortarEntity")]
    [TestCase("BalanceArcEntity")]
    public void SimulationUnit_IsTaggedSimulation(string name)
    {
        EntityData unit = AssetDatabase.LoadAssetAtPath<EntityData>($"Assets/Data/Entities/{name}/{name}.asset");

        Assert.IsNotNull(unit, name);
        Assert.IsTrue(unit.HasTag(TagNames.Simulation), name);
    }

    [TestCase("Assets/Data/GameData.asset")]
    [TestCase("Assets/Data/TestData.asset")]
    public void NoSimulationUnitOrItem_InTheGameData(string gameDataPath)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);

        Assert.IsFalse(data.entities.Exists(unit => unit != null && unit.HasTag(TagNames.Simulation)), "unit");
        Assert.IsFalse(data.items.Exists(item => item != null && item.HasTag(TagNames.Simulation)), "item");
    }
}

}

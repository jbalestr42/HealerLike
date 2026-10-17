using NUnit.Framework;

namespace CharacterSkills
{

// Balance Life: every living unit ends up at the same health percent (the computation is tested in HealthBalanceTests)
public class BalanceLifeCharacterSkillTests
{
    [Test]
    public void Mode_IsRelativeByDefault()
    {
        Assert.AreEqual(BalanceLifeMode.Relative, new BalanceLifeCharacterSkillData().mode);
    }
}

}

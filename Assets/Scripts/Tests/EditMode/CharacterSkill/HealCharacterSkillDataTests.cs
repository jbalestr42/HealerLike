using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CharacterSkills
{

// Heals are positive values: only negative ones go through the damage reduction of the target
public class HealCharacterSkillDataTests
{
    static readonly string[] HealSkillPaths =
    {
        "Assets/Data/CharacterSkills/HealSingleTarget/HealSingleTarget.asset",
        "Assets/Data/CharacterSkills/HealMultiTarget/HealMultiTarget.asset",
    };

    GameObject _source;

    [SetUp]
    public void SetUp()
    {
        _source = new GameObject("Healer");
        TestHelpers.CreateAttributeManager(_source, AttributeType.HealPower, 10f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_source);
    }

    [TestCaseSource(nameof(HealSkillPaths))]
    public void HealSkill_GivesAPositiveValue(string path)
    {
        ApplyConsumerCharacterSkillFactory skill = AssetDatabase.LoadAssetAtPath<ApplyConsumerCharacterSkillFactory>(path);
        Assert.IsNotNull(skill, path);

        Assert.Greater(skill.data.multiplier, 0f, "The multiplier only scales the heal");
        Assert.Greater(skill.data.consumer.GetConsumer(_source, _source).GetValue(), 0f, "The consumer must heal, not damage");
    }
}

}

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace Game
{

// The statuses the {status:X} keywords of the descriptions are found among: every one of the game, each title once
public class StatusKeywordsDataTests
{
    static readonly string[] GameDataPaths = { "Assets/Data/GameData.asset", "Assets/Data/TestData.asset" };

    static List<StatusData> LoadStatusAssets()
    {
        List<StatusData> statuses = new List<StatusData>();
        foreach (string guid in AssetDatabase.FindAssets("t:StatusData", new[] { "Assets/Data" }))
        {
            statuses.Add(AssetDatabase.LoadAssetAtPath<StatusData>(AssetDatabase.GUIDToAssetPath(guid)));
        }
        return statuses;
    }

    [TestCaseSource(nameof(GameDataPaths))]
    public void GameData_ListsEveryStatus(string path)
    {
        GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(path);

        CollectionAssert.AreEquivalent(LoadStatusAssets(), gameData.statuses);
    }

    [Test]
    public void EveryStatus_HasItsOwnTitle()
    {
        List<string> titles = LoadStatusAssets().ConvertAll(status => status.title);

        CollectionAssert.AllItemsAreUnique(titles);
    }

    // A buff colored as a buff, a debuff as a debuff
    [TestCase("Blessed", true)]
    [TestCase("Invulnerable", true)]
    [TestCase("Hasted", true)]
    [TestCase("Empowered", true)]
    [TestCase("Fortified", true)]
    [TestCase("Regenerating", true)]
    [TestCase("Vulnerable", false)]
    [TestCase("Poisoned", false)]
    [TestCase("Chilled", false)]
    [TestCase("Blighted", false)]
    [TestCase("Sundered", false)]
    public void Status_IsPositiveOnlyForABuff(string title, bool isPositive)
    {
        StatusData status = StatusKeywords.Find(title, LoadStatusAssets());

        Assert.IsNotNull(status, title);
        Assert.AreEqual(isPositive, status.isPositive, title);
    }

    // A typo in a description would show the bare name, without its explanation
    [Test]
    public void EveryKeywordOfTheData_NamesAStatus()
    {
        GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPaths[0]);
        Regex keyword = new Regex(@"\{status:([^}]*)\}");
        List<string> unknown = new List<string>();
        foreach (string path in Directory.GetFiles("Assets/Data", "*.asset", SearchOption.AllDirectories))
        {
            foreach (Match match in keyword.Matches(File.ReadAllText(path)))
            {
                if (StatusKeywords.Find(match.Groups[1].Value, gameData.statuses) == null)
                {
                    unknown.Add($"{path}: {match.Value}");
                }
            }
        }

        CollectionAssert.IsEmpty(unknown);
    }
}

}

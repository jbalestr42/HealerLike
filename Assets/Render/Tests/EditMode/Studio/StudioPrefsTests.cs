using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Studio.Editor
{

public class StudioPrefsTests
{
    static readonly string key = "HealerLike.StudioPrefsTests";

    [TearDown]
    public void TearDown()
    {
        EditorPrefs.DeleteKey(key);
    }

    [TestCase("{}")]
    [TestCase("{\"items\":[{\"json\":\"{\\\"a\\\":1}\"}],\"selected\":-1}")]
    [TestCase("{\"name\":\"a } in a string\"}")]
    public void IsWholeObject_WellFormed_IsTrue(string json)
    {
        Assert.IsTrue(StudioPrefs.IsWholeObject(json));
    }

    [TestCase("")]
    [TestCase("{\"items\":[{\"json\":\"x\"}")]
    [TestCase("{\"name\":\"cut")]
    [TestCase("{}{}")]
    [TestCase("not json")]
    public void IsWholeObject_CutOrForeign_IsFalse(string json)
    {
        Assert.IsFalse(StudioPrefs.IsWholeObject(json));
    }

    [Test]
    public void ReadJson_NothingKept_ReturnsNull()
    {
        EditorPrefs.DeleteKey(key);

        Assert.IsNull(StudioPrefs.ReadJson(key));
    }

    [Test]
    public void ReadJson_CutShort_LogsDropsTheKeyAndReturnsNull()
    {
        EditorPrefs.SetString(key, "{\"items\":[{\"json\":");
        LogAssert.Expect(LogType.Error, "[StudioPrefs] Dropped the unreadable drafts under " + key);

        string json = StudioPrefs.ReadJson(key);

        Assert.IsNull(json);
        Assert.IsFalse(EditorPrefs.HasKey(key));
    }

    [Test]
    public void ReadJson_WholeObject_ReturnsIt()
    {
        EditorPrefs.SetString(key, " {\"selected\":2} ");

        Assert.AreEqual("{\"selected\":2}", StudioPrefs.ReadJson(key));
    }
}

}

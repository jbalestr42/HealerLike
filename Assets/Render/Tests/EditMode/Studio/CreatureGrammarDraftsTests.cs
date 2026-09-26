using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureGrammarDraftsTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    CreatureGrammarDrafts _drafts;

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
        _drafts = new CreatureGrammarDrafts();
    }

    [TearDown]
    public void TearDown()
    {
        _drafts.Dispose();
        _prefs.Restore();
    }

    [TestCase("{\"items\":[null]}")]
    [TestCase("{\"items\":[{\"json\":\"{}\"},{\"json\":\"{broken}\"}]}")]
    public void Restore_CorruptRecord_ReleasesPartialDraftsAndDropsTheCollection(string json)
    {
        string key = "HealerLike.CreatureStudio.GrammarDrafts." + Application.dataPath;
        EditorPrefs.SetString(key, json);
        int before = Resources.FindObjectsOfTypeAll<CreatureGrammarPreset>().Length;
        LogAssert.Expect(LogType.Error, "[StudioPrefs] Dropped the unreadable drafts under " + key);

        CreatureGrammarDraftCollection collection = _drafts.Restore();

        Assert.IsNull(collection);
        Assert.IsEmpty(_drafts.items);
        Assert.IsFalse(EditorPrefs.HasKey(key));
        Assert.AreEqual(before, Resources.FindObjectsOfTypeAll<CreatureGrammarPreset>().Length);
    }
}

}

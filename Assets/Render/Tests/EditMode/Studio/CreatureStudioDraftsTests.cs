using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{

public class CreatureStudioDraftsTests
{
    readonly StudioPrefsBackup _prefs = new StudioPrefsBackup();
    readonly List<CreatureStudioDrafts> _drafts = new List<CreatureStudioDrafts>();

    [SetUp]
    public void SetUp()
    {
        _prefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (CreatureStudioDrafts drafts in _drafts)
        {
            drafts.Dispose();
        }
        _drafts.Clear();
        _prefs.Restore();
    }

    CreatureStudioDrafts CreateDrafts()
    {
        CreatureStudioDrafts drafts = new CreatureStudioDrafts();
        drafts.Init();
        _drafts.Add(drafts);
        return drafts;
    }

    [Test]
    public void Init_FirstOpen_DraftsTheStarters()
    {
        CreatureStudioDrafts drafts = CreateDrafts();

        Assert.AreEqual(CreatureStudioAuthoring.SampleNames.Length, drafts.items.Count);
        foreach (CreatureRecipe draft in drafts.items)
        {
            Assert.IsFalse(AssetDatabase.Contains(draft));
            Assert.IsEmpty(CreatureStudioAuthoring.Validate(draft));
        }
    }

    [Test]
    public void Persist_EditedSelection_RestoresItByValue()
    {
        CreatureStudioDrafts drafts = CreateDrafts();
        CreatureRecipe copy = drafts.Add(CreatureStudioAuthoring.Clone(drafts.items[0]));
        copy.name = "Saved local draft";
        copy.parts[0].id = "Independent root";

        drafts.Persist(copy);
        CreatureStudioDrafts reopened = CreateDrafts();

        Assert.AreEqual("Saved local draft", reopened.restoredSelection.name);
        Assert.AreEqual("Independent root", reopened.restoredSelection.parts[0].id);
        Assert.AreNotSame(copy, reopened.restoredSelection);
        Assert.AreEqual(4, reopened.items.Count);
    }

    [Test]
    public void GetSurface_NothingRemembered_ReadsTheRecipesBody()
    {
        CreatureStudioDrafts drafts = CreateDrafts();

        Assert.AreEqual(LookSide.Plant, drafts.GetSurface(drafts.items[1]));
        Assert.AreEqual(LookSide.Stone, drafts.GetSurface(drafts.items[2]));
    }

    [Test]
    public void RememberSurface_Draft_SurvivesReopeningWithoutDirtyingIt()
    {
        CreatureStudioDrafts drafts = CreateDrafts();
        CreatureRecipe sprout = drafts.items[1];
        bool isDirty = EditorUtility.IsDirty(sprout);

        drafts.RememberSurface(sprout, LookSide.Stone);
        drafts.Persist(sprout);
        CreatureStudioDrafts reopened = CreateDrafts();

        Assert.AreEqual(LookSide.Stone, drafts.GetSurface(sprout));
        Assert.AreEqual(isDirty, EditorUtility.IsDirty(sprout));
        Assert.AreEqual(LookSide.Stone, reopened.GetSurface(reopened.restoredSelection));
    }
}

}

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures.Editor.Studio;

namespace HealerLike.Render.Creatures.Tests
{
    public class CreatureStudioWindowTests
    {
        private CreatureStudioWindow window;
        private string originalDrafts;
        private bool hadDrafts;
        private string Key => "HealerLike.CreatureStudio.Drafts."+Application.dataPath;
        [SetUp] public void SetUp()
        {
            hadDrafts=EditorPrefs.HasKey(Key); originalDrafts=EditorPrefs.GetString(Key);
            EditorPrefs.DeleteKey(Key);
            window=ScriptableObject.CreateInstance<CreatureStudioWindow>();
        }
        [TearDown] public void TearDown()
        {
            if (window!=null) Object.DestroyImmediate(window);
            if (hadDrafts) EditorPrefs.SetString(Key,originalDrafts); else EditorPrefs.DeleteKey(Key);
        }
        private T Get<T>(string name)=>(T)typeof(CreatureStudioWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
        private void Invoke(string name)=>typeof(CreatureStudioWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);

        [Test] public void StartsWithIndependentUsableDrafts()
        {
            var drafts=Get<List<CreatureRecipe>>("drafts");
            Assert.That(drafts.Count,Is.EqualTo(CreatureStudioAuthoring.SampleNames.Length));
            foreach (var draft in drafts)
            {
                Assert.That(AssetDatabase.Contains(draft),Is.False);
                Assert.That(CreatureStudioAuthoring.Validate(draft),Is.Empty);
                Assert.That(draft.parts.Length,Is.GreaterThan(0));
            }
        }
        [Test] public void DuplicateAndReopenKeepDetachedPartsAndSelection()
        {
            var source=Get<CreatureRecipe>("selected");
            string originalId=source.parts[0].id;
            Invoke("Duplicate");
            var copy=Get<CreatureRecipe>("selected");
            copy.name="Saved local draft";
            copy.parts[0].id="Independent root";
            Assert.That(source.parts[0].id,Is.EqualTo(originalId));
            Object.DestroyImmediate(window);
            window=ScriptableObject.CreateInstance<CreatureStudioWindow>();
            copy=Get<CreatureRecipe>("selected");
            Assert.That(copy.name,Is.EqualTo("Saved local draft"));
            Assert.That(copy.parts[0].id,Is.EqualTo("Independent root"));
            Assert.That(AssetDatabase.Contains(copy),Is.False);
        }
        [Test] public void PartToolsAddAChildThenRemoveTheSelectedSubtree()
        {
            var recipe=Get<CreatureRecipe>("selected");
            int count=recipe.parts.Length;
            Invoke("AddPart");
            Assert.That(recipe.parts.Length,Is.EqualTo(count+1));
            Assert.That(Get<int>("selectedPart"),Is.EqualTo(count));
            Assert.That(recipe.parts[count].parent,Is.EqualTo(0));
            Invoke("RemovePart");
            Assert.That(recipe.parts.Length,Is.EqualTo(count));
            Assert.That(CreatureStudioAuthoring.Validate(recipe),Is.Empty);
        }
        [Test] public void OpenRecipeSelectsAnExternalRecipe()
        {
            var recipe=CreatureStudioAuthoring.BuildSample(1);
            CreatureStudioWindow opened=null;
            try
            {
                opened=CreatureStudioWindow.OpenRecipe(recipe);
                var field=typeof(CreatureStudioWindow).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(field.GetValue(opened),Is.SameAs(recipe));
            }
            finally
            {
                if (opened!=null && opened!=window) Object.DestroyImmediate(opened);
                Object.DestroyImmediate(recipe);
            }
        }
    }
}

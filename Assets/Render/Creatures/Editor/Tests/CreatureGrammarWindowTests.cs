using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Creatures.Editor.Studio;
using HealerLike.Render.Creatures.Studio;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures.Tests
{
    public class CreatureGrammarWindowTests
    {
        private CreatureStudioWindow window;
        private string grammarBackup,partsBackup;
        private bool hadGrammar,hadParts;
        private string GrammarKey=>"HealerLike.CreatureStudio.GrammarDrafts."+Application.dataPath;
        private string PartsKey=>"HealerLike.CreatureStudio.Drafts."+Application.dataPath;
        [SetUp] public void SetUp()
        {
            hadGrammar=EditorPrefs.HasKey(GrammarKey); grammarBackup=EditorPrefs.GetString(GrammarKey);
            hadParts=EditorPrefs.HasKey(PartsKey); partsBackup=EditorPrefs.GetString(PartsKey);
            EditorPrefs.DeleteKey(GrammarKey); EditorPrefs.DeleteKey(PartsKey);
            window=ScriptableObject.CreateInstance<CreatureStudioWindow>();
        }
        [TearDown] public void TearDown()
        {
            if (window!=null) Object.DestroyImmediate(window);
            if (hadGrammar) EditorPrefs.SetString(GrammarKey,grammarBackup); else EditorPrefs.DeleteKey(GrammarKey);
            if (hadParts) EditorPrefs.SetString(PartsKey,partsBackup); else EditorPrefs.DeleteKey(PartsKey);
        }
        private T Get<T>(string name)=>(T)typeof(CreatureStudioWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
        private void Invoke(string name)=>typeof(CreatureStudioWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);

        [Test] public void DefaultWorkflowIsEditableGrammarAndRegeneratesRealOutput()
        {
            Assert.That(Get<bool>("grammarMode"),Is.True);
            var preset=Get<CreatureGrammarPreset>("grammarSelected");
            var before=Get<CreatureRecipe>("grammarOutput");
            Assert.That(before,Is.Not.Null);
            preset.head=HeadKind.Spear;
            Invoke("RegenerateGrammar");
            var after=Get<CreatureRecipe>("grammarOutput");
            Assert.That(after,Is.Not.Null);
            Assert.That(after,Is.Not.SameAs(before));
            Assert.That(preset.head,Is.EqualTo(HeadKind.Spear));
            Assert.That(CreatureStudioAuthoring.Validate(after),Is.Empty);
            Assert.That(AssetDatabase.Contains(after),Is.False);
        }

        [Test] public void RegenerationPreservesInspectorScroll()
        {
            var scroll=new Vector2(0,430);
            typeof(CreatureStudioWindow).GetField("inspectorScroll",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,scroll);
            Get<CreatureGrammarPreset>("grammarSelected").head=HeadKind.Spear;
            Invoke("RegenerateGrammar");
            Assert.That(Get<Vector2>("inspectorScroll"),Is.EqualTo(scroll));
        }

        [Test] public void ChosenGameOverrideTableSurvivesReopening()
        {
            var table=ScriptableObject.CreateInstance<CreatureLooks>();
            string path=AssetDatabase.GenerateUniqueAssetPath("Assets/__CreatureStudioAlternateLooks.asset");
            try
            {
                AssetDatabase.CreateAsset(table,path);
                typeof(CreatureStudioWindow).GetField("creatureLooks",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,table);
                Object.DestroyImmediate(window);
                window=ScriptableObject.CreateInstance<CreatureStudioWindow>();
                Assert.That(Get<CreatureLooks>("creatureLooks"),Is.SameAs(table));
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [Test] public void BakeIsAnIndependentManualDraftAndKeepsGrammarPreset()
        {
            var preset=Get<CreatureGrammarPreset>("grammarSelected");
            var output=Get<CreatureRecipe>("grammarOutput");
            string id=output.parts[0].id;
            Invoke("BakeGrammar");
            var baked=Get<CreatureRecipe>("selected");
            Assert.That(Get<bool>("grammarMode"),Is.False);
            Assert.That(baked,Is.Not.SameAs(output));
            baked.parts[0].id="Independent authored edit";
            Assert.That(output.parts[0].id,Is.EqualTo(id));
            Assert.That(Get<CreatureGrammarPreset>("grammarSelected"),Is.SameAs(preset));
            Invoke("SwitchToGrammar");
            Assert.That(Get<CreatureRecipe>("partsSelection"),Is.SameAs(baked));
            Assert.That(Get<bool>("grammarMode"),Is.True);
        }

        [Test] public void LocalGrammarChannelsAndVocabularySurviveReopening()
        {
            var preset=Get<CreatureGrammarPreset>("grammarSelected");
            preset.displayName="Persistent grammar"; preset.head=HeadKind.Arch; preset.mass=MassBand.Heavy;
            var vocabulary=preset.vocabulary;
            Object.DestroyImmediate(window);
            window=ScriptableObject.CreateInstance<CreatureStudioWindow>();
            preset=Get<CreatureGrammarPreset>("grammarSelected");
            Assert.That(preset.displayName,Is.EqualTo("Persistent grammar"));
            Assert.That(preset.head,Is.EqualTo(HeadKind.Arch));
            Assert.That(preset.mass,Is.EqualTo(MassBand.Heavy));
            Assert.That(preset.vocabulary,Is.SameAs(vocabulary));
            Assert.That(Get<CreatureRecipe>("grammarOutput"),Is.Not.Null);
        }

        [Test] public void SavedGrammarRemainsChannelsWhenOpenedAndBaked()
        {
            var source=Get<CreatureGrammarPreset>("grammarSelected");
            var preset=Object.Instantiate(source); preset.hideFlags=HideFlags.None; preset.head=HeadKind.Pulse;
            string path=AssetDatabase.GenerateUniqueAssetPath("Assets/__CreatureGrammarWindowTest.asset");
            CreatureStudioWindow opened=null;
            try
            {
                AssetDatabase.CreateAsset(preset,path);
                opened=CreatureStudioWindow.OpenGrammar(preset);
                var field=typeof(CreatureStudioWindow).GetField("grammarSelected",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(field.GetValue(opened),Is.SameAs(preset));
                typeof(CreatureStudioWindow).GetMethod("BakeGrammar",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(opened,null);
                Assert.That(preset.head,Is.EqualTo(HeadKind.Pulse));
                Assert.That(AssetDatabase.LoadAssetAtPath<CreatureGrammarPreset>(path),Is.SameAs(preset));
            }
            finally
            {
                if (opened!=null && opened!=window) Object.DestroyImmediate(opened);
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitRosterTests
    {
        [Test]
        public void DraggingSecondIdenticalChoiceRetainsBothSlotsWhenGameplayConsumesTheFirst()
        {
            GameObject root = new GameObject("Identical roster fixture");
            root.SetActive(false);
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                SelectEntityButton a = root.AddComponent<SelectEntityButton>(); a.data = data;
                SelectEntityButton b = root.AddComponent<SelectEntityButton>(); b.data = data;
                Entity deployed = null;
                TestHelpers.WithLoggingDisabled(() => deployed = root.AddComponent<Entity>());
                deployed.data = data;
                GameView legacy = root.AddComponent<GameView>();
                TestHelpers.SetPrivateField(legacy, "_entityInventory", root.AddComponent<EntityInventory>());
                List<SelectEntityButton> choices = new List<SelectEntityButton> { a, b };
                TestHelpers.SetPrivateField(legacy.entityInventory, "_entityButtons", choices);
                EntityManager entities = root.AddComponent<EntityManager>();
                TestHelpers.SetPrivateField(entities, "_entities", new Dictionary<Entity.EntityType, List<GameObject>>());
                ToolkitGameContext context = new ToolkitGameContext { legacy = legacy, entities = entities };
                TemplateContainer tree = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                ToolkitGameView view = new ToolkitGameView(tree);
                try
                {
                    ToolkitPartyPanel panel = new ToolkitPartyPanel();
                    panel.Init(context, view, null);
                    panel.Refresh(true);
                    List<Button> cards = tree.Q("party-list").Query<Button>("data-card").ToList();
                    string[] keys = cards.Select(c => ((ToolkitCardModel)c.userData).key).ToArray();
                    // Exercise the production callback, including its choice bookkeeping.
                    ((ToolkitCardModel)cards[1].userData).deployed(deployed);
                    // Julien consumes the first matching EntityData, even for a drag of the second card.
                    choices.RemoveAt(0);
                    entities.GetEntities(Entity.EntityType.Player).Add(deployed.gameObject);
                    for (int refresh = 0; refresh < 2; refresh++)
                    {
                        panel.Refresh(true);
                        cards = tree.Q("party-list").Query<Button>("data-card").ToList();
                        ToolkitCardModel[] models = cards.Select(c => (ToolkitCardModel)c.userData).ToArray();
                        Assert.That(models.Select(m => m.key), Is.EqualTo(keys));
                        Assert.That(models.Length, Is.EqualTo(2));
                        Assert.That(models[0].source, Is.SameAs(data));
                        Assert.That(models[0].canDrag, Is.True);
                        Assert.That(models[1].source, Is.SameAs(deployed));
                        Assert.That(models.Count(m => ReferenceEquals(m.source, deployed)), Is.EqualTo(1));
                        Assert.That(models[1].canDrag, Is.False);
                    }
                }
                finally { view.Release(); }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
        }
        [Test]
        public void SameDataChoicesKeepDistinctIdentityAndOrderAcrossDeployment()
        {
            GameObject root = new GameObject("Roster fixture");
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                SelectEntityButton a = root.AddComponent<SelectEntityButton>(); a.data = data;
                SelectEntityButton b = root.AddComponent<SelectEntityButton>(); b.data = data;
                Entity deployed = null;
                TestHelpers.WithLoggingDisabled(() => deployed = root.AddComponent<Entity>());
                deployed.data = data;
                ToolkitRoster roster = new ToolkitRoster();
                roster.Sync(new[] { a, b }, new Entity[0]);
                string first = roster.entries[0].key, second = roster.entries[1].key;
                Assert.That(first, Is.Not.EqualTo(second));
                roster.Sync(new[] { b }, new[] { deployed });
                Assert.That(roster.entries.Count, Is.EqualTo(2));
                Assert.That(roster.entries[0].key, Is.EqualTo(first));
                Assert.That(roster.entries[0].entity, Is.SameAs(deployed));
                Assert.That(roster.entries[1].key, Is.EqualTo(second));
                Assert.That(roster.entries[1].choice, Is.SameAs(b));
                roster.Sync(new[] { b }, new[] { deployed });
                Assert.That(roster.entries.Count, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
        }
    }
}

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
        public void DraggingSecondIdenticalUnitRetainsBothCardsWhenTheRosterGivesTheFirst()
        {
            GameObject root = new GameObject("Identical roster fixture");
            root.SetActive(false);
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                Entity deployed = null;
                TestHelpers.WithLoggingDisabled(() => deployed = root.AddComponent<Entity>());
                deployed.data = data;
                PlayerBehaviour player = root.AddComponent<PlayerBehaviour>();
                player.roster.Init(new[] { data, data });
                EntityManager entities = root.AddComponent<EntityManager>();
                TestHelpers.SetPrivateField(entities, "_entities", new Dictionary<Entity.EntityType, List<GameObject>>());
                ToolkitGameContext context = new ToolkitGameContext { player = player, entities = entities };
                TemplateContainer tree = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
                ToolkitGameView view = new ToolkitGameView(tree);
                try
                {
                    ToolkitPartyPanel panel = new ToolkitPartyPanel();
                    panel.Init(context, view, null);
                    panel.Refresh(true);
                    List<Button> cards = tree.Q("party-list").Query<Button>("data-card").ToList();
                    string[] keys = cards.Select(c => ((ToolkitCardModel)c.userData).key).ToArray();
                    // The second card is dragged: the production callback ties it to the unit placed
                    ((ToolkitCardModel)cards[1].userData).deployed(deployed);
                    player.roster.Remove(data);
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
                        Assert.That(models[1].canDrag, Is.False);
                    }
                }
                finally { view.Release(); }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
        }

        [Test]
        public void SameDataUnitsKeepDistinctKeysAndOrderAcrossDeployment()
        {
            GameObject root = new GameObject("Roster fixture");
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                Entity deployed = null;
                TestHelpers.WithLoggingDisabled(() => deployed = root.AddComponent<Entity>());
                deployed.data = data;
                ToolkitRoster roster = new ToolkitRoster();
                roster.Sync(new[] { data, data }, new Entity[0]);
                string first = roster.entries[0].key, second = roster.entries[1].key;
                Assert.That(first, Is.Not.EqualTo(second));

                // Placed without a drag of its card: the first copy is the one placed
                roster.Sync(new[] { data }, new[] { deployed });

                Assert.That(roster.entries.Count, Is.EqualTo(2));
                Assert.That(roster.entries[0].key, Is.EqualTo(first));
                Assert.That(roster.entries[0].entity, Is.SameAs(deployed));
                Assert.That(roster.entries[1].key, Is.EqualTo(second));
                Assert.That(roster.entries[1].isDeployed, Is.False);
                roster.Sync(new[] { data }, new[] { deployed });
                Assert.That(roster.entries.Count, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
        }

        [Test]
        public void UnitBackToTheRoster_LosesItsBoardCardAndGetsAWaitingOne()
        {
            GameObject root = new GameObject("Roster fixture");
            GameObject unit = new GameObject("Unit");
            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                Entity deployed = null;
                TestHelpers.WithLoggingDisabled(() => deployed = unit.AddComponent<Entity>());
                deployed.data = data;
                ToolkitRoster roster = new ToolkitRoster();
                roster.Sync(new EntityData[0], new[] { deployed });

                // Taken back from the board: the unit is destroyed and its data back in the roster
                TestHelpers.WithLoggingDisabled(() => Object.DestroyImmediate(unit));
                roster.Sync(new[] { data }, new Entity[0]);

                Assert.That(roster.entries.Count, Is.EqualTo(1));
                Assert.That(roster.entries[0].isDeployed, Is.False);
                Assert.That(roster.entries[0].data, Is.SameAs(data));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); if (unit != null) { TestHelpers.WithLoggingDisabled(() => Object.DestroyImmediate(unit)); } }
        }
    }
}

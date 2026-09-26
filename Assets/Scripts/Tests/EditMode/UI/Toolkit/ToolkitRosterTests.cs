using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitRosterTests
    {
        [Test]
        public void SameDataChoicesKeepDistinctIdentityAndOrderAcrossDeployment()
        {
            var root = new GameObject("Roster fixture");
            var data = ScriptableObject.CreateInstance<EntityData>();
            try
            {
                var a = root.AddComponent<SelectEntityButton>(); a.data = data;
                var b = root.AddComponent<SelectEntityButton>(); b.data = data;
                Entity deployed = null;
                TestHelpers.WithLoggingDisabled(() => deployed = root.AddComponent<Entity>());
                deployed.data = data;
                var roster = new ToolkitRoster();
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

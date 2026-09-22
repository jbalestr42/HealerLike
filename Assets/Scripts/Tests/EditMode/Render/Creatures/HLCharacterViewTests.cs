using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLCharacterViewTests
    {
        [Test] public void CharacterViewUsesAnchorAndRegistryWithoutEntityOrCharacterInit()
        {
            var go = new GameObject("HLCharacterFixture"); var anchor = new GameObject("HLAnchor"); var target = new GameObject("HLHealTarget");
            var recipe = HLCreatureValidatorTests.Recipe(); var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                Character character = null; TestHelpers.WithLoggingDisabled(() => character = go.AddComponent<Character>());
                anchor.transform.position = new Vector3(5, 1, 2); target.transform.position = anchor.transform.position + Vector3.one;
                var view = anchor.AddComponent<HLCharacterView>(); var registry = new HLRenderRegistry();
                view.Bind(character, recipe, anchor.transform, material, registry); var rig = view.Rig;
                view.Bind(character, recipe, anchor.transform, material, registry); Assert.AreSame(rig, view.Rig);
                TestHelpers.InvokePrivate(view, "LateUpdate"); Assert.AreEqual(anchor.transform.position, rig.Root.position);
                Assert.IsNull(go.GetComponent<Entity>()); Assert.IsNull(character.mana);
                registry.NotifyHeal(go, target, 0, false); Assert.AreEqual(0, rig.ActiveArmCount);
                registry.NotifyHeal(go, target, 4, true); Assert.AreEqual(1, rig.ActiveArmCount);
                view.enabled = false; TestHelpers.InvokePrivate(view, "OnDisable"); rig.Tick(1, .3f, new HLFootFrame(anchor.transform.position, Vector3.up, 1));
                rig.Tick(1.3f, .3f, new HLFootFrame(anchor.transform.position, Vector3.up, 1));
                registry.NotifyHeal(go, target, 4, true); Assert.AreEqual(0, rig.ActiveArmCount);
                view.enabled = true; TestHelpers.InvokePrivate(view, "OnEnable"); registry.NotifyHeal(go, target, 4, false); Assert.AreEqual(1, rig.ActiveArmCount);
                Object.DestroyImmediate(go);
                view.enabled = false; TestHelpers.InvokePrivate(view, "OnDisable");
                var field = typeof(HLRenderRegistry).GetField("_healSinks",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.AreEqual(0, ((System.Collections.IDictionary)field.GetValue(registry)).Count);
            }
            finally { var view = anchor.GetComponent<HLCharacterView>(); if (view) TestHelpers.InvokePrivate(view, "OnDestroy"); Object.DestroyImmediate(anchor); Object.DestroyImmediate(go); Object.DestroyImmediate(target); Object.DestroyImmediate(recipe); Object.DestroyImmediate(material); HLPrimitiveMeshes.ReleaseAll(); }
        }
        [Test] public void SignedCharacterOutcomesManaAnchorsAndDuplicateHealReports()
        {
            var go = new GameObject("HLCharacter"); var target = new GameObject("HLRecipient");
            var recipe = UnityEditor.AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>("Assets/Render/Creatures/Data/HLHealer.asset");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                Character character = null; TestHelpers.WithLoggingDisabled(() => character = go.AddComponent<Character>());
                var mana = TestHelpers.CreateResourceAttribute(go, AttributeType.ManaMax, 100);
                TestHelpers.SetPrivateField(character, "_mana", mana);
                var health = TestHelpers.CreateResourceAttribute(target, AttributeType.HealthMax, 100);
                var view = go.AddComponent<HLCharacterView>(); var registry = new HLRenderRegistry();
                view.Bind(character, recipe, go.transform, material, registry);
                var update = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), view,
                    typeof(HLCharacterView).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
                for (int i = 0; i < 10; i++) update();
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 20; i++) update();
                Assert.AreEqual(0, System.GC.GetAllocatedBytesForCurrentThread() - before);
                Assert.AreEqual(3, view.BudAnchors.Count); Assert.NotNull(view.Bud0); Assert.NotNull(view.Bud1); Assert.NotNull(view.Bud2);
                var modifier = new ResourceModifier { source = go };
                health.OnAllConsumerProcessed.Invoke(target, modifier, -5, false);
                Assert.AreEqual(1, view.CastGestureCount);
                for (int i = 0; i < 2; i++)
                {
                    health.OnAllConsumerProcessed.Invoke(target, modifier, 5, false);
                    registry.NotifyHeal(go, target, 5, false);
                }
                Assert.AreEqual(3, view.CastGestureCount);
                registry.NotifyHeal(go, target, 7, false); health.OnAllConsumerProcessed.Invoke(target, modifier, 7, false);
                Assert.AreEqual(4, view.CastGestureCount);
                TestHelpers.SetPrivateField(mana, "_value", 0f); TestHelpers.InvokePrivate(view, "LateUpdate");
                var block = new MaterialPropertyBlock(); var renderer = view.Bud0.GetComponentInChildren<Renderer>(); renderer.GetPropertyBlock(block);
                Color empty = block.GetColor("_BaseColor");
                TestHelpers.SetPrivateField(mana, "_value", 100f); TestHelpers.InvokePrivate(view, "LateUpdate"); renderer.GetPropertyBlock(block);
                Assert.Greater(block.GetColor("_BaseColor").g, empty.g);
                view.enabled = false; health.OnAllConsumerProcessed.Invoke(target, modifier, -5, false);
                Assert.AreEqual(4, view.CastGestureCount);
            }
            finally { var view = go.GetComponent<HLCharacterView>(); if (view) TestHelpers.InvokePrivate(view, "OnDestroy"); Object.DestroyImmediate(go); Object.DestroyImmediate(target); Object.DestroyImmediate(material); HLPrimitiveMeshes.ReleaseAll(); }
        }
    }
}

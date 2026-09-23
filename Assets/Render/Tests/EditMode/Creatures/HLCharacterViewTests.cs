using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Stage;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{
    public class HLCharacterViewTests
    {
        // What Init takes from the view prefab and the manager, without a manager
        static void Bind(HLCharacterView view, Character character, HLCreatureRecipe recipe, Transform anchor,
            Material material, HLRenderRegistry registry)
        {
            TestHelpers.SetPrivateField(view, "_character", character);
            TestHelpers.SetPrivateField(view, "_recipe", recipe);
            TestHelpers.SetPrivateField(view, "_visualAnchor", anchor);
            TestHelpers.SetPrivateField(view, "_material", material);
            TestHelpers.SetPrivateField(view, "_registry", registry);
            TestHelpers.InvokePrivate(view, "BuildAndRegister");
            TestHelpers.InvokePrivate(view, "ObserveResources");
        }

        [Test]
        public void CharacterViewUsesAnchorAndRegistryWithoutEntityOrCharacterInit()
        {
            GameObject go = new GameObject("HLCharacterFixture");
            GameObject anchor = new GameObject("HLAnchor");
            GameObject target = new GameObject("HLHealTarget");
            HLCreatureRecipe recipe = HLCreatureValidatorTests.Recipe();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                Character character = null;
                TestHelpers.WithLoggingDisabled(() => character = go.AddComponent<Character>());
                anchor.transform.position = new Vector3(5f, 1f, 2f);
                target.transform.position = anchor.transform.position + Vector3.one;
                HLCharacterView view = anchor.AddComponent<HLCharacterView>();
                TestHelpers.SetPrivateField(view, "_meshes", HLPrimitiveMeshesTests.Meshes());
                HLRenderRegistry registry = new HLRenderRegistry();
                Bind(view, character, recipe, anchor.transform, material, registry);
                HLCreatureRig rig = view.rig;
                Bind(view, character, recipe, anchor.transform, material, registry);
                Assert.AreSame(rig, view.rig);
                TestHelpers.InvokePrivate(view, "LateUpdate");
                Assert.AreEqual(anchor.transform.position, rig.root.position);
                Assert.IsNull(go.GetComponent<Entity>());
                Assert.IsNull(character.mana);
                registry.NotifyHeal(go, target, 0f, false);
                Assert.AreEqual(0, rig.activeArmCount);
                registry.NotifyHeal(go, target, 4f, true);
                Assert.AreEqual(1, rig.activeArmCount);
                view.enabled = false;
                TestHelpers.InvokePrivate(view, "OnDisable");
                rig.Tick(1f, 0.3f, new HLFootFrame(anchor.transform.position, Vector3.up, 1f));
                rig.Tick(1.3f, 0.3f, new HLFootFrame(anchor.transform.position, Vector3.up, 1f));
                registry.NotifyHeal(go, target, 4f, true);
                Assert.AreEqual(0, rig.activeArmCount);
                view.enabled = true;
                TestHelpers.InvokePrivate(view, "OnEnable");
                registry.NotifyHeal(go, target, 4f, false);
                Assert.AreEqual(1, rig.activeArmCount);
                Object.DestroyImmediate(go);
                view.enabled = false;
                TestHelpers.InvokePrivate(view, "OnDisable");
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo field = typeof(HLRenderRegistry).GetField("_healSinks", flags);
                Assert.AreEqual(0, ((IDictionary)field.GetValue(registry)).Count);
            }
            finally
            {
                HLCharacterView view = anchor.GetComponent<HLCharacterView>();
                if (view)
                {
                    TestHelpers.InvokePrivate(view, "OnDestroy");
                }

                Object.DestroyImmediate(anchor);
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(recipe);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void RegisteredCharacterOutcomesManaAnchorsAndNoGlobalDiscovery()
        {
            GameObject go = new GameObject("HLCharacter");
            GameObject target = new GameObject("HLRecipient");
            string recipePath = "Assets/Render/Creatures/Data/HLHealer.asset";
            HLCreatureRecipe recipe = UnityEditor.AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(recipePath);
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                Character character = null;
                TestHelpers.WithLoggingDisabled(() => character = go.AddComponent<Character>());
                ResourceAttribute mana = TestHelpers.CreateResourceAttribute(go, AttributeType.ManaMax, 100);
                TestHelpers.SetPrivateField(character, "_mana", mana);
                ResourceAttribute health = TestHelpers.CreateResourceAttribute(target, AttributeType.HealthMax, 100);
                HLCharacterView view = go.AddComponent<HLCharacterView>();
                TestHelpers.SetPrivateField(view, "_meshes", HLPrimitiveMeshesTests.Meshes());
                HLRenderRegistry registry = new HLRenderRegistry();
                Bind(view, character, recipe, go.transform, material, registry);
                HLResourceOutcomeObserver observer = target.AddComponent<HLResourceOutcomeObserver>();
                observer.Bind(health, null, null, registry);
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo updateMethod = typeof(HLCharacterView).GetMethod("Update", flags);
                Action update = (Action)Delegate.CreateDelegate(typeof(Action), view, updateMethod);
                for (int i = 0; i < 10; i++)
                {
                    update();
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 20; i++)
                {
                    update();
                }

                Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
                Assert.AreEqual(3, view.budAnchors.Count);
                Assert.NotNull(view.bud0);
                Assert.NotNull(view.bud1);
                Assert.NotNull(view.bud2);
                ResourceModifier modifier = new ResourceModifier { source = go };
                health.OnAllConsumerProcessed.Invoke(target, modifier, -5f, false);
                Assert.AreEqual(1, view.castGestureCount);
                for (int i = 0; i < 2; i++)
                {
                    health.OnAllConsumerProcessed.Invoke(target, modifier, 5f, false);
                }

                Assert.AreEqual(3, view.castGestureCount);
                health.OnAllConsumerProcessed.Invoke(target, modifier, 7f, false);
                Assert.AreEqual(4, view.castGestureCount);
                mana.OnAllConsumerProcessed.Invoke(go, modifier, 10f, false);
                Assert.AreEqual(4, view.castGestureCount, "Mana restoration must not count as a heal gesture.");
                TestHelpers.SetPrivateField(mana, "_value", 0f);
                TestHelpers.InvokePrivate(view, "LateUpdate");
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                Renderer renderer = view.bud0.GetComponentInChildren<Renderer>();
                renderer.GetPropertyBlock(block);
                Color empty = block.GetColor("_BaseColor");
                TestHelpers.SetPrivateField(mana, "_value", 100f);
                TestHelpers.InvokePrivate(view, "LateUpdate");
                renderer.GetPropertyBlock(block);
                Assert.Greater(block.GetColor("_BaseColor").g, empty.g);
                view.enabled = false;
                health.OnAllConsumerProcessed.Invoke(target, modifier, -5f, false);
                Assert.AreEqual(4, view.castGestureCount);
            }
            finally
            {
                HLCharacterView view = go.GetComponent<HLCharacterView>();
                if (view)
                {
                    TestHelpers.InvokePrivate(view, "OnDestroy");
                }

                Object.DestroyImmediate(go);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void Init_ViewPrefab_AnchorsBodyOnItself()
        {
            GameObject characterGo = new GameObject("HLCharacter");
            GameObject managerGo = new GameObject("HLRenderManager");
            string path = "Assets/Render/Creatures/Prefabs/HLHealerCharacter.prefab";
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject viewGo = Object.Instantiate(prefab, characterGo.transform);
            Character character = null;
            TestHelpers.WithLoggingDisabled(() => character = characterGo.AddComponent<Character>());
            RenderManager manager = managerGo.AddComponent<RenderManager>();
            TestHelpers.SetPrivateField(manager, "_meshes", HLPrimitiveMeshesTests.Meshes());
            HLCharacterView view = viewGo.GetComponent<HLCharacterView>();

            view.Init(character, manager);

            Assert.NotNull(view.rig);
            Assert.AreSame(viewGo.transform, view.rig.root.parent);
            TestHelpers.InvokePrivate(view, "OnDestroy");
            Object.DestroyImmediate(characterGo);
            Object.DestroyImmediate(managerGo);
        }
    }
}

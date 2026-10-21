using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{
    public abstract class UiAccessorFixture
    {
        protected GameObject root;

        [SetUp]
        public void CreateRoot()
        {
            root = new GameObject("UI accessor fixture");
            root.SetActive(false);
        }

        [TearDown]
        public void DestroyRoot()
        {
            Object.DestroyImmediate(root);
        }

        protected T CreateChild<T>() where T : Component
        {
            GameObject child = new GameObject(typeof(T).Name, typeof(RectTransform));
            child.transform.SetParent(root.transform);
            return child.AddComponent<T>();
        }
    }
}

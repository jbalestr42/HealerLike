using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GroundResourcesTests
    {
        readonly List<RenderTexture> _textures = new List<RenderTexture>();
        GroundResources _resources;

        [TearDown]
        public void TearDown()
        {
            _resources?.Dispose();
            _resources = null;
            foreach (RenderTexture texture in _textures)
            {
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            _textures.Clear();
        }

        [TestCase(1, false)]
        [TestCase(4, false)]
        [TestCase(9, false)]
        [TestCase(4, true)]
        public void Constructor_TargetCreationFails_ReleasesPartialTargetsAndMaterial(int failAt, bool throws)
        {
            if (!GroundSimulation.IsSupported())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/GroundSimulation.shader");
            int materials = MaterialCount(shader);
            Assert.Throws<InvalidOperationException>(() => _resources = new GroundResources(shader,
                GroundVolume.Create(new Rect(0f, 0f, 1f, 1f), 0.125f), texture =>
                {
                    _textures.Add(texture);
                    if (_textures.Count == failAt)
                    {
                        if (throws)
                        {
                            throw new InvalidOperationException("Injected native target failure");
                        }
                        return false;
                    }
                    return texture.Create();
                }));

            Assert.IsNull(_resources);
            Assert.AreEqual(failAt, _textures.Count);
            foreach (RenderTexture texture in _textures)
            {
                Assert.IsTrue(texture == null, "Even the failed target's Unity object must be destroyed.");
            }
            Assert.AreEqual(materials, MaterialCount(shader));
        }

        static int MaterialCount(Shader shader)
        {
            int count = 0;
            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (material.shader == shader)
                {
                    count++;
                }
            }
            return count;
        }
    }
}

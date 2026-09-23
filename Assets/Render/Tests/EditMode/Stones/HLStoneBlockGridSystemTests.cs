using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace HealerLike.Render.Stones
{
    public class HLStoneBlockGridSystemTests
    {
        [Test]
        public void RequiresConfigurationAndPreservesBookkeepingAndCellSeed()
        {
            GameObject go = new GameObject("HLSystem");
            HLStoneBlockGridSystem system = go.AddComponent<HLStoneBlockGridSystem>();
            GameObject spawned = null;
            GameObject reference = new GameObject("HLReferenceSeed");
            try
            {
                GridCell cell = new GridCell { coord = new Vector2Int(-2, 4), center = new Vector3(1f, 2f, 3f) };
                Assert.Throws<System.InvalidOperationException>(() => system.SpawnObject(cell));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => system.Configure(1, 0f));

                SerializedObject so = new SerializedObject(system);
                string blockPath = "Assets/Render/Stones/Prefabs/HLStoneBlock.prefab";
                so.FindProperty("_prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(blockPath);
                so.ApplyModifiedPropertiesWithoutUndo();

                system.Configure(4, 1f);
                spawned = system.SpawnObject(cell);
                Assert.AreEqual(1, system.count);
                Assert.AreSame(cell, system.spawnedObjects[0].cell);
                Assert.AreEqual(cell.center, spawned.transform.position);

                HLStoneAssembly.Part part = spawned.GetComponentInChildren<HLStoneTerrainClump>().assembly.parts[0];
                HLStoneTerrainClump expected = reference.AddComponent<HLStoneTerrainClump>();
                expected.Initialize(HLStoneSeed.ForCell(4, cell.coord), 1f);
                CollectionAssert.AreEqual(expected.assembly.parts[0].lease.data.vertices, part.lease.data.vertices);
            }
            finally
            {
                if (spawned != null)
                {
                    TestHelpers.InvokePrivate(spawned.GetComponentInChildren<HLStoneTerrainClump>(), "OnDestroy");
                    Object.DestroyImmediate(spawned);
                }
                Object.DestroyImmediate(reference);
                Object.DestroyImmediate(go);
            }
        }
    }
}

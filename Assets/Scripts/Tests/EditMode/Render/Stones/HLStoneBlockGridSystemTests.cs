using NUnit.Framework;
using UnityEngine;
using UnityEditor;
namespace HealerLike.Render.Stones
{
    public class HLStoneBlockGridSystemTests
    {
        [Test] public void RequiresConfigurationAndPreservesBookkeepingAndCellSeed()
        {
            var go=new GameObject("HLSystem");var system=go.AddComponent<HLStoneBlockGridSystem>();GameObject spawned=null;
            try {
                var cell=new GridCell{coord=new Vector2Int(-2,4),center=new Vector3(1,2,3)};
                Assert.Throws<System.InvalidOperationException>(()=>system.SpawnObject(cell));
                Assert.Throws<System.ArgumentOutOfRangeException>(()=>system.Configure(1,0));
                var so=new SerializedObject(system);so.FindProperty("_prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/HLStoneBlock.prefab");so.ApplyModifiedPropertiesWithoutUndo();
                system.Configure(4,1);spawned=system.SpawnObject(cell);Assert.AreEqual(1,system.count);Assert.AreSame(cell,system.spawnedObjects[0].cell);Assert.AreEqual(cell.center,spawned.transform.position);
                var part=spawned.GetComponentInChildren<HLStoneTerrainClump>().Assembly.Parts[0];
                CollectionAssert.AreEqual(HLStoneMesh.Generate(HLStoneSeed.ForPart(HLStoneSeed.ForCell(4,cell.coord),501),HLStonePresets.Shape(.65f,1.35f,.95f,.14f,part.Lease.Data.Vertices.Length==60?0:1)).Vertices,part.Lease.Data.Vertices);
            }finally{if(spawned!=null){TestHelpers.InvokePrivate(spawned.GetComponentInChildren<HLStoneTerrainClump>(),"OnDestroy");Object.DestroyImmediate(spawned);}Object.DestroyImmediate(go);}
        }
    }
}

using System.Collections.Generic;
using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StonePrefabBuilderTests
{
    static readonly string root = "Assets/Render/Stones/Prefabs/";

    [Test]
    public void DerivedStone_ShippedPrefab_CarriesTheBuilderThenTheBodyAndAHiddenShadow()
    {
        GameObject stone = AssetDatabase.LoadAssetAtPath<GameObject>(root + "DerivedStone.prefab");

        List<Component> components = new List<Component>(stone.GetComponents<Component>());
        CreatureBuilder builder = stone.GetComponent<CreatureBuilder>();
        StoneBody body = stone.GetComponent<StoneBody>();
        Assert.Less(components.IndexOf(builder), components.IndexOf(body)); // the builder makes the rig first
        StoneGroundDisc shadow = stone.transform.Find("GroundShadow").GetComponent<StoneGroundDisc>();
        Assert.IsTrue(shadow.isShadow);
        Assert.IsFalse(shadow.gameObject.activeSelf);
        SerializedObject bodySO = new SerializedObject(body);
        Assert.AreSame(shadow, bodySO.FindProperty("_groundShadow").objectReferenceValue);
    }

    [Test]
    public void StoneBlock_ShippedPrefab_KeepsHisColliderAndCarriesDiscsAndFacet()
    {
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Block.prefab");
        GameObject block = AssetDatabase.LoadAssetAtPath<GameObject>(root + "StoneBlock.prefab");
        Assert.AreEqual(original.layer, block.layer);
        BoxCollider a = original.GetComponentInChildren<BoxCollider>();
        BoxCollider b = block.GetComponentInChildren<BoxCollider>();
        Assert.AreEqual(a.size, b.size);
        Assert.AreEqual(a.center, b.center);
        Assert.AreEqual(a.transform.localPosition, b.transform.localPosition);
        Assert.AreEqual(a.isTrigger, b.isTrigger);
        Assert.AreEqual(a.gameObject.layer, b.gameObject.layer);

        StoneTerrainClump clump = block.GetComponentInChildren<StoneTerrainClump>(true);
        SerializedObject clumpSO = new SerializedObject(clump);
        StoneGroundDisc disc = (StoneGroundDisc)clumpSO.FindProperty("_groundDisc").objectReferenceValue;
        StoneGroundDisc shadow = (StoneGroundDisc)clumpSO.FindProperty("_groundShadow").objectReferenceValue;
        Assert.IsFalse(disc.isShadow);
        Assert.IsTrue(shadow.isShadow);
        Assert.AreEqual("GroundDisc", disc.GetComponent<MeshRenderer>().sharedMaterial.name);
        Assert.AreEqual("GroundShadow", shadow.GetComponent<MeshRenderer>().sharedMaterial.name);
        Assert.AreEqual("Disc", disc.GetComponent<MeshFilter>().sharedMesh.name);
        Assert.IsNotNull(clumpSO.FindProperty("_ochreFace").objectReferenceValue);
        Assert.IsNotNull(clumpSO.FindProperty("_trample").objectReferenceValue);
    }

    [Test]
    public void StoneEffects_ShippedPrefab_WiresFragmentMeshesAndMaterials()
    {
        GameObject effects = AssetDatabase.LoadAssetAtPath<GameObject>(root + "StoneEffects.prefab");
        SerializedObject effectsSO = new SerializedObject(effects.GetComponent<StoneEffects>());
        foreach (string field in new string[] { "_stoneMaterial", "_coralMaterial", "_dustMaterial", "_meshes" })
        {
            Assert.IsNotNull(effectsSO.FindProperty(field).objectReferenceValue, field);
        }

        GameObject fragment = (GameObject)effectsSO.FindProperty("_fragmentPrefab").objectReferenceValue;
        Assert.IsNotNull(fragment.GetComponent<MeshFilter>());
        Assert.IsNotNull(fragment.GetComponent<MeshRenderer>());
        Material dust = (Material)effectsSO.FindProperty("_dustMaterial").objectReferenceValue;
        Assert.AreEqual("HealerLike/Stones/Dust", dust.shader.name);
    }
}

}

using UnityEditor;
using UnityEngine;
using HealerLike.Render.Spells;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stones
{
    // The enemy view: a body that turns toward its target, the stone parts under it and a cheap ground shadow.
    // It sits under his model, whose own sockets and HUD stay live.
    public static class StoneModelAuthoring
    {
        public static void Build(string path, string name, HLStonePreset preset)
        {
            GameObject modelGo = new GameObject(name);
            GameObject body = new GameObject("BodyPivot");
            body.transform.SetParent(modelGo.transform, false);
            body.AddComponent<LookAtTarget>();
            GameObject presentation = new GameObject("HLStonePresentation");
            presentation.transform.SetParent(body.transform, false);

            HLStoneEnemyVisual visual = modelGo.AddComponent<HLStoneEnemyVisual>();
            SerializedObject visualData = new SerializedObject(visual);
            visualData.FindProperty("_bodyPivot").objectReferenceValue = body.transform;
            visualData.FindProperty("_presentation").objectReferenceValue = presentation.transform;
            visualData.FindProperty("_groundShadow").objectReferenceValue = HLStonePrefabBuilder.AddDisc(modelGo.transform, true);
            visualData.FindProperty("_preset").enumValueIndex = (int)preset;
            visualData.FindProperty("_stoneMaterial").objectReferenceValue =
                HLStonePrefabBuilder.Load<Material>(HLStonePrefabBuilder.StoneMaterialPath);
            visualData.ApplyModifiedPropertiesWithoutUndo();

            modelGo.AddComponent<HLStatusObserver>();
            modelGo.AddComponent<HLHealPulse>();
            modelGo.AddComponent<HLTrampleZone>();
            modelGo.AddComponent<HLBruiseZone>();
            PrefabUtility.SaveAsPrefabAsset(modelGo, path);
            Object.DestroyImmediate(modelGo);
        }
    }
}

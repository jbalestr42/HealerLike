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
        public static void Build(string path, string name, StonePreset preset)
        {
            GameObject modelGo = new GameObject(name);
            GameObject body = new GameObject("BodyPivot");
            body.transform.SetParent(modelGo.transform, false);
            body.AddComponent<LookAtTarget>();
            GameObject presentation = new GameObject("StonePresentation");
            presentation.transform.SetParent(body.transform, false);

            StoneEnemyVisual visual = modelGo.AddComponent<StoneEnemyVisual>();
            SerializedObject visualData = new SerializedObject(visual);
            visualData.FindProperty("_bodyPivot").objectReferenceValue = body.transform;
            visualData.FindProperty("_presentation").objectReferenceValue = presentation.transform;
            visualData.FindProperty("_groundShadow").objectReferenceValue = StonePrefabBuilder.AddDisc(modelGo.transform, true);
            visualData.FindProperty("_preset").enumValueIndex = (int)preset;
            visualData.FindProperty("_stoneMaterial").objectReferenceValue =
                StonePrefabBuilder.Load<Material>(StonePrefabBuilder.StoneMaterialPath);
            visualData.ApplyModifiedPropertiesWithoutUndo();

            modelGo.AddComponent<StatusObserver>();
            modelGo.AddComponent<HealPulse>();
            modelGo.AddComponent<TrampleZone>();
            modelGo.AddComponent<BruiseZone>();
            PrefabUtility.SaveAsPrefabAsset(modelGo, path);
            Object.DestroyImmediate(modelGo);
        }
    }
}

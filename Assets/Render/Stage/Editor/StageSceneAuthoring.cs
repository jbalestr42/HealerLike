using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HealerLike.Render.Stage
{
    // RenderStage.unity with its one RenderManager, and the battle focus controls the manager nests
    public static class StageSceneAuthoring
    {
        public static readonly string ScenePath = "Assets/Render/Stage/RenderStage.unity";
        public static readonly string ControlsPath = "Assets/Render/Stage/Prefabs/BattleFocusControls.prefab";

        public static Scene Create(GameObject manager)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(manager, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return scene;
        }

        // A screen overlay button at the top right, the way his UI lives in prefabs
        public static GameObject CreateControls()
        {
            GameObject root = new GameObject("BattleFocusControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            GameObject buttonGo = new GameObject("FocusOverview", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(root.transform, false);
            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-24f, -142f);
            buttonRect.sizeDelta = new Vector2(260f, 70f);
            buttonGo.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.22f, 0.94f);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.color = new Color(0.94f, 0.97f, 0.83f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = "Focus battle";

            BattleFocus battleFocus = root.AddComponent<BattleFocus>();
            SerializedObject data = new SerializedObject(battleFocus);
            data.FindProperty("_toggle").objectReferenceValue = buttonGo.GetComponent<Button>();
            data.FindProperty("_label").objectReferenceValue = label;
            data.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ControlsPath));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ControlsPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}

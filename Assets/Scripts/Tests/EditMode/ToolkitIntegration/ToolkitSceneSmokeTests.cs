using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using HealerLike.UI.Toolkit;
using HealerLike.UI.Toolkit.Integration;

namespace HealerLike.Tests.ToolkitIntegration
{
    public class ToolkitSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator MenuScene_StartNavigatesToToolkitGameplay()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Toolkit/MenuToolkit.unity");
            yield return new EnterPlayMode();
            for (int frame = 0; frame < 5; frame++) yield return null;
            var menu = Object.FindAnyObjectByType<ToolkitGameUI>();
            Assert.That(menu, Is.Not.Null);
            var root = menu.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.Q("menu-panel").ClassListContains("is-hidden"), Is.False);
            root.Q<Button>("start-button").Focus();
            yield return null;
            using (var submit = NavigationSubmitEvent.GetPooled()) root.Q<Button>("start-button").SendEvent(submit);
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, Is.EqualTo("MainToolkit"));
            Assert.That(Object.FindAnyObjectByType<ToolkitGameUI>(), Is.Not.Null);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator GameplayScene_StartButtonInitializesPartyAndSpells()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Toolkit/MainToolkit.unity");
            yield return new EnterPlayMode();
            for (int frame = 0; frame < 5; frame++) yield return null;
            var ui = Object.FindAnyObjectByType<ToolkitGameUI>();
            Assert.That(ui, Is.Not.Null);
            var root = ui.GetComponent<UIDocument>().rootVisualElement;
            var start = root.Q<Button>("start-button");
            Assert.That(start, Is.Not.Null);
            Assert.That(start.panel, Is.Not.Null, "The runtime document must attach to a panel.");
            Assert.That(start.enabledInHierarchy, Is.True);
            start.Focus();
            yield return null;
            // The original demo has more spells than shortcut bindings. Preserve its
            // diagnostic; only this known legacy message is expected, never all errors.
            LogAssert.Expect(LogType.Error, "Not Enough inputs");
            using (var submit = NavigationSubmitEvent.GetPooled()) start.SendEvent(submit);
            yield return new WaitForSecondsRealtime(.3f);
            AssertNativePointerBridge(root);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                System.IO.Directory.CreateDirectory("docs/validation");
                var settings = ui.GetComponent<UIDocument>().panelSettings;
                var previousTarget = settings.targetTexture;
                var target = RenderTexture.GetTemporary(1600, 900, 24);
                settings.targetTexture = target;
                for (int frame = 0; frame < 10; frame++) yield return null;
                var previousActive = RenderTexture.active;
                RenderTexture.active = target;
                var capture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                capture.Apply();
                System.IO.File.WriteAllBytes("docs/validation/Toolkit-Gameplay.png", capture.EncodeToPNG());
                Object.Destroy(capture);
                RenderTexture.active = previousActive;
                settings.targetTexture = previousTarget;
                RenderTexture.ReleaseTemporary(target);
            }
            var manager = Object.FindAnyObjectByType<GameManager>();
            Assert.That(LegacyUiReader.GameState(manager), Is.EqualTo(GameManager.GameState.Running));
            Assert.That(root.Q("party-list").Query<Button>().ToList().Count, Is.GreaterThan(0));
            Assert.That(root.Q("spell-list").Query<Button>().ToList().Count, Is.GreaterThan(0));
            Assert.That(root.Q<Button>("wave-button").enabledSelf, Is.True);
            var legacyCanvas = Object.FindAnyObjectByType<UIManager>().GetComponentsInChildren<Canvas>()
                .FirstOrDefault(canvas => canvas.renderMode != RenderMode.WorldSpace);
            Assert.That(legacyCanvas, Is.Not.Null);
            Assert.That(legacyCanvas.enabled, Is.False);
            ui.enabled = false;
            yield return null;
            Assert.That(legacyCanvas.enabled, Is.True, "Detaching Toolkit must restore legacy rendering.");
            Assert.That(root.childCount, Is.Zero);
            ui.enabled = true;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(legacyCanvas.enabled, Is.False);
            Assert.That(ui.GetComponent<UIDocument>().rootVisualElement.Q("spell-list").Query<Button>().ToList().Count, Is.GreaterThan(0));
            yield return new ExitPlayMode();
        }
        static void AssertNativePointerBridge(VisualElement root)
        {
            Assert.That(EventSystem.current, Is.Not.Null);
            var panelOrigin = RuntimePanelUtils.ScreenToPanel(root.panel, Vector2.zero);
            var panelCorner = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(Screen.width, Screen.height));
            var panelSize = panelCorner - panelOrigin;
            bool HasToolkitHit(Vector2 point)
            {
                var position = new Vector2((point.x - panelOrigin.x) / panelSize.x * Screen.width,
                    Screen.height - (point.y - panelOrigin.y) / panelSize.y * Screen.height);
                var pointer = new PointerEventData(EventSystem.current) { position = position, pointerId = -1 };
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, results);
                return results.Exists(result => result.module is PanelRaycaster);
            }
            Assert.That(HasToolkitHit(root.Q("inventory-button").worldBound.center), Is.True,
                "Native EventSystem raycasting must recognize Toolkit controls for the unchanged InteractionManager.");
            Assert.That(HasToolkitHit(root.Q("world-space").worldBound.center), Is.False,
                "The Toolkit's empty battlefield area must allow world interactions.");
        }
    }
}

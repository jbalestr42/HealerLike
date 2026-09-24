using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

// Play mode runs of the two Toolkit demo scenes
public class ToolkitGameUITests
{
    static IEnumerator WaitFrames(int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            yield return null;
        }
    }

    static void Submit(Button button)
    {
        using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
        {
            button.SendEvent(submit);
        }
    }

    static Canvas FindScreenCanvas()
    {
        foreach (Canvas canvas in Object.FindAnyObjectByType<UIManager>().GetComponentsInChildren<Canvas>())
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                return canvas;
            }
        }

        return null;
    }

    // Raycasts the EventSystem at a panel position, as the unchanged InteractionManager does
    static bool HasToolkitHit(VisualElement root, Vector2 point)
    {
        Vector2 panelOrigin = RuntimePanelUtils.ScreenToPanel(root.panel, Vector2.zero);
        Vector2 panelCorner = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(Screen.width, Screen.height));
        Vector2 panelSize = panelCorner - panelOrigin;
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = new Vector2((point.x - panelOrigin.x) / panelSize.x * Screen.width,
            Screen.height - (point.y - panelOrigin.y) / panelSize.y * Screen.height);
        pointer.pointerId = -1;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        foreach (RaycastResult result in results)
        {
            if (result.module is PanelRaycaster)
            {
                return true;
            }
        }

        return false;
    }

    [UnityTest]
    public IEnumerator StartButton_MenuScene_LoadsToolkitGameplay()
    {
        EditorSceneManager.OpenScene(ToolkitSceneNavigation.MenuPath);
        yield return new EnterPlayMode();
        yield return WaitFrames(5);
        ToolkitGameUI menu = Object.FindAnyObjectByType<ToolkitGameUI>();
        Assert.IsNotNull(menu);
        VisualElement root = menu.GetComponent<UIDocument>().rootVisualElement;
        Assert.IsFalse(root.Q("menu-panel").ClassListContains("is-hidden"));

        root.Q<Button>("start-button").Focus();
        yield return null;
        Submit(root.Q<Button>("start-button"));
        yield return WaitFrames(10);

        Assert.AreEqual(ToolkitSceneNavigation.GameplayScene, SceneManager.GetActiveScene().name);
        Assert.IsNotNull(Object.FindAnyObjectByType<ToolkitGameUI>());
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator StartButton_GameplayScene_InitializesPartyAndSpells()
    {
        EditorSceneManager.OpenScene(ToolkitSceneNavigation.GameplayPath);
        yield return new EnterPlayMode();
        yield return WaitFrames(5);
        ToolkitGameUI gameUI = Object.FindAnyObjectByType<ToolkitGameUI>();
        Assert.IsNotNull(gameUI);
        VisualElement root = gameUI.GetComponent<UIDocument>().rootVisualElement;
        Button start = root.Q<Button>("start-button");
        Assert.IsNotNull(start);
        Assert.IsNotNull(start.panel, "The runtime document must attach to a panel.");
        Assert.IsTrue(start.enabledInHierarchy);

        start.Focus();
        yield return null;
        // The demo data has more spells than shortcut bindings: only this known legacy message is expected
        LogAssert.Expect(LogType.Error, "Not Enough inputs");
        Submit(start);
        yield return new WaitForSecondsRealtime(0.3f);

        Assert.IsNotNull(EventSystem.current);
        Assert.IsTrue(HasToolkitHit(root, root.Q("inventory-button").worldBound.center),
            "The EventSystem raycast must recognize Toolkit controls for the unchanged InteractionManager.");
        Assert.IsFalse(HasToolkitHit(root, root.Q("world-space").worldBound.center),
            "The empty battlefield area must let world interactions through.");
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        Assert.AreEqual(GameManager.GameState.Running, LegacyUiReader.GameState(gameManager));
        Assert.Greater(root.Q("party-list").Query<Button>().ToList().Count, 0);
        Assert.Greater(root.Q("spell-list").Query<Button>().ToList().Count, 0);
        Assert.IsTrue(root.Q<Button>("wave-button").enabledSelf);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator OnDisable_GameplayScene_RestoresLegacyCanvasUntilEnabled()
    {
        EditorSceneManager.OpenScene(ToolkitSceneNavigation.GameplayPath);
        yield return new EnterPlayMode();
        yield return WaitFrames(5);
        ToolkitGameUI gameUI = Object.FindAnyObjectByType<ToolkitGameUI>();
        LogAssert.Expect(LogType.Error, "Not Enough inputs");
        Submit(gameUI.GetComponent<UIDocument>().rootVisualElement.Q<Button>("start-button"));
        yield return new WaitForSecondsRealtime(0.3f);
        Canvas legacyCanvas = FindScreenCanvas();
        Assert.IsNotNull(legacyCanvas);
        Assert.IsFalse(legacyCanvas.enabled);

        gameUI.enabled = false;
        yield return null;

        Assert.IsTrue(legacyCanvas.enabled, "Detaching the Toolkit must restore the legacy rendering.");
        Assert.AreEqual(0, gameUI.GetComponent<UIDocument>().rootVisualElement.childCount);

        gameUI.enabled = true;
        yield return new WaitForSecondsRealtime(0.15f);

        VisualElement root = gameUI.GetComponent<UIDocument>().rootVisualElement;
        Assert.IsFalse(legacyCanvas.enabled);
        Assert.Greater(root.Q("spell-list").Query<Button>().ToList().Count, 0);
        yield return new ExitPlayMode();
    }
}
}

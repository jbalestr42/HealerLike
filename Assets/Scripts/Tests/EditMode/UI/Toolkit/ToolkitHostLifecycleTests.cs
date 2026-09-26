using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UI.Toolkit
{

public class ToolkitHostLifecycleTests
{
    GameObject _host;
    ToolkitGameUI _gameUI;
    UIDocument _document;
    PanelSettings _originalPanel;
    SceneSetup[] _scenes;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _scenes = EditorSceneManager.GetSceneManagerSetup();
        EditorSceneManager.OpenScene(ToolkitSceneNavigation.MenuPath);
        yield return new EnterPlayMode();
        yield return WaitFrames(5);
        Object.FindAnyObjectByType<ToolkitGameUI>().enabled = false;
        _host = new GameObject("Toolkit host lifecycle test");
        _host.SetActive(false);
        _document = _host.AddComponent<UIDocument>();
        _originalPanel = ScriptableObject.CreateInstance<PanelSettings>();
        _document.panelSettings = _originalPanel;
        _gameUI = _host.AddComponent<ToolkitGameUI>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(_host);
        yield return null;
        Object.Destroy(_originalPanel);
        yield return new ExitPlayMode();
        EditorSceneManager.RestoreSceneManagerSetup(_scenes);
    }

    [UnityTest]
    public IEnumerator Init_FirstLayoutInvalid_ReenableAfterRepairBuildsInterface()
    {
        SetLayout(Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard"));
        LogAssert.Expect(LogType.Error, "[ToolkitTemplates] Required Button 'cancel-button' is missing.");
        _host.SetActive(true);
        yield return WaitFrames(3);
        Assert.IsFalse(_gameUI.enabled);
        Assert.AreEqual(0, _document.rootVisualElement.childCount);
        SetLayout(null);
        _gameUI.enabled = true;
        yield return WaitFrames(3);
        Assert.IsTrue(_gameUI.enabled);
        Assert.IsTrue(ToolkitLayoutContract.Validate(_document.rootVisualElement));
    }

    [UnityTest]
    public IEnumerator Destroy_OnlyToolkitComponent_RestoresBorrowedDocumentPanel()
    {
        _host.SetActive(true);
        yield return WaitFrames(3);
        Assert.AreNotSame(_originalPanel, _document.panelSettings);
        Object.Destroy(_gameUI);
        yield return null;
        Assert.IsNotNull(_document);
        Assert.AreSame(_originalPanel, _document.panelSettings);
        Assert.AreEqual(0, _document.rootVisualElement.childCount);
    }

    void SetLayout(VisualTreeAsset layout)
    {
        using (SerializedObject serialized = new SerializedObject(_gameUI))
        {
            serialized.FindProperty("_layout").objectReferenceValue = layout;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static IEnumerator WaitFrames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return null;
        }
    }
}
}

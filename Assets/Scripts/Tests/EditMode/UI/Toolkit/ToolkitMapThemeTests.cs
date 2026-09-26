using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;

namespace UI.Toolkit
{

public class ToolkitMapThemeTests
{
    ToolkitTestPanel _panel;
    VisualElement _root;
    ToolkitMapGraph _graph;
    RunState _run;
    string _folder;
    StyleSheet _override;
    int _selections;

    [SetUp]
    public void SetUp()
    {
        _panel = new ToolkitTestPanel();
        _root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
        _root.style.width = 720f;
        _root.style.height = 800f;
        ToolkitTheme.Apply(_root, null);
        _panel.root.Add(_root);
        _root.Q("map-panel").RemoveFromClassList("is-hidden");
        _run = ToolkitRunMapPresentationTests.CreateRun();
        _selections = 0;
        _graph = new ToolkitMapGraph(
            _root.Q<ScrollView>("map-scroll"),
            delegate
            {
                _selections++;
            }
        );
        _folder = "Assets/ToolkitMapThemeTest-" + System.Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
        string path = _folder + "/Geometry.uss";
        File.WriteAllText(path, ".game-ui { --map-node-height: 92; --map-floor-height: 144; }");
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        _override = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
    }

    [TearDown]
    public void TearDown()
    {
        _graph.Dispose();
        _panel.Dispose();
        AssetDatabase.DeleteAsset(_folder);
    }

    [UnityTest]
    public IEnumerator CustomStyle_GeometryChangedAndRestored_RepositionsRetainedNodes()
    {
        _graph.Display(_run, true);
        yield return null;
        yield return null;
        Button first = _root.Q<Button>("map-node-0-0");
        Assert.IsNotNull(first);
        Assert.AreEqual(68f, first.style.height.value.value);
        float originalTop = first.style.top.value.value;
        _root.styleSheets.Add(_override);
        yield return null;
        yield return null;
        Assert.AreSame(first, _root.Q<Button>("map-node-0-0"));
        Assert.AreEqual(92f, first.style.height.value.value);
        Assert.AreNotEqual(originalTop, first.style.top.value.value);
        Assert.AreEqual(5, _graph.connectionCount);
        _root.styleSheets.Remove(_override);
        yield return null;
        yield return null;
        Assert.AreEqual(68f, first.style.height.value.value);
        Assert.AreEqual(originalTop, first.style.top.value.value);
    }

    [Test]
    public void Dispose_OldRoomReattached_DoesNotSelectItsRun()
    {
        _graph.Display(_run, true);
        Button first = _root.Q<Button>("map-node-0-0");
        _graph.Dispose();
        _panel.root.Add(first);
        ToolkitTestPanel.Submit(first);
        Assert.AreEqual(0, _selections);
        Assert.IsNull(first.userData);
        Assert.IsNull(_root.Q("map-connections").userData);
    }
}
}

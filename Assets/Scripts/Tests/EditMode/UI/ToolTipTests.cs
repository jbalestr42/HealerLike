using NUnit.Framework;
using UnityEngine;

namespace UI
{

// The tooltip shows at its default place, or under the element hovered (e.g. a player item)
public class ToolTipTests
{
    GameObject _go;
    GameObject _targetGo;
    ToolTip _toolTip;
    RectTransform _panel;
    RectTransform _target;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ToolTip");
        _toolTip = _go.AddComponent<ToolTip>();
        GameObject panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(_go.transform, false);
        _panel = (RectTransform)panelGo.transform;
        _panel.sizeDelta = new Vector2(500f, 300f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.anchoredPosition = new Vector2(250f, 384f);
        panelGo.SetActive(false);
        TestHelpers.SetPrivateField(_toolTip, "_toolTip", panelGo);
        TestHelpers.InvokePrivate(_toolTip, "Awake");

        _targetGo = new GameObject("Target", typeof(RectTransform));
        _target = (RectTransform)_targetGo.transform;
        _target.sizeDelta = new Vector2(40f, 40f);
        _target.pivot = new Vector2(0.5f, 0.5f);
        _target.position = new Vector3(100f, 900f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_targetGo);
    }

    [Test]
    public void ShowBelow_PutsTheTopLeftCornerUnderTheTarget_AlignedOnItsLeftEdge()
    {
        _toolTip.ShowBelow(_target);

        Assert.IsTrue(_panel.gameObject.activeSelf);
        Assert.AreEqual(new Vector2(0f, 1f), _panel.pivot);
        // Target bottom left corner (80, 880), 8 below
        Assert.AreEqual(80f, _panel.position.x, 0.001f);
        Assert.AreEqual(872f, _panel.position.y, 0.001f);
    }

    [Test]
    public void Show_AfterShowBelow_IsBackAtItsDefaultPlace()
    {
        _toolTip.ShowBelow(_target);

        _toolTip.Show(true);

        Assert.AreEqual(new Vector2(0.5f, 0.5f), _panel.pivot);
        Assert.AreEqual(new Vector2(250f, 384f), _panel.anchoredPosition);
    }

    [Test]
    public void Show_False_HidesIt()
    {
        _toolTip.ShowBelow(_target);

        _toolTip.Show(false);

        Assert.IsFalse(_panel.gameObject.activeSelf);
    }
}

}

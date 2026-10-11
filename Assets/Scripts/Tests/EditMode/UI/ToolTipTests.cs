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
        AddText();
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
    public void ShowBelow_ShowsThePanelFromItsBottomLeftCorner()
    {
        _toolTip.ShowBelow(_target);

        Assert.IsTrue(_panel.gameObject.activeSelf);
        Assert.AreEqual(Vector2.zero, _panel.pivot);
    }

    static readonly Vector2 Screen1080 = new Vector2(1920f, 1080f);

    [Test]
    public void GetBottomLeft_RoomBelow_UnderTheTargetAlignedOnItsLeftEdge()
    {
        // Target from (80, 880) to (120, 920): the top of the panel 8 under its bottom
        Assert.AreEqual(new Vector2(80f, 772f), ToolTip.GetBottomLeft(new Rect(80f, 880f, 40f, 40f), new Vector2(200f, 100f), Screen1080, 8f));
    }

    [Test]
    public void GetBottomLeft_NoRoomBelow_AboveTheTarget()
    {
        // An item of a unit at the bottom of the screen
        Assert.AreEqual(new Vector2(80f, 58f), ToolTip.GetBottomLeft(new Rect(80f, 10f, 40f, 40f), new Vector2(200f, 100f), Screen1080, 8f));
    }

    [Test]
    public void GetBottomLeft_PastTheRightOfTheScreen_AlignedOnTheRightEdgeOfTheTarget()
    {
        Assert.AreEqual(new Vector2(1690f, 392f), ToolTip.GetBottomLeft(new Rect(1850f, 500f, 40f, 40f), new Vector2(200f, 100f), Screen1080, 8f));
    }

    [Test]
    public void GetBottomLeft_WiderThanTheScreen_StartsAtItsLeft()
    {
        Assert.AreEqual(0f, ToolTip.GetBottomLeft(new Rect(100f, 500f, 40f, 40f), new Vector2(2000f, 100f), Screen1080, 8f).x);
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

    [Test]
    public void FitSize_ShortText_KeepsItsWidthPlusThePadding()
    {
        Assert.AreEqual(new Vector2(124f, 54f), ToolTip.FitSize(new Vector2(100f, 30f), 476f, 12f));
    }

    [Test]
    public void FitSize_TextWiderThanTheMax_IsCappedToTheMaxWidth()
    {
        Assert.AreEqual(new Vector2(500f, 84f), ToolTip.FitSize(new Vector2(800f, 60f), 476f, 12f));
    }

    // The text stretched over the panel, the padding on each side, like in the prefab
    void AddText()
    {
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(_panel, false);
        TMPro.TextMeshProUGUI text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        text.fontSize = 20f;
        text.textWrappingMode = TMPro.TextWrappingModes.Normal;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = new Vector2(-24f, -24f);
        TestHelpers.SetPrivateField(_toolTip, "_text", text);
    }

    [Test]
    public void SetText_ShortText_ShrinksThePanelAroundIt()
    {
        _toolTip.Show(true);

        _toolTip.SetText("Heal");

        Assert.Less(_panel.sizeDelta.x, 500f);
        Assert.Less(_panel.sizeDelta.y, 300f);
        Assert.Greater(_panel.sizeDelta.x, 24f);
        Assert.Greater(_panel.sizeDelta.y, 24f);
    }

    [Test]
    public void SetText_LongText_WrapsWithinTheWidthOfThePrefabAndGrowsInHeight()
    {
        _toolTip.Show(true);
        _toolTip.SetText("Heal");
        float oneLineHeight = _panel.sizeDelta.y;

        _toolTip.SetText(string.Join(" ", System.Linq.Enumerable.Repeat("Restores health to every ally around the target", 4)));

        // As wide as its longest wrapped line, never wider than the prefab
        Assert.LessOrEqual(_panel.sizeDelta.x, 500f);
        Assert.Greater(_panel.sizeDelta.x, 300f);
        Assert.Greater(_panel.sizeDelta.y, oneLineHeight * 2f);
    }

    // As a player item does: the text set while hidden, the panel fitted once shown
    [Test]
    public void ShowBelow_AfterSetTextWhileHidden_FitsThePanelToTheText()
    {
        _toolTip.SetText("Heal");
        _toolTip.ShowBelow(_target);

        Assert.Less(_panel.sizeDelta.x, 500f);
        Assert.Less(_panel.sizeDelta.y, 300f);
    }
}

}

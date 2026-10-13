using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// The Devotion in the game HUD: an icon lit while the character holds some, and its count over its max
public class DevotionViewTests
{
    GameObject _go;
    DevotionView _view;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Devotion", typeof(RectTransform));
        Image icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
        icon.transform.SetParent(_go.transform, false);
        TMP_Text text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(_go.transform, false);
        _view = _go.AddComponent<DevotionView>();
        TestHelpers.SetPrivateField(_view, "_icon", icon);
        TestHelpers.SetPrivateField(_view, "_text", text);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void SetDevotion_ShowsTheCountOverTheMax_Lit()
    {
        _view.SetDevotion(2, 5);

        Assert.IsTrue(_go.activeSelf);
        Assert.AreEqual("2/5", _view.text);
        Assert.IsTrue(_view.isLit);
    }

    [Test]
    public void SetDevotion_Empty_IsUnlit()
    {
        _view.SetDevotion(0, 5);

        Assert.AreEqual("0/5", _view.text);
        Assert.IsFalse(_view.isLit);
    }

    [Test]
    public void SetDevotion_NoMax_IsHidden()
    {
        _view.SetDevotion(0, 0);

        Assert.IsFalse(_go.activeSelf);
    }
}

}

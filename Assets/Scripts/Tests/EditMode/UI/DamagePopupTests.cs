using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace UI
{

public class DamagePopupTests
{
    GameObject _popupGo;
    DamagePopup _popup;

    [SetUp]
    public void SetUp()
    {
        _popupGo = new GameObject("Damage Popup");
        _popup = _popupGo.AddComponent<DamagePopup>();
        TestHelpers.SetPrivateField(_popup, "_text", _popupGo.AddComponent<TextMeshPro>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_popupGo);
    }

    // The unit that sent the value may be gone, e.g. killed since
    [Test]
    public void Init_WithoutSource_ShowsTheValueInPlace()
    {
        _popup.Init(null, -10f);

        Assert.AreEqual("-10", _popupGo.GetComponent<TMP_Text>().text);
        Assert.AreEqual(0f, TestHelpers.GetPrivateField<float>(_popup, "_speed"));
    }
}

}

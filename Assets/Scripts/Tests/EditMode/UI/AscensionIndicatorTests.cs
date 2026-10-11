using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace UI
{

// "Ascension N" in the game HUD: shown only when the run has maluses, with the maluses in its tooltip
public class AscensionIndicatorTests
{
    GameObject _go;
    AscensionIndicator _indicator;
    AscensionSettings _ascension;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("AscensionIndicator", typeof(RectTransform));
        GameObject label = new GameObject("Text", typeof(RectTransform));
        label.transform.SetParent(_go.transform, false);
        _indicator = _go.AddComponent<AscensionIndicator>();
        TestHelpers.SetPrivateField(_indicator, "_label", label.AddComponent<TextMeshProUGUI>());

        _ascension = ScriptableObject.CreateInstance<AscensionSettings>();
        for (int i = 1; i <= 3; i++)
        {
            _ascension.levels.Add(new AscensionLevel { title = "Malus " + i, description = "Harder " + i });
        }
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_ascension);
    }

    [Test]
    public void Show_ALevel_ShowsItAndItsMaluses()
    {
        _indicator.Show(_ascension, 2);

        Assert.IsTrue(_go.activeSelf);
        Assert.AreEqual("Ascension 2", _indicator.label);
        Assert.AreEqual(_ascension.GetTooltip(2), _indicator.tooltipText);
    }

    [Test]
    public void Show_LevelZero_IsHidden()
    {
        _indicator.Show(_ascension, 0);

        Assert.IsFalse(_go.activeSelf);
    }

    [Test]
    public void Show_WithoutAscension_IsHidden()
    {
        _indicator.Show(null, 2);

        Assert.IsFalse(_go.activeSelf);
    }

    [Test]
    public void Show_AboveTheHighestLevel_ShowsTheHighestLevel()
    {
        _indicator.Show(_ascension, 9);

        Assert.AreEqual("Ascension 3", _indicator.label);
    }
}

}

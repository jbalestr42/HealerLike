using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// An item with a style (e.g. cursed, ascension) stands out among the items with the outline and the label of
// its ItemStyle (found by ItemStyleSet, see ItemStyleSetTests)
public class PlayerItemIconTests
{
    class StubItem : AItem
    {
        public override void Equip(GameObject target) { }
        public override void Unequip(GameObject target) { }
        public override string title => "Stub";
        public override string description => "";
        public override Sprite icon => null;
        public override List<GameplayTag> tags => new List<GameplayTag>();
    }

    static readonly Color CursedColor = new Color(0.69f, 0.49f, 1f, 1f);
    static readonly Color AscensionColor = new Color(0.91f, 0.27f, 0.27f, 1f);

    readonly List<Object> _assets = new List<Object>();
    GameObject _go;
    Image _image;
    ItemStyle _cursedStyle;
    ItemStyle _ascensionStyle;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Icon");
        _image = _go.AddComponent<Image>();

        _cursedStyle = CreateStyle(CursedColor, "");
        _ascensionStyle = CreateStyle(AscensionColor, "Ascension");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        foreach (Object asset in _assets)
        {
            Object.DestroyImmediate(asset);
        }
        _assets.Clear();
    }

    ItemStyle CreateStyle(Color color, string label)
    {
        ItemStyle style = ScriptableObject.CreateInstance<ItemStyle>();
        style.outlineColor = color;
        style.label = label;
        _assets.Add(style);
        return style;
    }

    UnityEngine.UI.Outline GetOutline()
    {
        UnityEngine.UI.Outline outline = _image.GetComponent<UnityEngine.UI.Outline>();
        return outline != null && outline.enabled ? outline : null;
    }

    [Test]
    public void ApplyOutline_CursedStyle_HasItsColor()
    {
        PlayerItemIcon.ApplyOutline(_image, _cursedStyle);

        Assert.IsNotNull(GetOutline());
        Assert.AreEqual(CursedColor, GetOutline().effectColor);
    }

    [Test]
    public void ApplyOutline_AscensionStyle_HasItsColor()
    {
        PlayerItemIcon.ApplyOutline(_image, _ascensionStyle);

        Assert.IsNotNull(GetOutline());
        Assert.AreEqual(AscensionColor, GetOutline().effectColor);
    }

    [Test]
    public void ApplyOutline_NoStyle_NoOutline()
    {
        PlayerItemIcon.ApplyOutline(_image, null);

        Assert.IsNull(GetOutline());
    }

    [Test]
    public void ApplyOutline_NoStyleAfterAStyle_RemovesTheOutline()
    {
        PlayerItemIcon.ApplyOutline(_image, _cursedStyle);

        PlayerItemIcon.ApplyOutline(_image, null);

        Assert.IsNull(GetOutline());
    }

    [Test]
    public void GetStyle_OutOfPlayMode_IsNull()
    {
        Assert.IsNull(PlayerItemIcon.GetStyle(new StubItem()));
    }

    [Test]
    public void GetToolTipText_StyleWithALabel_ShowsItUnderTheName()
    {
        string text = PlayerItemIcon.GetToolTipText(new StubItem(), _ascensionStyle);

        Assert.AreEqual($"<b>Stub</b>\n<color=#{ColorUtility.ToHtmlStringRGB(AscensionColor)}>Ascension</color>", text);
    }

    [Test]
    public void GetToolTipText_StyleWithoutLabel_ShowsTheNameOnly()
    {
        Assert.AreEqual("<b>Stub</b>", PlayerItemIcon.GetToolTipText(new StubItem(), _cursedStyle));
    }
}

}

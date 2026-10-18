using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// The icons above a unit: here the hits it still blocks (Hit Armor), e.g. the guard of the Armored Footman
public class BuffIconBarTests
{
    GameObject _barGo;
    BuffIconBar _bar;
    BuffIconView _iconPrefab;

    [SetUp]
    public void SetUp()
    {
        _iconPrefab = CreateIconPrefab();
        _barGo = new GameObject("BuffIconBar", typeof(RectTransform));
        _bar = _barGo.AddComponent<BuffIconBar>();
        TestHelpers.SetPrivateField(_bar, "_iconPrefab", _iconPrefab);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_barGo);
        Object.DestroyImmediate(_iconPrefab.gameObject);
    }

    // An icon with its image and its stack counter, like the prefab
    public static BuffIconView CreateIconPrefab()
    {
        GameObject go = new GameObject("BuffIcon", typeof(RectTransform));
        BuffIconView view = go.AddComponent<BuffIconView>();
        Image image = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(go.transform, false);
        TMP_Text stacks = new GameObject("Stacks", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        stacks.transform.SetParent(go.transform, false);
        TestHelpers.SetPrivateField(view, "_icon", image);
        TestHelpers.SetPrivateField(view, "_stacks", stacks);
        Image timer = new GameObject("Timer", typeof(RectTransform)).AddComponent<Image>();
        timer.transform.SetParent(go.transform, false);
        timer.type = Image.Type.Filled;
        TestHelpers.SetPrivateField(view, "_timer", timer);
        return view;
    }

    [Test]
    public void SetHitArmor_None_NoIcon()
    {
        _bar.SetHitArmor(0);

        Assert.IsNull(_bar.hitArmorView);
        Assert.AreEqual(0, _barGo.transform.childCount);
    }

    [Test]
    public void SetHitArmor_SomeHits_AnIconWithTheirCount()
    {
        _bar.SetHitArmor(2);

        Assert.IsNotNull(_bar.hitArmorView);
        Assert.AreEqual("2", _bar.hitArmorView.stacksText);
    }

    // Unlike the stacks of a buff, a single hit blocked is still counted
    [Test]
    public void SetHitArmor_OneHit_StillCounted()
    {
        _bar.SetHitArmor(1);

        Assert.AreEqual("1", _bar.hitArmorView.stacksText);
    }

    [Test]
    public void SetHitArmor_HitBlocked_TheCountGoesDown()
    {
        _bar.SetHitArmor(2);

        _bar.SetHitArmor(1);

        Assert.AreEqual("1", _bar.hitArmorView.stacksText);
        Assert.AreEqual(1, _barGo.transform.childCount);
    }

    [Test]
    public void SetHitArmor_LastHitBlocked_TheIconGoes()
    {
        _bar.SetHitArmor(1);

        _bar.SetHitArmor(0);

        Assert.IsNull(_bar.hitArmorView);
        Assert.AreEqual(0, _barGo.transform.childCount);
    }

    [Test]
    public void TrackHitArmor_FollowsTheAttribute()
    {
        Attribute hitArmor = new Attribute(2f);
        _bar.TrackHitArmor(hitArmor);
        Assert.AreEqual("2", _bar.hitArmorView.stacksText);

        // A hit blocked, as the resolver of the health does
        hitArmor.BaseValue -= 1f;
        hitArmor.Update();

        Assert.AreEqual("1", _bar.hitArmorView.stacksText);
    }

    // Without it, the units blocking hits would show an empty icon
    [Test]
    public void EntityHUDPrefab_HasTheHitArmorIcon()
    {
        GameObject hud = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EntityHUD.prefab");
        BuffIconBar bar = hud.GetComponentInChildren<BuffIconBar>(true);

        Assert.IsNotNull(TestHelpers.GetPrivateField<Sprite>(bar, "_hitArmorIcon"));
    }

    [Test]
    public void BuffIconView_SingleStack_NoCounterByDefault()
    {
        BuffIconView view = Object.Instantiate(_iconPrefab);
        try
        {
            view.SetStacks(1);

            Assert.AreEqual("", view.stacksText);
        }
        finally
        {
            Object.DestroyImmediate(view.gameObject);
        }
    }

    [Test]
    public void BuffIconPrefab_HasItsTimer()
    {
        GameObject icon = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/BuffIcon.prefab");

        Assert.IsNotNull(TestHelpers.GetPrivateField<Image>(icon.GetComponent<BuffIconView>(), "_timer"));
    }

    [Test]
    public void BuffIconView_SetRemaining_DarkensThePartOfTheDurationGone()
    {
        BuffIconView view = Object.Instantiate(_iconPrefab);
        try
        {
            view.SetRemaining(4f, 6f);

            Assert.AreEqual(1f / 3f, view.timerFill, 0.001f);
            Assert.AreEqual(1f, view.iconAlpha, 0.001f, "Not ending yet");
        }
        finally
        {
            Object.DestroyImmediate(view.gameObject);
        }
    }

    [Test]
    public void BuffIconView_SetRemaining_WithoutDuration_NeitherDarkenedNorBlinking()
    {
        BuffIconView view = Object.Instantiate(_iconPrefab);
        try
        {
            view.SetRemaining(0f, 0f);

            Assert.AreEqual(0f, view.timerFill);
            Assert.AreEqual(1f, view.iconAlpha, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(view.gameObject);
        }
    }

    [TestCase(6f, 6f, 0f)]
    [TestCase(3f, 6f, 0.5f)]
    [TestCase(0f, 6f, 1f)]
    [TestCase(1f, 0f, 0f)]
    public void BuffIconView_GetElapsedShare(float remaining, float duration, float expected)
    {
        Assert.AreEqual(expected, BuffIconView.GetElapsedShare(remaining, duration), 0.001f);
    }

    // Its last second
    [TestCase(0.5f, 6f, true)]
    [TestCase(1f, 6f, true)]
    [TestCase(1.5f, 6f, false)]
    [TestCase(0f, 0f, false)]
    public void BuffIconView_IsEnding(float remaining, float duration, bool expected)
    {
        Assert.AreEqual(expected, BuffIconView.IsEnding(remaining, duration));
    }

    [Test]
    public void BuffIconView_GetBlinkAlpha_BetweenFaintAndOpaque()
    {
        float min = 1f;
        float max = 0f;
        for (float time = 0f; time < 1f; time += 0.01f)
        {
            float alpha = BuffIconView.GetBlinkAlpha(time);
            min = Mathf.Min(min, alpha);
            max = Mathf.Max(max, alpha);
        }

        Assert.Less(min, 0.5f);
        Assert.AreEqual(1f, max, 0.05f);
    }
}

}

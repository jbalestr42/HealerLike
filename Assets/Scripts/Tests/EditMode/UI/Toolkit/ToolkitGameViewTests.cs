using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Toolkit
{

public class ToolkitGameViewTests
{
    VisualElement _root;
    VisualElement _list;
    ToolkitGameView _view;

    static ToolkitCardModel CreateModel(string title, bool isEnabled = true)
    {
        ToolkitCardModel model = new ToolkitCardModel();
        model.title = title;
        model.isEnabled = isEnabled;
        return model;
    }

    [SetUp]
    public void SetUp()
    {
        _root = new VisualElement();
        _list = new VisualElement();
        _list.name = "cards";
        _root.Add(_list);
        _view = new ToolkitGameView(_root);
    }

    [TearDown]
    public void TearDown()
    {
        _view.Release();
    }

    [Test]
    public void SetCards_TwoModels_AddsTwoCards()
    {
        _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("Heal"), CreateModel("Guard") });

        Assert.AreEqual(2, _list.childCount);
    }

    [Test]
    public void SetCards_FewerModels_ReusesFirstCardAndRemovesStale()
    {
        _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("Heal"), CreateModel("Guard") });
        VisualElement first = _list[0];

        _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("New spell", isEnabled: false) });

        Assert.AreEqual(1, _list.childCount);
        Assert.AreSame(first, _list[0]);
        Assert.IsFalse(first.enabledSelf);
        Assert.AreEqual("New spell", first.Q<Label>(className: "data-card__title").text);
    }

    [Test]
    public void SetCards_NoIconSource_ShowsFallbackIcon()
    {
        _view.SetCards("cards", new ToolkitCardModel[] { CreateModel("Unknown data") });

        Assert.IsNotNull(_list[0].Q(className: "data-card__icon").style.backgroundImage.value.texture);
    }

    [Test]
    public void Show_HiddenThenShown_RemovesHiddenClass()
    {
        VisualElement panel = new VisualElement();
        panel.name = "inventory-panel";
        _root.Add(panel);

        _view.Show("inventory-panel", false);
        bool wasHidden = panel.ClassListContains("is-hidden");
        _view.Show("inventory-panel", true);

        Assert.IsTrue(wasHidden);
        Assert.IsFalse(panel.ClassListContains("is-hidden"));
    }

    [Test]
    public void SetResource_HalfOfMaximum_SetsFiftyPercent()
    {
        ProgressBar bar = new ProgressBar();
        bar.name = "mana-bar";
        _root.Add(bar);

        _view.SetResource("mana-bar", 30f, 60f);

        Assert.AreEqual(50f, bar.value);
        Assert.AreEqual("30 / 60", bar.title);
    }

    [Test]
    public void ShowDetail_Model_SetsTitleAndDescription()
    {
        Label title = new Label();
        title.name = "detail-title";
        Label description = new Label();
        description.name = "detail-description";
        _root.Add(title);
        _root.Add(description);
        ToolkitCardModel model = CreateModel("Guard");
        model.description = "Protects an ally";

        _view.ShowDetail(model);

        Assert.AreEqual("Guard", title.text);
        Assert.AreEqual("Protects an ally", description.text);
    }

    [Test]
    public void Load_GameUIAndDataCard_ResolveFromResources()
    {
        VisualTreeAsset layout = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI");
        VisualTreeAsset card = Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard");

        TemplateContainer cardTree = card.CloneTree();

        Assert.IsNotNull(layout);
        Assert.IsNotNull(cardTree.Q<Button>("data-card"));
        Assert.IsNotNull(cardTree.Q("card-icon"));
    }
}
}

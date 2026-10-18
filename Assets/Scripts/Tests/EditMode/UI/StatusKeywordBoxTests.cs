using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{

// The box explaining the statuses a text cites, next to that text
public class StatusKeywordBoxTests
{
    static readonly Vector2 Screen1080 = new Vector2(1920f, 1080f);

    readonly List<Object> _objects = new List<Object>();
    IReadOnlyList<StatusData> _registeredStatuses;
    GameObject _canvasGo;
    RectTransform _element;

    [SetUp]
    public void SetUp()
    {
        _registeredStatuses = StatusKeywords.statuses;
        StatusData vulnerable = ScriptableObject.CreateInstance<StatusData>();
        vulnerable.title = "Vulnerable";
        vulnerable.description = "Takes 15% more damage per stack";
        _objects.Add(vulnerable);
        StatusKeywords.Register(new List<StatusData> { vulnerable });

        _canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        _objects.Add(_canvasGo);
        _element = (RectTransform)new GameObject("Element", typeof(RectTransform)).transform;
        _element.SetParent(_canvasGo.transform, false);
        _element.sizeDelta = new Vector2(100f, 50f);
    }

    [TearDown]
    public void TearDown()
    {
        StatusKeywords.Register(_registeredStatuses);
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    [Test]
    public void Create_Hidden()
    {
        StatusKeywordBox box = StatusKeywordBox.Create(_canvasGo.transform);

        Assert.IsFalse(box.isShown);
    }

    [Test]
    public void Show_TextCitingAStatus_ShowsItsExplanation()
    {
        StatusKeywordBox box = StatusKeywordBox.Create(_canvasGo.transform);

        bool isShown = box.Show(StatusKeywords.Format("Makes the target {status:Vulnerable}"), _element);

        Assert.IsTrue(isShown);
        Assert.IsTrue(box.isShown);
        StringAssert.Contains("Takes 15% more damage per stack", box.text);
    }

    [Test]
    public void Show_TextCitingNoStatus_Hidden()
    {
        StatusKeywordBox box = StatusKeywordBox.Create(_canvasGo.transform);
        box.Show(StatusKeywords.Format("{status:Vulnerable}"), _element);

        bool isShown = box.Show("Heals 10 HP", _element);

        Assert.IsFalse(isShown);
        Assert.IsFalse(box.isShown);
    }

    [Test]
    public void Hide_HidesIt()
    {
        StatusKeywordBox box = StatusKeywordBox.Create(_canvasGo.transform);
        box.Show(StatusKeywords.Format("{status:Vulnerable}"), _element);

        box.Hide();

        Assert.IsFalse(box.isShown);
    }

    [Test]
    public void Get_TheSameBoxForEveryElementOfACanvas()
    {
        RectTransform other = (RectTransform)new GameObject("Other", typeof(RectTransform)).transform;
        other.SetParent(_element, false);

        StatusKeywordBox box = StatusKeywordBox.Get(_element);

        Assert.AreSame(box, StatusKeywordBox.Get(other));
        Assert.AreSame(_canvasGo.transform, box.transform.parent);
    }

    [Test]
    public void AddHover_HoveredThenLeft_ShowsThenHidesTheBox()
    {
        StatusKeywordBox.AddHover(_element.gameObject, () => StatusKeywords.Format("{status:Vulnerable}"));
        HoverTooltipTrigger trigger = _element.GetComponent<HoverTooltipTrigger>();
        StatusKeywordBox box = StatusKeywordBox.Get(_element);

        trigger.OnPointerEnter(new PointerEventData(null));
        Assert.IsTrue(box.isShown);

        trigger.OnPointerExit(new PointerEventData(null));
        Assert.IsFalse(box.isShown);
    }

    [Test]
    public void GetBottomLeft_RoomOnTheRight_RightOfTheElementTopAligned()
    {
        // Element from (100, 500) to (200, 550): the box 8 on its right, its top on the top of the element
        Assert.AreEqual(new Vector2(208f, 450f), StatusKeywordBox.GetBottomLeft(new Rect(100f, 500f, 100f, 50f), new Vector2(300f, 100f), Screen1080, 8f));
    }

    [Test]
    public void GetBottomLeft_NoRoomOnTheRight_LeftOfTheElement()
    {
        // A panel on the right edge of the screen (e.g. the unit panel)
        Assert.AreEqual(new Vector2(1172f, 450f), StatusKeywordBox.GetBottomLeft(new Rect(1480f, 500f, 440f, 50f), new Vector2(300f, 100f), Screen1080, 8f));
    }

    [Test]
    public void GetBottomLeft_PastTheBottomOfTheScreen_MovedUpOnIt()
    {
        Assert.AreEqual(0f, StatusKeywordBox.GetBottomLeft(new Rect(100f, 0f, 100f, 50f), new Vector2(300f, 200f), Screen1080, 8f).y);
    }

    [Test]
    public void GetBottomLeft_PastTheTopOfTheScreen_MovedDownOnIt()
    {
        // Taller than the room above its bottom left corner: its top stays on the top of the screen
        Assert.AreEqual(880f, StatusKeywordBox.GetBottomLeft(new Rect(100f, 1000f, 100f, 200f), new Vector2(300f, 200f), Screen1080, 8f).y);
    }
}

}

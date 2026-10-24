using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitRosterDragTests
    {
        ToolkitTouchFixture _fixture;
        GameObject _host, _model;
        EntityData _data;
        Board _board;
        BoardLayout _layout;
        [SetUp]
        public void Setup()
        {
            _fixture = new ToolkitTouchFixture(); _fixture.Init();
            _host = new GameObject("Roster placement test");
            _model = new GameObject("Preview source");
            _data = ScriptableObject.CreateInstance<EntityData>(); _data.model = _model;
            _board = _host.AddComponent<Board>();
            _board.transform.position = _fixture.hit.point;
            _layout = ScriptableObject.CreateInstance<BoardLayout>();
            _layout.Resize(3, 3);
            _layout.cellSize = 1;
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    _layout.SetCell(x, y, BoardCellType.Player);
                }
            }
            _board.SetLayout(_layout);
        }
        [TearDown]
        public void Teardown()
        {
            _fixture.Dispose(); Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_model); Object.DestroyImmediate(_data); Object.DestroyImmediate(_layout);
        }
        [Test]
        public void ValidReleaseDispatchesExistingPlacementExactlyOnceAndClearsPreview()
        {
            _fixture.WithInput((input, manager, original) =>
            {
                Placement placement = new Placement(_data);
                using (ToolkitRosterDrag drag = new ToolkitRosterDrag(manager, _board,
                    (Vector2 p, out RaycastHit h, int mask) => { h = _fixture.hit; return true; },
                    p => false, () => {}, (data, spawned) => placement))
                {
                    Assert.That(drag.Begin(_data, Vector2.one, null), Is.True);
                    Assert.That(placement.clicks, Is.Zero);
                    Assert.That(drag.End(Vector2.one), Is.True);
                    Assert.That(drag.End(Vector2.one), Is.False);
                    Assert.That(placement.clicks, Is.EqualTo(1));
                    Assert.That(drag.active, Is.False);
                    Assert.That(manager.GetInteraction(), Is.Null);
                }
            });
        }
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void InvalidUiReturnOrCancellationNeverDispatches(bool overUi, bool cancel)
        {
            _fixture.WithInput((input, manager, original) =>
            {
                Placement placement = new Placement(_data);
                using (ToolkitRosterDrag drag = new ToolkitRosterDrag(manager, _board,
                    (Vector2 p, out RaycastHit h, int mask) => { h = _fixture.hit; return overUi || cancel; },
                    p => overUi, () => {}, (data, spawned) => placement))
                {
                    drag.Begin(_data, Vector2.one, null);
                    if (cancel)
                    {
                        drag.Cancel();
                    }

                    Assert.That(drag.End(Vector2.one), Is.False);
                    Assert.That(placement.clicks, Is.Zero);
                    Assert.That(drag.active, Is.False);
                    Assert.That(manager.GetInteraction(), Is.Null);
                }
            });
        }
        sealed class Placement : EntityGridInteraction
        {
            public int clicks;
            GameObject _preview;
            public Placement(EntityData data) : base(data)
            { _preview = ToolkitTouchFixture.PreviewOf(this); }
            public override void Cancel() { Object.DestroyImmediate(_preview); }
            public override void End() { Object.DestroyImmediate(_preview); }
            public override void OnMouseOver(RaycastHit hit) { }
            public override void OnMouseClick(RaycastHit hit) { clicks++; }
        }
    }
}

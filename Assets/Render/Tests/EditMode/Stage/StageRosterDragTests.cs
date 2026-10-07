using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageRosterDragTests
    {
        StageTouchFixture _fixture;
        GameObject _host, _ground, _model;
        EntityData _data;
        GridManager _grid;
        [SetUp]
        public void Setup()
        {
            _fixture = new StageTouchFixture(); _fixture.Init();
            _host = new GameObject("Roster placement test");
            _ground = new GameObject("Ground");
            _model = new GameObject("Preview source");
            _data = ScriptableObject.CreateInstance<EntityData>(); _data.model = _model;
            _grid = _host.AddComponent<GridManager>();
            _grid.transform.position = _fixture.hit.point;
            _grid.width = _grid.height = 3; _grid.size = 1;
            TestHelpers.SetPrivateField(_grid, "_ground", _ground); _grid.Generate();
        }
        [TearDown]
        public void Teardown()
        {
            _fixture.Dispose(); Object.DestroyImmediate(_host); Object.DestroyImmediate(_ground);
            Object.DestroyImmediate(_model); Object.DestroyImmediate(_data);
        }
        [Test]
        public void ValidReleaseDispatchesExistingPlacementExactlyOnceAndClearsPreview()
        {
            _fixture.WithInput((input, manager, original) =>
            {
                Placement placement = new Placement(_data);
                using (StageRosterDrag drag = new StageRosterDrag(manager, _grid,
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
                using (StageRosterDrag drag = new StageRosterDrag(manager, _grid,
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
            { EntityPlacementReadout.TryRead(this, out _, out _preview, out _); }
            public override void Cancel() { Object.DestroyImmediate(_preview); }
            public override void End() { Object.DestroyImmediate(_preview); }
            public override void OnMouseOver(RaycastHit hit) { }
            public override void OnMouseClick(RaycastHit hit) { clicks++; }
        }
    }
}

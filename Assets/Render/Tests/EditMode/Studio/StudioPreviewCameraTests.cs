using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{

// A subject that always fills the same box and counts how often it was asked
public class FixedSubject : IPreviewSubject
{
    public Bounds bounds = new Bounds(Vector3.up, Vector3.one);
    public int askedCount;

    public Bounds GetSubjectBounds()
    {
        askedCount++;
        return bounds;
    }
}

public class StudioPreviewCameraTests
{
    GameObject _cameraGo;
    Camera _camera;
    StudioPreviewCamera _controller;
    FixedSubject _subject;

    [SetUp]
    public void SetUp()
    {
        _cameraGo = new GameObject("Studio preview camera", typeof(Camera));
        _camera = _cameraGo.GetComponent<Camera>();
        _controller = new StudioPreviewCamera();
        _controller.Init(new Vector2(28f, 32f), 0.5f, 25f, 1.12f, "StudioPreviewCameraTests");
        _subject = new FixedSubject();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_cameraGo);
    }

    [Test]
    public void Apply_FirstView_FitsTheSubjectOnce()
    {
        _controller.Apply(_camera, 600f, 400f, true, _subject);
        Vector3 toCentre = _subject.bounds.center - _camera.transform.position;

        _controller.Apply(_camera, 600f, 400f, true, _subject);

        Assert.AreEqual(1, _subject.askedCount);
        Assert.Less(Vector3.Angle(_camera.transform.forward, toCentre), 0.01f); // looking at the subject
        Assert.AreEqual(34f, _camera.fieldOfView);
    }

    [Test]
    public void Apply_NewAspect_RefitsUnlessTheFramingIsKept()
    {
        _controller.Apply(_camera, 600f, 400f, true, _subject);

        _controller.Apply(_camera, 300f, 600f, false, _subject);
        Assert.AreEqual(1, _subject.askedCount);

        _controller.Apply(_camera, 300f, 600f, true, _subject);
        Assert.AreEqual(1, _subject.askedCount); // the aspect was already taken by the kept framing
    }

    [Test]
    public void EndCapture_AfterAnotherSize_PutsBackCameraAndFraming()
    {
        _controller.Apply(_camera, 600f, 600f, true, _subject);
        _camera.clearFlags = CameraClearFlags.Depth;
        _camera.backgroundColor = new Color(0.7f, 0.2f, 0.5f, 0.8f);
        _camera.allowHDR = false;
        Vector3 position = _camera.transform.position;
        Quaternion rotation = _camera.transform.rotation;

        _controller.BeginCapture(_camera);
        _controller.Apply(_camera, 640f, 360f, false, _subject);
        _controller.EndCapture(_camera);

        Assert.AreEqual(position, _camera.transform.position);
        Assert.AreEqual(rotation, _camera.transform.rotation);
        Assert.AreEqual(1f, _camera.aspect);
        Assert.AreEqual(CameraClearFlags.Depth, _camera.clearFlags);
        Assert.AreEqual(new Color(0.7f, 0.2f, 0.5f, 0.8f), _camera.backgroundColor);
        Assert.IsFalse(_camera.allowHDR);
    }

    [Test]
    public void EndCapture_FitPendingBeforeTheCapture_StillFitsAfter()
    {
        _controller.Apply(_camera, 600f, 600f, true, _subject);
        _controller.Refit();

        _controller.BeginCapture(_camera);
        _controller.Apply(_camera, 640f, 360f, false, _subject);
        _controller.EndCapture(_camera);
        _controller.Apply(_camera, 600f, 600f, true, _subject);

        Assert.AreEqual(3, _subject.askedCount); // the first view, the capture, the view after it
    }

    [Test]
    public void Reset_AfterAView_FitsAgainFromTheDefaultOrbit()
    {
        _controller.Apply(_camera, 600f, 600f, true, _subject);
        Quaternion rotation = _camera.transform.rotation;

        _controller.Reset();
        _controller.Apply(_camera, 600f, 600f, true, _subject);

        Assert.AreEqual(2, _subject.askedCount);
        Assert.Less(Quaternion.Angle(Quaternion.Euler(28f, 32f, 0f), rotation), 0.01f);
    }
}

}

using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{
    public class ToolkitCameraInputProviderTests
    {
        GameObject _camera;
        FixtureProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _camera = new GameObject("Board camera");
            _provider = _camera.AddComponent<FixtureProvider>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_camera);
        }

        [Test]
        public void GetAxisValue_PointerOverInterface_LeavesTheCameraStill()
        {
            _provider.overInterface = true;
            for (int axis = 0; axis < 3; axis++)
            {
                Assert.That(_provider.GetAxisValue(axis), Is.EqualTo(0f), "Axis " + axis);
            }
        }

        [Test]
        public void GetAxisValue_PointerOverTheWorld_ReadsTheWheel()
        {
            _provider.overInterface = false;
            Assert.That(_provider.GetAxisValue(1), Is.EqualTo(-2f));
        }

        public class FixtureProvider : ToolkitCameraInputProvider
        {
            public bool overInterface;

            protected override float ReadAxis(int axis)
            {
                return -2f;
            }

            protected override bool IsPointerOverInterface()
            {
                return overInterface;
            }
        }
    }
}

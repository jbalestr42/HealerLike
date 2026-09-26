using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    public class StageGroundTests
    {
        [Test]
        public void Clear_RevokesOldSceneEffectsAndPointerBeforeReattachment()
        {
            GameObject owner = new GameObject("Ground service owner");
            GameObject cameraObject = new GameObject("Ground camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(Vector3.up * 10f, Quaternion.Euler(90f, 0f, 0f));
            camera.pixelRect = new Rect(0f, 0f, 200f, 200f);
            StageGround service = new StageGround();
            try
            {
                service.Init(owner, null, camera, 0f, 1f);
                Ground old = service.ground;
                GroundVocabulary owned = old.vocabulary;
                GroundHandle handle = old.Hold(owned.boost);
                handle.Show(Vector3.zero, 1f, 1f);
                PointerBrush brush = owner.GetComponent<PointerBrush>();
                brush.Point(true, true, new Vector2(100f, 100f));
                Assert.IsTrue(brush.isBrushing);

                service.Clear();
                service.Clear();
                Assert.IsNull(service.ground);
                Assert.IsFalse(owned);
                Assert.IsTrue(handle.isReleased);
                Assert.AreEqual(0, old.bodyCount);
                Assert.AreEqual(0, old.heldCount);
                brush.Point(true, true, new Vector2(100f, 100f));
                Assert.IsFalse(brush.isBrushing);
                Assert.AreEqual(0, brush.AppendCapsules(new BodyCapsule[1], 0));

                service.Init(owner, null, camera, 0f, 1f);
                Assert.AreNotSame(old, service.ground);
                Assert.AreSame(brush, owner.GetComponent<PointerBrush>());
                Assert.AreEqual(1, service.ground.bodyCount);
                Assert.IsFalse(brush.isBrushing);
            }
            finally
            {
                service.Clear();
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}

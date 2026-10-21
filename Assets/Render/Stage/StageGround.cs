using HealerLike.Render.Grass;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The scene attachment owns ground effects and the persistent manager's pointer registration together.
    public class StageGround
    {
        PointerBrush _brush;
        public Ground ground { get; private set; }

        public void Init(GameObject owner, GroundVocabulary vocabulary, Camera camera, float height, float cellSize)
        {
            Clear();
            ground = new Ground(vocabulary);
            _brush = owner.GetComponent<PointerBrush>();
            if (_brush == null)
            {
                _brush = owner.AddComponent<PointerBrush>();
            }
            _brush.Init(ground, camera, height, cellSize);
        }

        public void Clear()
        {
            if (_brush != null)
            {
                _brush.Clear();
            }
            _brush = null;
            ground?.Dispose();
            ground = null;
        }
    }
}

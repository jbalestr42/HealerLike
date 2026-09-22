using UnityEngine;
namespace HealerLike.Render.Stones
{
    public interface IHLStoneMotionSource
    {
        bool TrySample(out Vector3 velocityWS,out Quaternion facingWS);
    }
    public sealed class HLStoneMotionSampler
    {
        Vector3 previous; bool primed;
        public void Reset() { primed=false; }
        public Vector3 Sample(Vector3 position,float deltaTime,bool dragging,float cellSize=1)
        {
            Vector3 delta=position-previous; previous=position; delta.y=0;
            if(!primed || dragging || deltaTime<=0 || delta.magnitude>cellSize*.5f) { primed=true; return Vector3.zero; }
            Vector3 velocity=delta/deltaTime; return velocity.magnitude<.02f?Vector3.zero:velocity;
        }
    }
}

using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // Sits on a drawn arm's object so the observer of a projectile can colour the tip its delivery took
    public class LianaArmView : MonoBehaviour
    {
        LianaArm _arm;
        public LianaArm arm { get { return _arm; } set { _arm = value; } }
    }
}

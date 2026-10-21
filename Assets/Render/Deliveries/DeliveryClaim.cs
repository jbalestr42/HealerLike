using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    // The view that claimed a shot, reached through IDeliverySource and, when it can tint, IDeliveryAccent only
    public class DeliveryClaim
    {
        IDeliverySource _source;
        IDeliveryAccent _accent;
        MonoBehaviour _component;

        int _token;
        public int token { get { return _token; } }

        public bool isClaimed { get { return _token != 0; } }

        // The claiming view was destroyed or switched off
        public bool isLost { get { return _token != 0 && (!_component || !_component.isActiveAndEnabled); } }

        // The first enabled source under the shooter's model that accepts the shot
        public bool TryClaim(GameObject model, int token, DeliveryStyle style, Transform projectile, Vector3 end)
        {
            Release();
            if (!model || token == 0)
            {
                return false;
            }

            foreach (MonoBehaviour component in model.GetComponentsInChildren<MonoBehaviour>())
            {
                if (!(component is IDeliverySource candidate) || !component.isActiveAndEnabled)
                {
                    continue;
                }

                if (candidate.BeginDelivery(token, style, projectile, end))
                {
                    _source = candidate;
                    _accent = component as IDeliveryAccent;
                    _component = component;
                    _token = token;
                    return true;
                }
            }
            return false;
        }

        public void Contact(Vector3 point, GameObject target)
        {
            if (_token != 0)
            {
                _source.ContactDelivery(_token, point, target);
            }
        }

        public void Tint(Color colour)
        {
            if (_token != 0 && _accent != null)
            {
                _accent.SetDeliveryAccent(_token, colour);
            }
        }

        // Ends the delivery at a source that is still there, and forgets it
        public void Release()
        {
            if (_token != 0 && _component)
            {
                _source.EndDelivery(_token);
            }

            _source = null;
            _accent = null;
            _component = null;
            _token = 0;
        }
    }
}

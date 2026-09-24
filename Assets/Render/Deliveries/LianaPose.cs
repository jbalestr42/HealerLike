using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Deliveries
{
    // Stored by value in assets: append new members, never reorder or remove
    public enum GestureKind
    {
        Attack,
        Heal,
        Channel
    }

    public enum GesturePhase
    {
        Rest,
        Extend,
        Contact,
        Hold,
        Retract
    }

    // Where an arm's joints are: the gesture it is in, and the chain that follows its goal as a solved reach, a rod
    // straight to a shot or an arc lifted over it. Gestures change goals, never link lengths or gameplay.
    public class LianaPose
    {
        // Gesture timings in seconds: the reach out, the pause at contact and the way back
        static readonly float extendSeconds = 0.1f;
        static readonly float contactSeconds = 0.04f;
        static readonly float retractSeconds = 0.2f;
        // An arc delivery lifts its middle by this much per cell of reach, never more than the cap
        static readonly float arcLiftPerCell = 0.4f;
        static readonly float arcMaxLift = 2f;
        // The solver's iteration budget for a reaching chain
        static readonly int solveIterations = 64;

        readonly ChainSolver _solver = new ChainSolver();
        Vector3[] _rest;
        Vector3[] _joints;
        float[] _lengths;
        Vector3 _pole;
        float _elapsed;
        Vector3 _startGoal;
        Vector3 _returnGoal;
        bool _isEndPending;
        GestureKind _kind;

        int _token;
        public int token { get { return _token; } }

        Vector3 _goal;
        public Vector3 goal { get { return _goal; } }

        DeliveryStyle _style;
        public DeliveryStyle style { get { return _style; } set { _style = value; } }

        // A delivery draws a rod or an arc to its projectile instead of solving a reaching chain
        bool _isDeliveryProfile;
        public bool isDeliveryProfile { get { return _isDeliveryProfile; } set { _isDeliveryProfile = value; } }

        GesturePhase _phase;
        public GesturePhase phase { get { return _phase; } }

        public Vector3 tip { get { return _joints[_joints.Length - 1]; } }

        public int jointCount { get { return _joints.Length; } }

        public int segmentCount { get { return _lengths.Length; } }

        public bool isAvailable { get { return _phase == GesturePhase.Rest; } }

        public void Init(ArmDefinition definition, float cellSize)
        {
            _rest = new Vector3[definition.restJoints.Length];
            _joints = new Vector3[_rest.Length];
            _lengths = new float[definition.segmentCount];
            for (int i = 0; i < _rest.Length; i++)
            {
                _rest[i] = definition.restJoints[i] * cellSize;
            }

            for (int i = 0; i < _lengths.Length; i++)
            {
                _lengths[i] = definition.segmentLength * cellSize;
            }

            _pole = definition.bendPole;
        }

        public Vector3 Joint(int index)
        {
            return _joints[index];
        }

        public void Begin(int gestureToken, GestureKind gestureKind, Vector3 worldTarget)
        {
            _token = gestureToken;
            _kind = gestureKind;
            _goal = worldTarget;
            _startGoal = tip;
            _isEndPending = false;
            _phase = GesturePhase.Extend;
            _elapsed = 0f;
        }

        public void SetTipGoal(int gestureToken, Vector3 worldPosition)
        {
            if (_token != gestureToken || _phase == GesturePhase.Rest || _phase == GesturePhase.Retract)
            {
                return;
            }

            _goal = worldPosition;
            // Projectile travel already sets the extension timing
            if (_phase != GesturePhase.Contact || _style == DeliveryStyle.Bounce)
            {
                _phase = GesturePhase.Hold;
            }
        }

        public void Contact(int gestureToken, Vector3 worldPosition)
        {
            if (_token != gestureToken || _phase == GesturePhase.Rest)
            {
                return;
            }

            _goal = worldPosition;
            _phase = GesturePhase.Contact;
            _elapsed = 0f;
        }

        public void End(int gestureToken)
        {
            if (_token != gestureToken || _phase == GesturePhase.Rest || _phase == GesturePhase.Retract)
            {
                return;
            }

            if (_phase == GesturePhase.Contact && _elapsed < contactSeconds)
            {
                _isEndPending = true;
                return;
            }

            StartReturn();
        }

        // False when the chain has no finite pose this frame
        public bool Tick(float deltaTime, Vector3 rootWorld, Quaternion restOrientation, ArmStyle arm)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float retract = retractSeconds;
            if (_isDeliveryProfile && arm.retractSeconds > 0f)
            {
                retract = arm.retractSeconds;
            }

            _elapsed += dt;
            if (_phase == GesturePhase.Rest || (_phase == GesturePhase.Retract && _elapsed >= retract))
            {
                _phase = GesturePhase.Rest;
                for (int i = 0; i < _joints.Length; i++)
                {
                    _joints[i] = rootWorld + restOrientation * _rest[i];
                }
                return true;
            }

            if (!RenderMath.IsFinite(rootWorld) || !RenderMath.IsFinite(_goal))
            {
                return false;
            }

            Vector3 target = _goal;
            if (_phase == GesturePhase.Extend)
            {
                target = Vector3.Lerp(_startGoal, _goal, Mathf.Clamp01(_elapsed / extendSeconds));
                if (_elapsed >= extendSeconds)
                {
                    _phase = GesturePhase.Hold;
                    _elapsed = 0f;
                }
            }

            if (_phase == GesturePhase.Retract)
            {
                float blend = Mathf.Clamp01(_elapsed / retract);
                Vector3 restTip = rootWorld + restOrientation * _rest[_rest.Length - 1];
                target = Vector3.Lerp(_returnGoal, restTip, blend);
                // Only an initial guess, FABRIK projects every link right below
                for (int i = 0; i < _joints.Length; i++)
                {
                    _joints[i] = Vector3.Lerp(_joints[i], rootWorld + restOrientation * _rest[i], blend);
                }
            }

            if (!Follow(rootWorld, target, restOrientation, arm.isRod))
            {
                return false;
            }

            if (_phase == GesturePhase.Contact && _elapsed >= contactSeconds)
            {
                if (_isEndPending || _kind == GestureKind.Heal)
                {
                    StartReturn();
                }
                else
                {
                    _phase = GesturePhase.Hold;
                    _elapsed = 0f;
                }
            }
            return true;
        }

        // The chain's three profiles: a reach solved toward the target, a rod or an arc straight to it
        bool Follow(Vector3 rootWorld, Vector3 target, Quaternion restOrientation, bool isRod)
        {
            if (!_isDeliveryProfile)
            {
                return _solver.Solve(_joints, _lengths, rootWorld, target, restOrientation * _pole, out _,
                    solveIterations);
            }

            if (isRod)
            {
                // Delivery rods telescope visually, the projectile endpoint stays authoritative
                if (_style == DeliveryStyle.Rigid && _phase != GesturePhase.Retract)
                {
                    target = _goal;
                }

                for (int i = 0; i < _joints.Length; i++)
                {
                    _joints[i] = Vector3.Lerp(rootWorld, target, (float)i / (_joints.Length - 1));
                }
                return true;
            }

            // A parabola through both ends, highest halfway
            float height = Mathf.Min(arcMaxLift, Vector3.Distance(rootWorld, target) * arcLiftPerCell);
            for (int i = 0; i < _joints.Length; i++)
            {
                float t = (float)i / (_joints.Length - 1);
                float lift = 4f * t * (1f - t) * height;
                _joints[i] = Vector3.Lerp(rootWorld, target, t) + Vector3.up * lift;
            }
            return true;
        }

        void StartReturn()
        {
            _returnGoal = tip;
            _phase = GesturePhase.Retract;
            _elapsed = 0f;
            _isEndPending = false;
        }
    }
}

using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Deliveries
{
    // A spell-owned connection. The caster lends an outlet, never its geometry or delivery decision.
    public sealed class DeliveryPathView : MonoBehaviour
    {
        public static readonly int MaxContacts = 32;
        readonly List<ProjectileContact> _contacts = new List<ProjectileContact>();
        CastSourceLease _source;
        DeliveryPathLook _look;
        LineRenderer _edge;
        LineRenderer _core;
        Color _colour;
        MaterialPropertyBlock _paint;
        float _age;
        float _releaseAge;
        bool _releasing;
        Vector3[] _points;
        public int contactCount => _contacts.Count;
        public string sourceId => _source?.sourceId;
        public Vector3 origin { get; private set; }
        public bool isReleasing => _releasing;

        public static DeliveryPathView Create(GameObject source, DeliveryChannels channels,
            DeliveryVocabulary vocabulary, int layer, uint sequence = 0)
        {
            if (!vocabulary || !vocabulary.material || !vocabulary.palette)
            {
                return null;
            }
            GameObject go = new GameObject("Spell " + channels.path) { layer = layer };
            DeliveryPathView view = go.AddComponent<DeliveryPathView>();
            view._paint = new MaterialPropertyBlock();
            view._look = vocabulary.GetPath(channels.path);
            view._source = CastSourceLease.From(source, sequence);
            view._colour = vocabulary.palette.Accent(channels.family);
            view._edge = view.Line("Spell path", vocabulary.material);
            view._core = view.Line("Spell path core", vocabulary.material);
            return view;
        }

        LineRenderer Line(string label, Material material)
        {
            GameObject child = new GameObject(label) { layer = gameObject.layer };
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.generateLightingData = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            line.positionCount = 0;
            return line;
        }

        public void Contact(GameObject target, Vector3 point)
        {
            if (_releasing || _contacts.Count >= MaxContacts || !RenderMath.IsFinite(point))
            {
                return;
            }
            _contacts.Add(new ProjectileContact(target, point));
            _points = new Vector3[1 + _contacts.Count * _look.segmentsPerLeg];
            Advance(0f);
        }

        void Update() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (_look == null)
            {
                return;
            }
            float dt = float.IsFinite(deltaTime) ? Mathf.Max(0f, deltaTime) : 0f;
            _age += dt;
            if (_releasing)
            {
                _releaseAge += dt;
                if (_releaseAge >= _look.releaseSeconds)
                {
                    Dispose();
                    return;
                }
            }
            else if (_source == null || !_source.TryGet(out Vector3 point))
            {
                Release();
            }
            else
            {
                origin = point;
            }
            if (_contacts.Count == 0)
            {
                return;
            }
            _points[0] = origin;
            Vector3 start = origin;
            for (int leg = 0; leg < _contacts.Count; leg++)
            {
                ProjectileContact contact = _contacts[leg];
                Vector3 end = !_releasing && contact.target ? RenderTargets.Point(contact.target) : contact.position;
                if (!RenderMath.IsFinite(end))
                {
                    end = contact.position;
                }

                _contacts[leg] = new ProjectileContact(contact.target, end);
                Vector3 side = Vector3.Cross(end - start, Vector3.up).normalized;
                if (side.sqrMagnitude < .001f)
                {
                    side = Vector3.right;
                }

                for (int i = 1; i <= _look.segmentsPerLeg; i++)
                {
                    float t = i / (float)_look.segmentsPerLeg;
                    float wave = Mathf.Sin(i * 2.4f + leg * 3.1f + Mathf.Floor(_age * _look.pulseFrequency));
                    Vector3 offset = side * (wave * Mathf.Sin(t * Mathf.PI) * _look.deviation);
                    _points[leg * _look.segmentsPerLeg + i] = i == _look.segmentsPerLeg ? end : Vector3.Lerp(start, end, t) + offset;
                }
                start = end;
            }
            float fade = _releasing ? 1f - Mathf.Clamp01(_releaseAge / _look.releaseSeconds) : 1f;
            Paint(_edge, _colour, _look.width * fade);
            Paint(_core, Color.Lerp(_colour, Color.white, .75f), _look.width * _look.coreWidth * fade);
        }

        void Paint(LineRenderer line, Color colour, float width)
        {
            line.positionCount = _points.Length;
            line.SetPositions(_points);
            line.widthMultiplier = width;
            _paint.SetColor(RenderObjects.BaseColorId, colour);
            _paint.SetFloat("_HLHatchMultiplier", 0f);
            line.SetPropertyBlock(_paint);
        }

        public void Release()
        {
            if (_releasing)
            {
                return;
            }

            _releasing = true;
            _source?.Dispose();
            _source = null;
        }

        public void Dispose()
        {
            Release();
            gameObject.SetActive(false);
            RenderObjects.Release(gameObject);
        }

        void OnDestroy()
        {
            _source?.Dispose();
            _source = null;
        }
    }
}

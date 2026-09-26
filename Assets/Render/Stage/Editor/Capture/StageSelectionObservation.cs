using System;
using System.Collections.Generic;
using HealerLike.Render.Creatures;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // Reads the live host's painted parts after LateUpdate; it never drives the rig's selection state.
    public class StageSelectionObservation
    {
        [Serializable]
        public class Part
        {
            public string id;
            public int materialIndex;
            public Color colour;
            public bool hasOutlineOverride;
            public float outlineWidth;
        }

        [Serializable]
        public class Frame
        {
            public string subject;
            public string entity;
            public string entityInstance;
            public string presentationInstance;
            public string state;
            public string file;
            public int gameFrame;
            public float timeScale;
            public bool isHighlighted;
            public Color highlightColour;
            public float highlightWidth;
            public Vector2 touchPoint;
            public string hitCollider;
            public List<Part> parts = new List<Part>();
        }

        readonly CreatureBuilder _host;
        readonly SelectableEntity _source;
        readonly StagePresentationOutput _output;
        readonly StageSelectionAssets _assets;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        readonly List<Part> _idle;

        public StageSelectionObservation(CreatureBuilder host, SelectableEntity source, StagePresentationOutput output)
        {
            _host = host;
            _source = source;
            _output = output;
            _assets = new StageSelectionAssets(host.presentation.GetComponentsInChildren<Renderer>(true));
            foreach (CreaturePart part in host.rig.parts)
            {
                if (!part.shape.isProcedural)
                {
                    _assets.AddMesh(host.meshes.GetMesh(part.primitive, part.variant));
                }
            }

            _idle = ReadParts();
        }

        public Frame Sample(string subject, string state, bool highlighted, Vector2 touchPoint = default,
            string collider = null)
        {
            _output.Check(_source.isHighlighted == highlighted, subject + " live highlight state: " + state);
            _assets.Verify();
            _output.Check(!_host.rig.isAppearing, subject + " selection proof follows completed growth");
            Frame frame = new Frame { subject = subject, state = state, file = "selection-" + subject + "-"
                + state + ".png", gameFrame = Time.frameCount, timeScale = Time.timeScale,
                entity = _source.name, entityInstance = _source.GetEntityId().ToString(),
                presentationInstance = _host.presentation.GetEntityId().ToString(),
                isHighlighted = _source.isHighlighted,
                highlightColour = _source.highlightColor, highlightWidth = _source.highlightWidth,
                touchPoint = touchPoint, hitCollider = collider, parts = ReadParts() };
            _output.Check(frame.parts.Count == _idle.Count && frame.parts.Count > 0,
                subject + " retained part/material observations: " + state);
            for (int i = 0; i < frame.parts.Count; i++)
            {
                Part actual = frame.parts[i];
                Part idle = _idle[i];
                Color expected = highlighted ? Color.Lerp(idle.colour, frame.highlightColour, 0.3f) : idle.colour;
                expected.a = idle.colour.a;
                float width = highlighted ? Mathf.Max(idle.outlineWidth, frame.highlightWidth) : idle.outlineWidth;
                _output.Check(Vector4.Distance(actual.colour, expected) < 0.0001f,
                    subject + " " + actual.id + " live selection colour/alpha: " + state);
                _output.Check(Mathf.Abs(actual.outlineWidth - width) < 0.0001f
                    && actual.hasOutlineOverride == (highlighted || idle.hasOutlineOverride),
                    subject + " " + actual.id + " live selection outline: " + state);
            }

            _output.manifest.selection.Add(frame);
            _output.manifest.checks.Add(subject + " shared mesh UV3/indices/materials preserved: " + state);
            return frame;
        }

        List<Part> ReadParts()
        {
            List<Part> parts = new List<Part>();
            for (int i = 0; i < _host.rig.partTransforms.Count; i++)
            {
                Renderer renderer = _host.rig.partTransforms[i].GetComponent<Renderer>();
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    _block.Clear();
                    if (materials.Length > 1)
                    {
                        renderer.GetPropertyBlock(_block, index);
                    }
                    else
                    {
                        renderer.GetPropertyBlock(_block);
                    }

                    bool overridden = _block.HasFloat("_HLOutlineWidthMultiplier");
                    float width = overridden ? _block.GetFloat("_HLOutlineWidthMultiplier")
                        : materials[index].GetFloat("_HLOutlineWidthMultiplier");
                    parts.Add(new Part { id = _host.rig.parts[i].id, materialIndex = index,
                        colour = _block.GetColor("_BaseColor"), hasOutlineOverride = overridden,
                        outlineWidth = width });
                }
            }

            return parts;
        }
    }
}

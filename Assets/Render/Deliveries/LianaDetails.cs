using System;
using HealerLike.Render.Creatures;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Deliveries
{
    // One arm owns its tip geometry and leaf/bead draw state until that arm's lease returns to rest.
    public class LianaDetails : IDisposable
    {
        static readonly float leafLengthRadii = 9f;
        static readonly float beadRadii = 2.4f;
        static readonly float leafWidth = 0.38f;
        static readonly float leafDepth = 0.22f;
        static readonly float leafLean = 0.45f;
        static readonly float leafCentre = 0.45f;
        static readonly float zeroLengthSquared = 0.000000000001f;
        readonly Matrix4x4[] _leaves = new Matrix4x4[LianaArm.LeafCount];
        readonly Matrix4x4[] _beads = new Matrix4x4[LianaArm.LeafCount + 1];
        readonly DeliveryTip _tip = new DeliveryTip();
        PrimitiveMeshes _meshes;
        Material _material;
        MaterialPropertyBlock _colours;
        Color _colour;

        public void Init(PrimitiveMeshes meshes, Material material, Color colour)
        {
            _meshes = meshes;
            _material = material;
            _colour = colour;
            _colours = new MaterialPropertyBlock();
            Vector4[] colours = new Vector4[LianaArm.LeafCount + 1];
            for (int i = 0; i < colours.Length; i++)
            {
                colours[i] = colour;
            }

            _colours.SetVectorArray(RenderObjects.BaseColorId, colours);
        }

        public Matrix4x4 LeafMatrix(int index)
        {
            return _leaves[index];
        }

        public void Hide()
        {
            _tip.Hide();
        }

        public void Draw(LianaPose pose, Transform parent, float radius, float width, float tipWidth,
            DeliveryVocabulary vocabulary, Color tipColour)
        {
            int segments = pose.segmentCount;
            for (int i = 0; i < LianaArm.LeafCount; i++)
            {
                int j = Mathf.Clamp((i + 1) * segments / (LianaArm.LeafCount + 1), 1, segments - 1);
                Vector3 tangent = (pose.Joint(j + 1) - pose.Joint(j - 1)).normalized;
                if (tangent.sqrMagnitude < zeroLengthSquared)
                {
                    tangent = Vector3.up;
                }

                Vector3 axis = Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 side = Vector3.Cross(tangent, axis).normalized;
                if (i % 2 == 1)
                {
                    side = -side;
                }

                Vector3 direction = (side + tangent * leafLean).normalized;
                float length = radius * leafLengthRadii * width;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
                Vector3 leafScale = new Vector3(length * leafWidth, length, length * leafDepth);
                _leaves[i] = Matrix4x4.TRS(pose.Joint(j) + direction * length * leafCentre, rotation, leafScale);
                _beads[i] = Matrix4x4.TRS(pose.Joint(j), Quaternion.identity, Vector3.one * radius * beadRadii * width);
            }

            Vector3 last = pose.Joint(pose.jointCount - 2);
            _beads[LianaArm.LeafCount] = DeliveryTip.Frame(pose.tip, pose.tip - last, tipWidth);
            if (!_tip.isSet || _tip.style != pose.style)
            {
                _tip.SetStyle(pose.style, vocabulary, _meshes);
            }

            _tip.Draw(parent, _beads[LianaArm.LeafCount], _material, tipColour, _colour);
            GameObject container = parent.gameObject;
            if (!SystemInfo.supportsInstancing || !_material || !_material.enableInstancing
                || !container.activeInHierarchy)
            {
                return;
            }

            // Immediate draws are outside Outline's descendant scan and can keep borrowing the source meshes.
            Graphics.DrawMeshInstanced(_meshes.cone, 0, _material, _leaves, LianaArm.LeafCount, _colours,
                ShadowCastingMode.On, true, container.layer);
            Graphics.DrawMeshInstanced(_meshes.sphere, 0, _material, _beads, LianaArm.LeafCount, _colours,
                ShadowCastingMode.On, true, container.layer);
        }

        public void Dispose()
        {
            _tip.Release();
        }
    }
}

using UnityEngine;

namespace HealerLike.Render.Spells
{
    // Puts an element on the socket its entry names, read from the target's anchors
    public static class EffectPlacement
    {
        // A view without anchors is read as a body of this radius around its target point
        public static readonly float FallbackBodyRadius = 0.3f;
        // Press sits a quarter of a body unit (the body's diameter) over the head, in body radii
        public static readonly float AboveHeadGap = 0.5f;
        // A lasting element keeps out of the head sphere grown by a tenth of a body unit, in body radii
        public static readonly float HeadMargin = 0.2f;
        static readonly int samples = 16;
        static readonly int attempts = 24;
        // Long enough for both orbit spins to turn once
        static readonly float orbitSpan = 13f;

        public static EffectAnchors Anchors(GameObject target)
        {
            return EffectAnchorReader.Read(target);
        }

        public static Vector3 Socket(EffectAnchors anchors, EffectSocket socket)
        {
            float radius = anchors.bodyRadius;
            switch (socket)
            {
                case EffectSocket.AboveHead:
                    return anchors.headCentre + Vector3.up * (anchors.headRadius + AboveHeadGap * radius);
                case EffectSocket.UnderHead:
                    // On the right of the body, just under the head, so the drops fall clear of both
                    return new Vector3(anchors.bodyCentre.x + 1.15f * radius,
                                       anchors.headCentre.y - anchors.headRadius - 0.55f * radius,
                                       anchors.bodyCentre.z);
                case EffectSocket.Ground:
                case EffectSocket.Feet:
                    return anchors.foot;
                default:
                    return anchors.bodyCentre;
            }
        }

        // Apply once after socket placement. Depth is authored in the effect's body-radius units.
        public static void FaceCamera(SpellEffect effect, Camera camera)
        {
            if (!effect || !camera)
            {
                return;
            }

            SpellEffect[] layers = effect.GetComponentsInChildren<SpellEffect>(true);
            var positions = new Vector3[layers.Length];
            var rotations = new Quaternion[layers.Length];
            var scales = new Vector3[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                positions[i] = layers[i].transform.position;
                rotations[i] = layers[i].transform.rotation;
                scales[i] = layers[i].transform.lossyScale;
            }
            // Restore each socket after rotating its ancestors. Billboard choices belong to individual layers.
            for (int i = 0; i < layers.Length; i++)
            {
                SpellEffect layer = layers[i];
                Transform root = layer.transform;
                bool facesCamera = layer.recipe?.presentation?.billboard == true;
                root.SetPositionAndRotation(positions[i], facesCamera ? camera.transform.rotation : rotations[i]);
                Vector3 parentScale = root.parent ? root.parent.lossyScale : Vector3.one;
                if (Mathf.Abs(parentScale.x - parentScale.y) > .0001f
                    || Mathf.Abs(parentScale.x - parentScale.z) > .0001f)
                {
                    Vector3 current = root.lossyScale;
                    root.localScale = Vector3.Scale(root.localScale, new Vector3(
                        scales[i].x / Mathf.Max(.0001f, current.x),
                        scales[i].y / Mathf.Max(.0001f, current.y),
                        scales[i].z / Mathf.Max(.0001f, current.z)));
                }
                if (facesCamera)
                {
                    root.position -= camera.transform.forward * (scales[i].x * layer.recipe.presentation.cameraDepth);
                }
            }
        }

        // Lays the element at its socket in body radii, the parent keeps it on a moving unit
        public static void Place(SpellEffect effect, Transform parent, EffectAnchors anchors)
        {
            if (effect == null || effect.recipe == null)
            {
                return;
            }

            Transform root = effect.transform;
            root.SetParent(parent, false);
            root.position = Socket(anchors, effect.recipe.socket);
            root.rotation = Quaternion.identity;
            float parentScale = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.x) : 1f;
            root.localScale = Vector3.one * (anchors.bodyRadius / parentScale) * effect.recipe.scale;
            if (effect.recipe.presentation != null && effect.recipe.presentation.closesOverHead)
            {
                FitToNeck(effect, anchors);
            }

            if (effect.isLasting && (effect.recipe.presentation == null || effect.recipe.presentation.avoidHead))
            {
                KeepOffHead(effect, anchors);
            }

            // Drops fall from where they ended up to the ground
            if (effect.recipe.socket == EffectSocket.UnderHead)
            {
                effect.SetFallDistance((root.position.y - anchors.foot.y) / Mathf.Max(.0001f, root.lossyScale.y));
            }
            effect.PlaceLayers(anchors);
            effect.Advance(0f);
        }

        // Moves the element away from the head until no pose of its motion reaches into it: down for what hangs on
        // the body, up for what presses from above, and wider for what rises from the feet
        static void KeepOffHead(SpellEffect effect, EffectAnchors anchors)
        {
            EffectSocket socket = effect.recipe.socket;
            if (socket == EffectSocket.Link || socket == EffectSocket.Ground)
            {
                return;
            }

            Transform root = effect.transform;
            bool isUp = socket == EffectSocket.AboveHead;
            float nudge = 0.02f * anchors.bodyRadius;
            for (int i = 0; i < attempts; i++)
            {
                float overlap = HeadOverlap(effect, anchors, isUp);
                if (overlap <= 0f)
                {
                    return;
                }

                if (socket == EffectSocket.Feet)
                {
                    // The ring widens rather than sinks, so the stalks keep their height
                    Vector3 scale = root.localScale;
                    scale.x *= 1.1f;
                    scale.z *= 1.1f;
                    root.localScale = scale;
                }
                else
                {
                    Vector3 away = Vector3.down;
                    if (isUp)
                    {
                        away = Vector3.up;
                    }
                    root.position += away * (overlap + nudge);
                }
            }
            Debug.LogError($"[EffectPlacement] {effect.recipe.element} still reaches the head after {attempts} moves.");
        }

        // How far the element must move to clear the grown head sphere, over sampled poses of its motion. Every
        // part counts, shown or not, because stacks and charges can show more later; critical rings belong to impacts.
        public static float HeadOverlap(SpellEffect effect, EffectAnchors anchors, bool isUp)
        {
            float radius = anchors.headRadius + HeadMargin * anchors.bodyRadius;
            float span = effect.recipe.motion == EffectMotionKind.Orbit ? orbitSpan : effect.recipe.cycleSeconds;
            float worst = 0f;
            for (int i = 0; i < samples; i++)
            {
                float phase = (float)i / (samples - 1);
                effect.Pose(phase, phase * span);
                foreach (Transform part in effect.parts)
                {
                    if (effect.rings.Contains(part))
                    {
                        continue;
                    }

                    Bounds bounds = WorldBounds(part);
                    if (bounds.size.sqrMagnitude > 0.00000001f)
                    {
                        worst = Mathf.Max(worst, Overlap(bounds, anchors.headCentre, radius, isUp));
                    }
                }
            }
            return worst;
        }

        // The vertical move that takes a box out of a sphere, zero when the box is beside it or already clear
        public static float Overlap(Bounds bounds, Vector3 centre, float radius, bool isUp)
        {
            float dx = Mathf.Max(0f, Mathf.Max(bounds.min.x - centre.x, centre.x - bounds.max.x));
            float dz = Mathf.Max(0f, Mathf.Max(bounds.min.z - centre.z, centre.z - bounds.max.z));
            float flat = dx * dx + dz * dz;
            if (flat >= radius * radius)
            {
                return 0f;
            }

            float half = Mathf.Sqrt(radius * radius - flat);
            if (isUp)
            {
                if (bounds.max.y <= centre.y - half)
                {
                    return 0f;
                }
                return Mathf.Max(0f, centre.y + half - bounds.min.y);
            }

            if (bounds.min.y >= centre.y + half)
            {
                return 0f;
            }
            return Mathf.Max(0f, bounds.max.y - (centre.y - half));
        }

        // The bud closes up to the neck, never over the head
        static void FitToNeck(SpellEffect effect, EffectAnchors anchors)
        {
            Transform root = effect.transform;
            effect.Pose(1f, effect.recipe.cycleSeconds);
            float top = float.NegativeInfinity;
            foreach (Transform shape in effect.shapes)
            {
                if (shape.gameObject.activeSelf)
                {
                    top = Mathf.Max(top, WorldBounds(shape).max.y);
                }
            }

            float reach = top - root.position.y;
            float wanted = anchors.neck.y - root.position.y;
            if (reach > 0.0001f && wanted > 0f)
            {
                Vector3 scale = root.localScale;
                scale.y *= Mathf.Clamp(wanted / reach, 0.3f, 1.5f);
                root.localScale = scale;
            }
        }

        public static Bounds WorldBounds(Transform part)
        {
            MeshFilter filter = part.GetComponent<MeshFilter>();
            Bounds local = new Bounds(Vector3.zero, Vector3.zero);
            if (filter != null && filter.sharedMesh != null)
            {
                local = filter.sharedMesh.bounds;
            }
            Matrix4x4 matrix = part.localToWorldMatrix;
            Bounds world = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                world.Encapsulate(matrix.MultiplyPoint3x4(RenderMath.Corner(local, i)));
            }
            return world;
        }

    }
}

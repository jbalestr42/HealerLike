using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace HealerLike.Render.Stage
{
    // Keeps GPU particle simulation and DestroyOnDone unchanged, silences only the exposed output colours
    public class LegacyAreaVisualMask : MonoBehaviour
    {
        readonly List<Action> _restore = new List<Action>();

        public int maskedPropertyCount { get { return _restore.Count; } }

        void OnEnable()
        {
            List<VFXExposedProperty> properties = new List<VFXExposedProperty>();
            foreach (VisualEffect effect in GetComponentsInChildren<VisualEffect>(true))
            {
                if (!effect.visualEffectAsset)
                {
                    continue;
                }

                properties.Clear();
                effect.visualEffectAsset.GetExposedProperties(properties);
                foreach (VFXExposedProperty property in properties)
                {
                    Mask(effect, property);
                }
            }
        }

        void OnDisable()
        {
            foreach (Action undo in _restore)
            {
                undo();
            }

            _restore.Clear();
        }

        void Mask(VisualEffect effect, VFXExposedProperty property)
        {
            string name = property.name;
            bool isColour = name.IndexOf("Color", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isColour && property.type == typeof(Vector4))
            {
                Vector4 previous = effect.GetVector4(name);
                _restore.Add(() => RestoreVector4(effect, name, previous));
                effect.SetVector4(name, Vector4.zero);
            }
            else if (isColour && property.type == typeof(Vector3))
            {
                Vector3 previous = effect.GetVector3(name);
                _restore.Add(() => RestoreVector3(effect, name, previous));
                effect.SetVector3(name, Vector3.zero);
            }
            else if (property.type == typeof(Gradient) && (isColour || name.Contains("Alpha") || name == "Gradient"))
            {
                Gradient previous = effect.GetGradient(name);
                _restore.Add(() => RestoreGradient(effect, name, previous));
                Gradient silent = new Gradient();
                GradientColorKey[] colours =
                {
                    new GradientColorKey(Color.black, 0f),
                    new GradientColorKey(Color.black, 1f)
                };
                GradientAlphaKey[] alphas = { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0f, 1f) };
                silent.SetKeys(colours, alphas);
                effect.SetGradient(name, silent);
            }
        }

        static void RestoreVector4(VisualEffect effect, string name, Vector4 value)
        {
            if (effect)
            {
                effect.SetVector4(name, value);
            }
        }

        static void RestoreVector3(VisualEffect effect, string name, Vector3 value)
        {
            if (effect)
            {
                effect.SetVector3(name, value);
            }
        }

        static void RestoreGradient(VisualEffect effect, string name, Gradient value)
        {
            if (effect)
            {
                effect.SetGradient(name, value);
            }
        }
    }
}

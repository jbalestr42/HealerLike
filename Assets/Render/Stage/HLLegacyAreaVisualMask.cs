using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
namespace HealerLike.Render.Stage
{
    // Keep GPU particle simulation and DestroyOnDone unchanged; silence only exposed output colours.
    public sealed class HLLegacyAreaVisualMask : MonoBehaviour
    {
        readonly List<Action> restore=new();
        public int MaskedPropertyCount => restore.Count;
        void OnEnable()
        {
            var properties=new List<VFXExposedProperty>();
            foreach(var effect in GetComponentsInChildren<VisualEffect>(true))
            {
                if(!effect.visualEffectAsset) continue;
                properties.Clear(); effect.visualEffectAsset.GetExposedProperties(properties);
                foreach(var property in properties)
                {
                    string name=property.name;
                    bool colour=name.IndexOf("Color",StringComparison.OrdinalIgnoreCase)>=0;
                    if(colour && property.type==typeof(Vector4))
                    {
                        Vector4 previous=effect.GetVector4(name);
                        restore.Add(()=> { if(effect) effect.SetVector4(name,previous); });
                        effect.SetVector4(name,Vector4.zero);
                    }
                    else if(colour && property.type==typeof(Vector3))
                    {
                        Vector3 previous=effect.GetVector3(name);
                        restore.Add(()=> { if(effect) effect.SetVector3(name,previous); });
                        effect.SetVector3(name,Vector3.zero);
                    }
                    else if(property.type==typeof(Gradient) && (colour || name.Contains("Alpha") || name=="Gradient"))
                    {
                        Gradient previous=effect.GetGradient(name);
                        restore.Add(()=> { if(effect) effect.SetGradient(name,previous); });
                        var silent=new Gradient();
                        silent.SetKeys(new[]{new GradientColorKey(Color.black,0),new GradientColorKey(Color.black,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(0,1)});
                        effect.SetGradient(name,silent);
                    }
                }
            }
        }
        void OnDisable()
        {
            foreach(var undo in restore) undo();
            restore.Clear();
        }
    }
}

using UnityEngine;
namespace HealerLike.Render.Stage
{
    // Retain the original VFX simulation and its DestroyOnDone lifetime; suppress only its output.
    public sealed class HLLegacyAreaVisualMask : MonoBehaviour
    {
        Renderer[] outputs;
        bool[] previous;
        void OnEnable()
        {
            outputs=GetComponentsInChildren<Renderer>(true);
            previous=new bool[outputs.Length];
            for(int i=0;i<outputs.Length;i++)
            {
                previous[i]=outputs[i].forceRenderingOff;
                outputs[i].forceRenderingOff=true;
            }
        }
        void OnDisable()
        {
            if(outputs==null) return;
            for(int i=0;i<outputs.Length;i++) if(outputs[i]) outputs[i].forceRenderingOff=previous[i];
        }
    }
}

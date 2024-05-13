using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BuffManager))]
public class VisualBuffManager : MonoBehaviour
{
    Dictionary<BuffManager.BuffHandlerData, GameObject> _buffHandlers = new Dictionary<BuffManager.BuffHandlerData, GameObject>();

    void Start()
    {
        BuffManager buffManager = GetComponent<BuffManager>();
        buffManager.OnBuffHandlerStarted.AddListener(OnBuffHandlerStarted);
        buffManager.OnBuffHandlerStopped.AddListener(OnBuffHandlerStopped);
    }

    void OnBuffHandlerStarted(BuffManager.BuffHandlerData buffHandlerData)
    {
        if (!_buffHandlers.ContainsKey(buffHandlerData))
        {
            GameObject buffEffectPrefab = buffHandlerData.buffHandlerFactory.GetBuffEffect();
            if (buffEffectPrefab)
            {
                GameObject buffEffect = Instantiate(buffEffectPrefab, buffHandlerData.target.GetComponent<Entity>().targetPoint.transform);
                // buffEffect.start
                _buffHandlers[buffHandlerData] = buffEffect;
            }
        }
    }

    void OnBuffHandlerStopped(BuffManager.BuffHandlerData buffHandlerData)
    {
        if (_buffHandlers.ContainsKey(buffHandlerData))
        {
            GameObject buffEffect = _buffHandlers[buffHandlerData];
            Destroy(buffEffect);
            // buffEffect.stop
            _buffHandlers.Remove(buffHandlerData);
        }
    }
}
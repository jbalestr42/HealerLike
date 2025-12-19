
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class BuffManager : SerializedMonoBehaviour
{
    [HideInInspector] public UnityEvent<BuffData> OnBuffAdded = new UnityEvent<BuffData>();
    [HideInInspector] public UnityEvent<BuffData> OnBuffRemoved = new UnityEvent<BuffData>();
    [HideInInspector] public UnityEvent<BuffHandlerData> OnBuffHandlerStarted = new UnityEvent<BuffHandlerData>();
    [HideInInspector] public UnityEvent<BuffHandlerData> OnBuffHandlerStopped = new UnityEvent<BuffHandlerData>();

    public class BuffData
    {
        public int stacks = 0;
        public List<ABuff> buffList = new List<ABuff>();

        public bool isStackable => buffList.Count > 0 && first.isStackable;
        public bool shouldStack => stacks > 0 && isStackable;
        public bool shouldUnstack => stacks > 0 && isStackable;
        public ABuff first => buffList.Count > 0 ? buffList[0] : null;
    }

    class BuffDataPerId
    {
        // BuffFactory unique Id -> BuffDataPerId
        [DictionaryDrawerSettings(KeyLabel = "Id", ValueLabel = "Buff Data")]
        public Dictionary<string, BuffData> buffPerId = new Dictionary<string, BuffData>();
    }

    public class BuffHandlerData
    {
        public ABuffHandlerFactory buffHandlerFactory = null;
        public ABuffHandler buffHandler = null;
        public GameObject target = null;
        public int refreshStacks = 0;
        public int currentStacks = 0;
        public bool hasStarted => currentStacks != 0;
        public bool isInit => buffHandler != null;
        public bool shouldRemove => currentStacks == 0;
        public bool shouldRefresh => refreshStacks != 0;
    }

    class BuffHandlerDataPerId
    {
        // BuffHandlerFactory unique Id -> BuffHandlerDataPerId
        [DictionaryDrawerSettings(KeyLabel = "Id", ValueLabel = "Buff Handler Data")]
        public Dictionary<string, BuffHandlerData> buffHandlerPerId = new Dictionary<string, BuffHandlerData>();
    }

    // Buff source -> BuffData
    [DictionaryDrawerSettings(KeyLabel = "Source", ValueLabel = "Data per Id")]
    [SerializeField] Dictionary<GameObject, BuffDataPerId> _buffPerSource = new Dictionary<GameObject, BuffDataPerId>();
    // Buff source -> BuffHandlerData
    [DictionaryDrawerSettings(KeyLabel = "Source", ValueLabel = "Handler Data per Id")]
    [SerializeField] Dictionary<GameObject, BuffHandlerDataPerId> _buffHandlerPerSource = new Dictionary<GameObject, BuffHandlerDataPerId>();

    List<string> _cachedIdsToRemove = new List<string>();
    List<GameObject> _cachedSourcesToRemove = new List<GameObject>();

    public bool isEnabled { get; set; }

    void OnDestroy()
    {
        foreach (var item in _buffPerSource)
        {
            foreach (var buffData in item.Value.buffPerId)
            {
                buffData.Value.buffList.Clear();
            }
            item.Value.buffPerId.Clear();
        }
        _buffPerSource.Clear();

        foreach (var item in _buffHandlerPerSource)
        {
            item.Value.buffHandlerPerId.Clear();
        }
        _buffHandlerPerSource.Clear();
    }

    public void Reset()
    {
        foreach (var handlerPerSource in _buffHandlerPerSource)
        {
            GameObject source = handlerPerSource.Key;
            foreach (var kvpBuffHandler in handlerPerSource.Value.buffHandlerPerId)
            {
                BuffHandlerData buffHandlerData = kvpBuffHandler.Value;
                buffHandlerData.buffHandler.ResetPeriodDuration();
            }
        }
    }

    void Update()
    {
        if (!isEnabled)
        {
            return;
        }
        
        foreach (var handlerPerSource in _buffHandlerPerSource)
        {
            GameObject source = handlerPerSource.Key;
            foreach (var kvpBuffHandler in handlerPerSource.Value.buffHandlerPerId)
            {
                BuffHandlerData buffHandlerData = kvpBuffHandler.Value;
                if (buffHandlerData.isInit)
                {
                    ABuffHandlerFactory buffHandlerFactory = buffHandlerData.buffHandlerFactory;
                    if (buffHandlerData.buffHandler.durationType == DurationType.Instant)
                    {
                        foreach (var buffFactory in buffHandlerFactory.buffFactoryList)
                        {
                            // In some cases we are cumulating multiple instant buff, so we must appy all of them
                            Debug.Log($"[BuffManager] Instant buff {buffFactory.name} | refreshStacks={buffHandlerData.refreshStacks}");
                            for (int i = 0; i < buffHandlerData.refreshStacks; i++)
                            {
                                ABuff buff = buffFactory.GetBuff();
                                buff.Instant(source, buffHandlerData.target);
                            }
                            _cachedIdsToRemove.Add(kvpBuffHandler.Key);
                        }
                    }
                    else
                    {
                        // First time we start the handler
                        if (!buffHandlerData.hasStarted)
                        {
                            Debug.Log("[BuffManager] Start buff handler " + buffHandlerFactory.name);
                            buffHandlerData.buffHandler.Start(source, buffHandlerData.target);
                            OnBuffHandlerStarted.Invoke(buffHandlerData);
                        }

                        // Handler need to stack/unstack
                        if (buffHandlerData.shouldRefresh)
                        {
                            Debug.Log($"[BuffManager] Refresh buff handler {buffHandlerFactory.name} | currentStacks={buffHandlerData.currentStacks} | refreshStacks={buffHandlerData.refreshStacks}");
                            buffHandlerData.buffHandler.Refresh(source, buffHandlerData.target);
                            for (int i = 0; i < buffHandlerData.refreshStacks; i++)
                            {
                                foreach (var buffFactory in buffHandlerFactory.buffFactoryList)
                                {
                                    Add(buffFactory, source, buffHandlerData.target);
                                }
                                buffHandlerData.currentStacks++;
                            }
                            for (int i = 0; i > buffHandlerData.refreshStacks; i--)
                            {
                                foreach (var buffFactory in buffHandlerFactory.buffFactoryList)
                                {
                                    Remove(buffFactory, source, buffHandlerData.target);
                                }
                                buffHandlerData.currentStacks--;
                            }
                            buffHandlerData.refreshStacks = 0;
                        }

                        buffHandlerData.buffHandler.Update(Time.deltaTime);

                        if (buffHandlerData.buffHandler.isPeriodic && buffHandlerData.buffHandler.isPeriodDone)
                        {
                            foreach (var buffFactory in buffHandlerFactory.buffFactoryList)
                            {
                                BuffData buffData = GetBuffData(buffFactory, source);
                                ABuff buff = buffData.first;
                                if (buff != null)
                                {
                                    Debug.Log("[BuffManager] Instant periodic buff " + buffFactory.name);
                                    buff.Instant(source, buffHandlerData.target);
                                    buffHandlerData.buffHandler.ResetPeriodDuration();
                                }
                            }
                        }

                        if (buffHandlerData.buffHandler.isDone || buffHandlerData.shouldRemove)
                        {
                            Debug.Log("[BuffManager] Stop buff handler " + buffHandlerFactory.name);
                            foreach (var buffFactory in buffHandlerFactory.buffFactoryList)
                            {
                                Remove(buffFactory, source, buffHandlerData.target, removeAll: true);
                            }
                            buffHandlerData.buffHandler.Stop(source, buffHandlerData.target);
                            OnBuffHandlerStopped.Invoke(buffHandlerData);
                            _cachedIdsToRemove.Add(kvpBuffHandler.Key);
                        }
                    }
                }
            }

            RemoveCachedIdsFromHandler(handlerPerSource.Value.buffHandlerPerId);
        }

        RemoveOutdatedSources();
    }

    public void ForceUpdate()
    {
        Update();
    }

    public void RemoveBuffWithTag(GameplayTag tag)
    {
        foreach (var handlerPerSource in _buffHandlerPerSource)
        {
            foreach (var kvpBuffHandler in handlerPerSource.Value.buffHandlerPerId)
            {
                BuffHandlerData buffHandlerData = kvpBuffHandler.Value;
                if (buffHandlerData.buffHandlerFactory.tags.Contains(tag))
                {
                    _cachedIdsToRemove.Add(kvpBuffHandler.Key);
                }
            }

            RemoveCachedIdsFromHandler(handlerPerSource.Value.buffHandlerPerId);
        }

        RemoveOutdatedSources();
    }

    public void RemoveBuffWithoutTag(GameplayTag tag)
    {
        foreach (var handlerPerSource in _buffHandlerPerSource)
        {
            foreach (var kvpBuffHandler in handlerPerSource.Value.buffHandlerPerId)
            {
                BuffHandlerData buffHandlerData = kvpBuffHandler.Value;
                if (!buffHandlerData.buffHandlerFactory.tags.Contains(tag))
                {
                    _cachedIdsToRemove.Add(kvpBuffHandler.Key);
                }
            }

            RemoveCachedIdsFromHandler(handlerPerSource.Value.buffHandlerPerId);
        }

        RemoveOutdatedSources();
    }

    void RemoveCachedIdsFromHandler(Dictionary<string, BuffHandlerData> buffHandlerPerId)
    {
        foreach (string idToRemove in _cachedIdsToRemove)
        {
            buffHandlerPerId.Remove(idToRemove);
        }
        _cachedIdsToRemove.Clear();
    }

    void RemoveOutdatedSources()
    {
        // Clean sources that do not have an active buff or sources that doesn't exists anymore
        foreach (var handlerPerSource in _buffHandlerPerSource)
        {
            if (handlerPerSource.Key == null)
            {
                _cachedSourcesToRemove.Add(handlerPerSource.Key);
            }
            else if (handlerPerSource.Value.buffHandlerPerId.Count() == 0)
            {
                _cachedSourcesToRemove.Add(handlerPerSource.Key);
            }
        }
        
        foreach (GameObject sourceToRemove in _cachedSourcesToRemove)
        {
            _buffHandlerPerSource.Remove(sourceToRemove);
        }

        _cachedSourcesToRemove.Clear();
    }

    public void AddHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target)
    {
        if (string.IsNullOrEmpty(buffHandlerFactory.uniqueID))
        {
            Debug.LogError($"[BuffManager] uniqueId is null for factory '{buffHandlerFactory.name}'");
        }

        BuffHandlerData buffHandlerData = GetBuffHandlerData(buffHandlerFactory, source);
        if (!buffHandlerData.isInit)
        {
            Debug.Log("[BuffManager] Init handler " + buffHandlerFactory.name);
            buffHandlerData.buffHandler = buffHandlerFactory.GetBuffHandler();
            buffHandlerData.buffHandlerFactory = buffHandlerFactory;
            buffHandlerData.target = target;
        }
        else
        {
            Debug.Log("[BuffManager] Refresh handler " + buffHandlerFactory.name);
        }
        buffHandlerData.refreshStacks++;
    }

    public void RemoveHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target, bool removeAll = false)
    {
        if (string.IsNullOrEmpty(buffHandlerFactory.uniqueID))
        {
            Debug.LogError($"[BuffManager] uniqueId is null for factory '{buffHandlerFactory.name}'");
        }

        BuffHandlerData buffHandlerData = GetBuffHandlerData(buffHandlerFactory, source);
        if (buffHandlerData != null)
        {
            Debug.Log("[BuffManager] Remove handler " + buffHandlerFactory.name);
            buffHandlerData.refreshStacks--;
        }
    }

    void Add(ABuffFactory buffFactory, GameObject source, GameObject target)
    {
        if (string.IsNullOrEmpty(buffFactory.uniqueID))
        {
            Debug.LogError($"[BuffManager] UniqueId is null for factory '{buffFactory.name}'");
        }

        BuffData buffData = GetBuffData(buffFactory, source);
        if (buffData.shouldStack)
        {
            Debug.Log("[BuffManager] Stack buff " + buffFactory.name + " | stacks=" + buffData.stacks);
            IStackableBuff stackableBuff = buffData.first as IStackableBuff;
            stackableBuff.Stack(source, target);
        }
        else
        {
            Debug.Log("[BuffManager] Add buff " + buffFactory.name);
            ABuff buff = buffFactory.GetBuff();
            buffData.buffList.Add(buff);
            buff.Add(source, target);
        }
        buffData.stacks++;
        OnBuffAdded.Invoke(buffData);
    }

    void Remove(ABuffFactory buffFactory, GameObject source, GameObject target, bool removeAll = false)
    {
        if (_buffPerSource.ContainsKey(source))
        {
            BuffDataPerId sourceBuff = _buffPerSource[source];
            if (sourceBuff.buffPerId.ContainsKey(buffFactory.uniqueID))
            {
                BuffData buffData = sourceBuff.buffPerId[buffFactory.uniqueID];
                if (buffData.first != null)
                {
                    if (buffData.shouldUnstack && !removeAll)
                    {
                        Debug.Log("[BuffManager] Unstack buff " + buffFactory.name + " | stacks=" + buffData.stacks);
                        buffData.stacks--;
                        IStackableBuff stackableBuff = buffData.first as IStackableBuff;
                        stackableBuff.Unstack(source, target);
                    }
                    else
                    {
                        Debug.Log("[BuffManager] Remove buff " + buffFactory.name + " | stacks=" + buffData.stacks);
                        ABuff buff = buffData.first;
                        buffData.stacks = 0;
                        buffData.buffList.Remove(buff);
                        buff.Remove(source, target);
                    }
                }
                OnBuffRemoved.Invoke(buffData);
            }
        }
    }

    BuffData GetBuffData(ABuffFactory buffFactory, GameObject source)
    {
        // Get data by source
        if (!_buffPerSource.ContainsKey(source))
        {
            _buffPerSource[source] = new BuffDataPerId();
        }
        BuffDataPerId sourceBuff = _buffPerSource[source];

        // Get data per buff unique ID
        if (!sourceBuff.buffPerId.ContainsKey(buffFactory.uniqueID))
        {
            sourceBuff.buffPerId[buffFactory.uniqueID] = new BuffData();
        }
        return sourceBuff.buffPerId[buffFactory.uniqueID];
    }

    BuffHandlerData GetBuffHandlerData(ABuffHandlerFactory buffHandlerFactory, GameObject source)
    {
        // Get data by source
        if (!_buffHandlerPerSource.ContainsKey(source))
        {
            _buffHandlerPerSource[source] = new BuffHandlerDataPerId();
        }
        BuffHandlerDataPerId sourceBuffHandler = _buffHandlerPerSource[source];

        // Get data per buff handler unique ID
        if (!sourceBuffHandler.buffHandlerPerId.ContainsKey(buffHandlerFactory.uniqueID))
        {
            sourceBuffHandler.buffHandlerPerId[buffHandlerFactory.uniqueID] = new BuffHandlerData();
        }
        return sourceBuffHandler.buffHandlerPerId[buffHandlerFactory.uniqueID];
    }
}
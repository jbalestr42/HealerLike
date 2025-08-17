
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BuffManager : MonoBehaviour
{
    [HideInInspector] public UnityEvent<BuffData> OnBuffAdded = new UnityEvent<BuffData>();
    [HideInInspector] public UnityEvent<BuffData> OnBuffRemoved = new UnityEvent<BuffData>();
    [HideInInspector] public UnityEvent<BuffHandlerData> OnBuffHandlerStarted = new UnityEvent<BuffHandlerData>();
    [HideInInspector] public UnityEvent<BuffHandlerData> OnBuffHandlerStopped = new UnityEvent<BuffHandlerData>();

    public class BuffData
    {
        public int stacks = 0;
        public List<ABuff> buffList = new List<ABuff>();

        public bool shouldStack => buffList.Count > 0 && first.isStackable;
        public bool shouldUnstack => stacks > 0 && first.isStackable;
        public ABuff first => buffList.Count > 0 ? buffList[0] : null;
    }

    class BuffDataPerId
    {
        // BuffFactory unique Id -> BuffDataPerId
        public Dictionary<string, BuffData> buffPerId = new Dictionary<string, BuffData>();
    }

    public class BuffHandlerData
    {
        public ABuffHandlerFactory buffHandlerFactory = null;
        public ABuffHandler buffHandler = null;
        public GameObject target = null;
        public bool hasStarted = false;
        public int refreshStacks = 0;
        public bool isInit => buffHandler != null;
        public bool shouldRefresh => refreshStacks > 0;
    }

    class BuffHandlerDataPerId
    {
        // BuffHandlerFactory unique Id -> BuffHandlerDataPerId
        public Dictionary<string, BuffHandlerData> buffHandlerPerId = new Dictionary<string, BuffHandlerData>();
    }

    // Buff source -> BuffData
    Dictionary<GameObject, BuffDataPerId> _buffPerSource = new Dictionary<GameObject, BuffDataPerId>();
    // Buff source -> BuffHandlerData
    Dictionary<GameObject, BuffHandlerDataPerId> _buffHandlerPerSource = new Dictionary<GameObject, BuffHandlerDataPerId>();

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

    void Update()
    {
        foreach (var item in _buffHandlerPerSource)
        {
            GameObject source = item.Key;
            foreach (var kvpBuffHandler in item.Value.buffHandlerPerId)
            {
                BuffHandlerData buffHandlerData = kvpBuffHandler.Value;
                if (buffHandlerData.isInit)
                {
                    ABuffHandlerFactory buffHandlerFactory = buffHandlerData.buffHandlerFactory;
                    if (buffHandlerData.buffHandler.durationType == DurationType.Instant)
                    {
                        Debug.Log("[BuffManager] Instant buff " + buffHandlerFactory.buffFactory.name);
                        ABuff buff = buffHandlerFactory.buffFactory.GetBuff();
                        buff.Instant(source, buffHandlerData.target);
                        buffHandlerData.buffHandler = null;
                    }
                    else
                    {
                        if (!buffHandlerData.hasStarted)
                        {
                            Debug.Log("[BuffManager] Start buff " + buffHandlerFactory.buffFactory.name);
                            buffHandlerData.hasStarted = true;
                            buffHandlerData.buffHandler.Start(source, buffHandlerData.target);
                            Add(buffHandlerFactory.buffFactory, source, buffHandlerData.target);
                            OnBuffHandlerStarted.Invoke(buffHandlerData);
                        }

                        if (buffHandlerData.shouldRefresh)
                        {
                            Debug.Log("[BuffManager] Refresh buff " + buffHandlerFactory.buffFactory.name);
                            buffHandlerData.buffHandler.Refresh(source, buffHandlerData.target);
                            for (int i = 0; i < buffHandlerData.refreshStacks; i++)
                            {
                                Add(buffHandlerFactory.buffFactory, source, buffHandlerData.target);
                            }
                            buffHandlerData.refreshStacks = 0;
                        }

                        buffHandlerData.buffHandler.Update(Time.deltaTime);

                        if (buffHandlerData.buffHandler.isPeriodic && buffHandlerData.buffHandler.isPeriodDone)
                        {
                            BuffData buffData = GetBuffData(buffHandlerFactory.buffFactory, source);
                            ABuff buff = buffData.first;
                            if (buff != null)
                            {
                                Debug.Log("[BuffManager] Instant periodic buff " + buffHandlerFactory.buffFactory.name);
                                buff.Instant(source, buffHandlerData.target);
                                buffHandlerData.buffHandler.ResetPeriodDuration();
                            }
                        }

                        if (buffHandlerData.buffHandler.isDone)
                        {
                            Debug.Log("[BuffManager] Stop buff " + buffHandlerFactory.buffFactory.name);
                            Remove(buffHandlerFactory.buffFactory, source, buffHandlerData.target, true);
                            buffHandlerData.buffHandler.Stop(source, buffHandlerData.target);
                            OnBuffHandlerStopped.Invoke(buffHandlerData);
                            buffHandlerData.buffHandler = null;
                        }
                    }
                }
            }
        }
    }

    public void AddHandler(ABuffHandlerFactory buffHandlerFactory, GameObject source, GameObject target)
    {
        if (string.IsNullOrEmpty(buffHandlerFactory.uniqueID))
        {
            Debug.LogError($"[BuffManager] uniqueId is null for factory '{buffHandlerFactory.buffFactory.name}'");
        }

        BuffHandlerData buffHandlerData = GetBuffHandlerData(buffHandlerFactory, source);
        if (!buffHandlerData.isInit)
        {
            Debug.Log("[BuffManager] Init handler " + buffHandlerFactory.buffFactory.name);
            buffHandlerData.buffHandler = buffHandlerFactory.GetBuffHandler();
            buffHandlerData.buffHandlerFactory = buffHandlerFactory;
            buffHandlerData.target = target;
            buffHandlerData.hasStarted = false;
        }
        else
        {
            Debug.Log("[BuffManager] Refresh handler " + buffHandlerFactory.buffFactory.name);
            buffHandlerData.refreshStacks++;
        }
    }

    public void Add(ABuffFactory buffFactory, GameObject source, GameObject target)
    {
        if (string.IsNullOrEmpty(buffFactory.uniqueID))
        {
            Debug.LogError($"[BuffManager] UniqueId is null for factory '{buffFactory.name}'");
        }

        BuffData buffData = GetBuffData(buffFactory, source);
        if (buffData.shouldStack)
        {
            buffData.stacks++;
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
        OnBuffAdded.Invoke(buffData);
    }

    public void Remove(ABuffFactory buffFactory, GameObject source, GameObject target, bool removeAll = false)
    {
        if (_buffPerSource.ContainsKey(source))
        {
            BuffDataPerId sourceBuff = _buffPerSource[source];
            if (sourceBuff.buffPerId.ContainsKey(buffFactory.uniqueID))
            {
                BuffData buffData = sourceBuff.buffPerId[buffFactory.uniqueID];
                if (buffData.shouldUnstack && !removeAll)
                {
                    Debug.Log("[BuffManager] Unstack buff " + buffFactory.name + " | stacks=" + buffData.stacks);
                    buffData.stacks--;
                    IStackableBuff stackableBuff = buffData.first as IStackableBuff;
                    stackableBuff.Unstack(source, target);
                }
                else
                {
                    Debug.Log("[BuffManager] Remove buff " + buffFactory.name + " | stacks=" + (removeAll ? buffData.stacks : 1));
                    ABuff buff = buffData.first;
                    buffData.stacks = 0;
                    buffData.buffList.Remove(buff);
                    buff.Remove(source, target);
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
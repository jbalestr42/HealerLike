using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/Buff/ShareHealOnRelativeCellBuff")]
public class ShareHealOnRelativeCellBuffFactory : BuffFactory<ShareHealOnRelativeCellBuff, ShareHealOnRelativeCellBuffData> { }

[Serializable]
public class ShareHealOnRelativeCellBuffData
{
    // Part of each heal received by the holder also healed on the allies on the cells of the pattern
    [MinValue(0)]
    public float ratio = 0.2f;
    public RelativeCellPatternType pattern = RelativeCellPatternType.Adjacent;
    [MinValue(1)]
    public int range = 1;
    // Smaller shared heals are dropped, so two holders next to each other stop sharing back and forth
    [MinValue(0)]
    public float minimumSharedHeal = 1f;
    // Shown on each cell of the pattern around the holder, so the zone can be seen while placing it
    [PreviewField(75)]
    public GameObject cellPrefab;
}

public class ShareHealOnRelativeCellBuff : ABuff<ShareHealOnRelativeCellBuffData>
{
    Entity _owner;
    List<GameObject> _cells = new List<GameObject>();

    public override void Instant(GameObject source, GameObject target) { }

    public override void Add(GameObject source, GameObject target)
    {
        _owner = target.GetComponent<Entity>();
        _owner.OnHealReceived.AddListener(OnHealReceived);
        ShowCells();
    }

    public override void Remove(GameObject source, GameObject target)
    {
        if (_owner != null)
        {
            _owner.OnHealReceived.RemoveListener(OnHealReceived);
        }

        foreach (GameObject cell in _cells)
        {
            if (cell != null)
            {
                GameObject.Destroy(cell);
            }
        }
        _cells.Clear();
    }

    void ShowCells()
    {
        if (data.cellPrefab == null)
        {
            return;
        }

        float cellSize = GetCellSize();
        foreach (Vector2Int offset in RelativeCellPattern.GetOffsets(data.pattern, data.range))
        {
            Vector3 cellPosition = _owner.transform.position + new Vector3(offset.x * cellSize, 0f, offset.y * cellSize);
            _cells.Add(GameObject.Instantiate(data.cellPrefab, cellPosition, Quaternion.identity, _owner.transform));
        }
    }

    void OnHealReceived(GameObject source, ConsumerResult heal)
    {
        float sharedHeal = heal.value * data.ratio;
        if (sharedHeal <= 0f || sharedHeal < data.minimumSharedHeal)
        {
            return;
        }

        List<Vector2Int> offsets = RelativeCellPattern.GetOffsets(data.pattern, data.range);
        float cellSize = GetCellSize();
        foreach (GameObject ally in GetAllies())
        {
            Entity entity = ally != null ? ally.GetComponent<Entity>() : null;
            if (entity == null || entity == _owner || entity.health == null || entity.health.Value <= 0f)
            {
                continue;
            }
            if (!offsets.Contains(GetCellOffset(_owner.transform.position, ally.transform.position, cellSize)))
            {
                continue;
            }

            // A heal: ignores the armor and the invincibility
            ResourceModifier healModifier = new ResourceModifier { source = _owner.gameObject };
            healModifier.consumers.Add(new RuntimeConsumer(sharedHeal));
            entity.health.AddResourceModifier(healModifier);
        }
    }

    // Offset in cells from one position to another on the ground plane
    public static Vector2Int GetCellOffset(Vector3 from, Vector3 to, float cellSize)
    {
        Vector3 delta = (to - from) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(delta.x), Mathf.RoundToInt(delta.z));
    }

    protected virtual List<GameObject> GetAllies()
    {
        EntityManager entityManager = EntityManager.existingInstance;
        return entityManager != null ? entityManager.GetEntities(_owner.entityType) : new List<GameObject>();
    }

    protected virtual float GetCellSize()
    {
        PlayerBehaviour player = PlayerBehaviour.existingInstance;
        return player != null && player.grid != null ? player.grid.size : 1f;
    }
}

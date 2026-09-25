using System.Collections.Generic;
using UnityEngine;

// Test mode without restriction: place any number of allies/enemies, use every character skill
// without cost or cooldown, get any item, and start/stop the battle at will
public class SandboxGameType : AGameType
{
    [SerializeField] SandboxData _data;
    [SerializeField] SandboxPanel _panel;

    EntityManager _entities;
    bool _isInitialized = false;

    bool _isBattleRunning = false;
    public bool isBattleRunning { get { return _isBattleRunning; } }

    Entity.EntityType _placementSide = Entity.EntityType.Player;
    public Entity.EntityType placementSide { get { return _placementSide; } set { _placementSide = value; } }

    void Start()
    {
        _entities = EntityManager.instance;
    }

    void Update()
    {
        // Wait for the first frame so every manager has been started
        if (!_isInitialized)
        {
            Initialize();
        }
    }

    void Initialize()
    {
        _isInitialized = true;

        PlayerBehaviour.instance.character.hasUnrestrictedSkills = true;
        PlayerBehaviour.instance.Init(_data.CreateCharacterData());

        _panel.Init(this, _data, PlayerBehaviour.instance.character);
    }

    public override void StartGame()
    {
    }

    public override bool IsOver()
    {
        return false;
    }

    public void SelectEntity(EntityData entityData)
    {
        InteractionManager.instance.SetInteraction(new EntityGridInteraction(entityData, _placementSide, true, OnEntitySpawned));
    }

    public void StartRemovingEntities()
    {
        InteractionManager.instance.SetInteraction(new RemoveEntityInteraction());
    }

    void OnEntitySpawned(Entity entity)
    {
        // Entities are disabled at spawn, so the ones placed during a battle must join it
        entity.Enable(_isBattleRunning);
    }

    // Player items are equipped right away, the other ones are given to the entities clicked next
    public void GiveItem(AItemFactory itemFactory)
    {
        AItem item = itemFactory.GetItem();
        if (item.tags.Exists(tag => tag == DataManager.instance.GetTagWithName("Player")))
        {
            PlayerBehaviour.instance.character.inventoryHandler.AddItem(item, -1);
        }
        else
        {
            InteractionManager.instance.SetInteraction(new GiveItemInteraction(itemFactory));
        }
    }

    public void StartBattle()
    {
        InteractionManager.instance.CancelInteraction();
        _isBattleRunning = true;
        EnableAllEntities(true);
        AscensionGameType.OnBattleStart.Invoke();
    }

    // Back to placement: entities keep their health and permanent effects
    public void StopBattle()
    {
        _isBattleRunning = false;
        EnableAllEntities(false);
        PlayerBehaviour.instance.character.Reset();
        ForEachEntity(entity => entity.Reset());
        AscensionGameType.OnRoundEnd.Invoke();
    }

    public void RestoreAll()
    {
        PlayerBehaviour.instance.character.mana.Refill();
        ForEachEntity(entity => entity.health.Refill());
    }

    public void ClearAll()
    {
        InteractionManager.instance.CancelInteraction();
        ForEachEntity(entity => _entities.DestroyEntity(entity.gameObject, entity.entityType));
    }

    void EnableAllEntities(bool isEnabled)
    {
        PlayerBehaviour.instance.character.Enable(isEnabled);
        ForEachEntity(entity => entity.Enable(isEnabled));
    }

    void ForEachEntity(System.Action<Entity> action)
    {
        // Copy, the action can remove entities from the lists
        List<GameObject> entities = new List<GameObject>(_entities.GetEntities(Entity.EntityType.Player));
        entities.AddRange(_entities.GetEntities(Entity.EntityType.Computer));
        foreach (GameObject entity in entities)
        {
            action(entity.GetComponent<Entity>());
        }
    }
}

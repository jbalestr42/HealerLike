using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerBehaviour : Singleton<PlayerBehaviour>
{
    public UnityEvent<int> OnGoldChanged = new UnityEvent<int>();
    public UnityEvent<int> OnLifeChanged = new UnityEvent<int>();

    [SerializeField] GridManager _grid;
    public GridManager grid { get { return _grid; } set { _grid = value; } }

    [SerializeField] GridGenerator _gridGenerator;
    public GridGenerator gridGenerator { get { return _gridGenerator; } set { _gridGenerator = value; } }

    Renderer _renderer;

    int _gold;
    public int gold
    {
        get { return _gold; } 
        set
        {
            _gold = value;
            OnGoldChanged.Invoke(_gold);
        }
    }


    int _life;
    public int life
    {
        get { return _life; } 
        set
        {
            _life = value;
            OnLifeChanged.Invoke(_life);
        }
    }

    void Start()
    {
        _grid.Generate();
        Generate(6240090);
        EntityManager.instance.OnEnemyKilled.AddListener(OnEnemyKilled);

        life = DataManager.instance.data.life;
        gold = DataManager.instance.data.gold;

        // Register to static events
        Tower.OnTowerSold.AddListener(OnTowerSold);
    }

    void OnTowerSold(Tower tower)
    {
        PlayerBehaviour.instance.gold += (int)(tower.data.price * DataManager.instance.data.refoundFactor);
        PlayerInventory playerInventory = UIManager.instance.GetView<GameView>(ViewType.Game).playerInventory;
        playerInventory.inventory.inventoryHandler.TransfertItems(tower.inventoryHandler);
        grid.SetWalkable(grid.GetCoordFromPosition(tower.transform.position), true);
    }

    void OnEnemyKilled(Enemy enemy, bool hasReachedEnd)
    {
        if (hasReachedEnd)
        {
            life -= enemy.data.lifeCost;
        }
        else
        {
            gold += enemy.data.income;
        }
    }

    public void Generate(int seed = 42)
    {
        _gridGenerator.Generate(_grid, transform, seed);
    }

    public bool HasEnoughGold(int value)
    {
        return _gold >= value;
    }
}
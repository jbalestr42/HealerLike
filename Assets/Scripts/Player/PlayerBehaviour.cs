using UnityEngine;
using UnityEngine.Events;

public class PlayerBehaviour : Singleton<PlayerBehaviour>
{
    public UnityEvent<int> OnGoldChanged = new UnityEvent<int>();

    [SerializeField] GridManager _grid;
    public GridManager grid { get { return _grid; } set { _grid = value; } }

    [SerializeField] GridGenerator _gridGenerator;
    public GridGenerator gridGenerator { get { return _gridGenerator; } set { _gridGenerator = value; } }

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

    void Start()
    {
        _grid.Generate();

        gold = DataManager.instance.data.gold;
    }

    public bool HasEnoughGold(int value)
    {
        return _gold >= value;
    }
}
using UnityEngine;

public class MarkManager : Singleton<MarkManager>
{
    GameView _gameView;
    GameObject _markedEnemy;

    void Start()
    {
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _gameView.gameHUD.markEnemyToggle.onValueChanged.AddListener(OnEnemyMarkClicked);
        EntityManager.instance.OnEnemyKilled.AddListener(OnEnemyKilled);
    }

    void OnEnemyMarkClicked(bool isOn)
    {
        if (!isOn)
        {
            InteractionManager.instance.SetInteraction(new MarkEnemyInteraction());
        }
        else
        {
            UnMarkEnemy();
            InteractionManager.instance.CancelInteraction();
        }
    }

    void OnEnemyKilled(Enemy enemy, bool hasReachedEnd)
    {
        if (IsEnemyMarked(enemy.gameObject))
        {
            ResetMark();
        }
    }

    public bool IsEnemyMarked(GameObject enemy)
    {
        return enemy == _markedEnemy;
    }

    public void MarkEnemy(GameObject markedEnemy)
    {
        if (markedEnemy.GetComponent<IMarkable>() != null)
        {
            if (_markedEnemy != null)
            {
                _markedEnemy.GetComponent<IMarkable>().UnMark();
            }
            _markedEnemy = markedEnemy;
            _markedEnemy.GetComponent<IMarkable>().Mark();
        }
    }

    public void UnMarkEnemy()
    {
        if (_markedEnemy != null)
        {
            _markedEnemy.GetComponent<IMarkable>().UnMark();
            _markedEnemy = null;
        }
    }

    public void ResetMark()
    {
        _markedEnemy = null;
        _gameView.gameHUD.markEnemyToggle.SetIsOnWithoutNotify(true);
    }
}
using UnityEngine;

public class MarkManager : Singleton<MarkManager>
{
    GameView _gameView;
    GameObject _markedEntity;

    void Start()
    {
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _gameView.gameHUD.markEnemyToggle.onValueChanged.AddListener(OnEnemyMarkClicked);
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityKilled);
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

    void OnEntityKilled(Entity entity)
    {
        if (IsEnemyMarked(entity.gameObject))
        {
            ResetMark();
        }
    }

    public bool IsEnemyMarked(GameObject entity)
    {
        return entity == _markedEntity;
    }

    public void MarkEnemy(GameObject markedEnemy)
    {
        if (markedEnemy.GetComponent<IMarkable>() != null)
        {
            if (_markedEntity != null)
            {
                _markedEntity.GetComponent<IMarkable>().UnMark();
            }
            _markedEntity = markedEnemy;
            _markedEntity.GetComponent<IMarkable>().Mark();
        }
    }

    public void UnMarkEnemy()
    {
        if (_markedEntity != null)
        {
            _markedEntity.GetComponent<IMarkable>().UnMark();
            _markedEntity = null;
        }
    }

    public void ResetMark()
    {
        _markedEntity = null;
        _gameView.gameHUD.markEnemyToggle.SetIsOnWithoutNotify(true);
    }
}
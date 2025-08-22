using UnityEngine;

public class MarkManager : Singleton<MarkManager>
{
    GameView _gameView;
    GameObject _markedEntity;

    void Start()
    {
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _gameView.gameHUD.markEntityToggle.onValueChanged.AddListener(OnEntityMarkClicked);
        EntityManager.instance.OnEntityKilled.AddListener(OnEntityKilled);
    }

    void OnEntityMarkClicked(bool isOn)
    {
        if (!isOn)
        {
            InteractionManager.instance.SetInteraction(new MarkEntityInteraction());
        }
        else
        {
            UnMarkEntity();
            InteractionManager.instance.CancelInteraction();
        }
    }

    void OnEntityKilled(Entity entity)
    {
        if (IsEntityMarked(entity.gameObject))
        {
            ResetMark();
        }
    }

    public bool IsEntityMarked(GameObject entity)
    {
        return entity == _markedEntity;
    }

    public void MarkEntity(GameObject markedEntity)
    {
        if (markedEntity.GetComponent<IMarkable>() != null)
        {
            if (_markedEntity != null)
            {
                _markedEntity.GetComponent<IMarkable>().UnMark();
            }
            _markedEntity = markedEntity;
            _markedEntity.GetComponent<IMarkable>().Mark();
        }
    }

    public void UnMarkEntity()
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
        _gameView.gameHUD.markEntityToggle.SetIsOnWithoutNotify(true);
    }
}
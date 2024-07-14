using UnityEngine;

public class SelectableEntity : MonoBehaviour, ISelectable
{
    Outline _outline;
    [SerializeField] Color _playerColor = new Color(0f, 1f, 1f);
    [SerializeField] Color _computerColor = new Color(1f, 0f, 1f);
    [SerializeField] Color _selectedColor = new Color(1f, 1f, 1f);
    [SerializeField] float _lineWidth = 4f;
    Entity.EntityType _entityType;
    bool _isSelected = false;

    void Start()
    {
        _outline = gameObject.AddComponent<Outline>();
        _outline.OutlineWidth = _lineWidth;
        _outline.enabled = false;

        _entityType = GetComponent<Entity>().entityType;
    }

    void OnDestroy()
    {
        if (_isSelected)
        {
            InteractionManager.instance.CancelSelection();
        }
    }

    void OnMouseEnter()
    {
        if (!_isSelected)
        {
            _outline.enabled = true;
            _outline.OutlineColor = _entityType == Entity.EntityType.Player ? _playerColor : _computerColor;
        }
    }

    void OnMouseExit()
    {
        if (!_isSelected)
        {
            _outline.enabled = false;
        }
    }

    #region ISelectable

    public void Select()
    {
        _isSelected = true;
        _outline.enabled = true;
        _outline.OutlineColor = _selectedColor;
        UIManager.instance.GetView<GameView>(ViewType.Game).ShowPanel(PanelType.Entity, gameObject);
    }

    public void UnSelect()
    {
        _isSelected = false;
        _outline.enabled = false;
        UIManager.instance.GetView<GameView>(ViewType.Game).HidePanel(PanelType.Entity);
    }

    #endregion
}

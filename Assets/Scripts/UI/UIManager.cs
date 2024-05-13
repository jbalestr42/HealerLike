using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ViewType
{
    None,
    Game,
    Upgrade,
    Wave,
    GameOver
}

public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    Dictionary<ViewType, AView> _views = new Dictionary<ViewType, AView>();
    ViewType _currentView = ViewType.None;

    List<ViewType> _viewStack = new List<ViewType>();

    #region Views

    public void AddView(ViewType type)
    {
        if (_currentView != ViewType.None)
        {
            _views[_currentView].Hide();
        }

        _currentView = type;
        _viewStack.Add(_currentView);
        _views[_currentView].gameObject.SetActive(true);
        _views[_currentView].Show();
    }

    public void PopCurrentView()
    {
        if (_currentView != ViewType.None)
        {
            _views[_currentView].gameObject.SetActive(false);
            _viewStack.Remove(_currentView);
        }

        _currentView = _viewStack[_viewStack.Count - 1];
        _views[_currentView].Show();
    }

    public T GetView<T>(ViewType type) where T : AView
    {
        return _views[type] as T;
    }

    #endregion
}

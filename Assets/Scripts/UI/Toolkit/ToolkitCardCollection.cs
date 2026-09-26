using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

// Retains one list's bindings; removed cards release every callback before leaving the tree.
public class ToolkitCardCollection : IDisposable
{
    readonly List<ToolkitCard> _cards = new List<ToolkitCard>();
    readonly ToolkitGameView _view;
    readonly VisualElement _parent;
    readonly VisualTreeAsset _template;
    readonly bool _showInfo;
    bool _isValid = true;

    public ToolkitCardCollection(ToolkitGameView view, VisualElement parent, VisualTreeAsset template)
    {
        _view = view;
        _parent = parent;
        _template = template;
        _showInfo = parent.name == "party-list" || parent.name == "spell-list";
    }

    public void Refresh(IReadOnlyList<ToolkitCardModel> models)
    {
        if (!_isValid)
        {
            return;
        }

        while (_cards.Count > models.Count)
        {
            _cards[_cards.Count - 1].Dispose();
            _cards.RemoveAt(_cards.Count - 1);
        }

        for (int i = 0; i < models.Count; i++)
        {
            if (i == _cards.Count)
            {
                ToolkitCard card = new ToolkitCard();
                card.Init(_view, _template);
                if (card.root == null)
                {
                    _isValid = false;
                    return;
                }

                _parent.Add(card.root);
                card.ShowInfo(_showInfo);
                _cards.Add(card);
            }

            _cards[i].Refresh(models[i]);
        }
    }

    public void RefreshIcons()
    {
        foreach (ToolkitCard card in _cards)
        {
            card.RefreshIcon();
        }
    }

    public void Dispose()
    {
        foreach (ToolkitCard card in _cards)
        {
            card.Dispose();
        }

        _cards.Clear();
        _isValid = false;
    }
}

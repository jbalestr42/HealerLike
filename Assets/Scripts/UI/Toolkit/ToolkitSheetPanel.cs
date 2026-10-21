using System;
using UnityEngine;
using UnityEngine.UIElements;

// The healer's sheet: what Julien's CharacterInfoPanel shows (mana, stats with their modifiers, items and active
// effects), which the Toolkit hides along with his canvas. His own text is used as it is, only its markup is
// normalised for the label. Opened from the header, closed by its button or Escape, refreshed with the host.
public sealed class ToolkitSheetPanel : IDisposable
{
    const string OpenButtonName = "sheet-button";
    const string CloseButtonName = "sheet-close-button";
    const string BodyName = "sheet-body";
    const string PanelName = "sheet-panel";
    const string TextName = "sheet-text";

    ToolkitGameContext _context;
    ToolkitGameView _view;
    Action _onChanged;
    ScrollView _body;
    Label _text;
    string _source;
    float _sourceSize;
    string _failure;

    // onChanged is the host's Refresh, so the other panels follow an open or a close in the same frame
    public void Init(ToolkitGameContext context, ToolkitGameView view, Action onChanged)
    {
        Dispose();
        _context = context;
        _view = view;
        _onChanged = onChanged;
        if (
            !ToolkitTemplates.Require(view.root, BodyName, out _body)
            || !ToolkitTemplates.Require(view.root, TextName, out _text)
            || !ToolkitTemplates.Require(view.root, OpenButtonName, out Button _)
            || !ToolkitTemplates.Require(view.root, CloseButtonName, out Button _)
        )
        {
            Dispose();
            return;
        }

        _text.enableRichText = true;
        view.AddClickListener(OpenButtonName, Open);
        view.AddClickListener(CloseButtonName, Close);
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _view.RemoveClickListener(OpenButtonName, Open);
            _view.RemoveClickListener(CloseButtonName, Close);
        }

        _context = null;
        _view = null;
        _onChanged = null;
        _body = null;
        _text = null;
        _source = null;
        _failure = null;
    }

    public void Refresh()
    {
        if (_context == null || _view == null)
        {
            return;
        }

        Character character = GetCharacter();
        bool isAvailable = !_context.isMenu && _context.IsCurrentView(ViewType.Game) && character != null;
        if (!isAvailable)
        {
            _context.isSheetOpen = false;
        }

        _view.Show(OpenButtonName, isAvailable);
        _view.SetButton(OpenButtonName, null, isAvailable && !_context.isPaused && !_context.isInventoryOpen);
        bool isVisible = _context.isSheetOpen && !_context.isPaused;
        _view.Show(PanelName, isVisible);
        if (isVisible)
        {
            SetBody(character);
        }
    }

    public void Close()
    {
        if (_context == null)
        {
            return;
        }

        _context.isSheetOpen = false;
        Changed();
    }

    void Open()
    {
        if (!CanOpen())
        {
            return;
        }

        _context.isSheetOpen = true;
        _context.isInventoryOpen = false;
        _body.scrollOffset = Vector2.zero;
        Changed();
    }

    bool CanOpen()
    {
        return _context != null
            && !_context.isMenu
            && !_context.isPaused
            && _context.IsCurrentView(ViewType.Game)
            && GetCharacter() != null;
    }

    Character GetCharacter()
    {
        return _context.player != null ? _context.player.character : null;
    }

    void Changed()
    {
        if (_onChanged != null)
        {
            _onChanged();
        }
        else
        {
            Refresh();
        }
    }

    void SetBody(Character character)
    {
        string source = BuildSource(character);
        float size = _text.resolvedStyle.fontSize;
        if (source == _source && Mathf.Approximately(size, _sourceSize))
        {
            return;
        }

        _source = source;
        _sourceSize = size;
        _view.SetText(TextName, ToolkitRichText.Normalise(source, size));
    }

    // Julien's text, or a muted line when his formatter cannot run (a character not yet initialised, or a data
    // problem inside his code, logged once per distinct message)
    string BuildSource(Character character)
    {
        if (character.attributeManager == null)
        {
            return Muted("Character not ready");
        }

        try
        {
            string body = CharacterInfoPanel.BuildBody(character);
            _failure = null;
            return body;
        }
        catch (Exception exception)
        {
            if (_failure != exception.Message)
            {
                _failure = exception.Message;
                Debug.LogError("[ToolkitSheetPanel] CharacterInfoPanel.BuildBody failed: " + exception);
            }

            return Muted("Sheet unavailable");
        }
    }

    static string Muted(string text)
    {
        return $"<color={EntityInfoFormatter.MutedColor}>{text}</color>";
    }
}

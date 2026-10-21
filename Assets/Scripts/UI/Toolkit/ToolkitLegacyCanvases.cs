using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hides the legacy screen canvases and their raycasters, and restores their previous state
public class ToolkitLegacyCanvases
{
    Dictionary<Canvas, bool> _canvases = new Dictionary<Canvas, bool>();
    Dictionary<GraphicRaycaster, bool> _raycasters = new Dictionary<GraphicRaycaster, bool>();
    List<Canvas> _eventCanvases = new List<Canvas>();

    // The legacy menu controller opens its own character select from Start; the Toolkit menu replaces that
    // screen, so the controller is switched off before any Start of its scene runs. It is not switched back on:
    // its Start would open a screen the Toolkit scenes are not wired for.
    public static void SilenceMenus()
    {
        foreach (MainMenu menu in Object.FindObjectsByType<MainMenu>())
        {
            menu.enabled = false;
        }
    }

    public void Hide(ToolkitGameContext context)
    {
        // Scope to the legacy screen views, the world-space health bars and floating combat text stay
        if (context.ui != null)
        {
            HideScreenCanvases(context.ui.gameObject);
        }
        else
        {
            foreach (MainMenu menu in Object.FindObjectsByType<MainMenu>())
            {
                HideScreenCanvases(menu.gameObject);
            }
        }

        if (!context.isMenu)
        {
            return;
        }

        // Some scenes keep the menu controller beneath its screen Canvas
        foreach (MainMenu menu in Object.FindObjectsByType<MainMenu>())
        {
            Canvas canvas = menu.GetComponentInParent<Canvas>();
            if (canvas != null && !_canvases.ContainsKey(canvas))
            {
                HideCanvas(canvas);
            }
        }
    }

    public void Restore()
    {
        foreach (KeyValuePair<Canvas, bool> pair in _canvases)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        foreach (KeyValuePair<GraphicRaycaster, bool> pair in _raycasters)
        {
            if (pair.Key != null)
            {
                pair.Key.enabled = pair.Value;
            }
        }

        _canvases.Clear();
        _raycasters.Clear();
        _eventCanvases.Clear();
    }

    // The event and rest choice screen is drawn by ToolkitEventPanel, but only while it has a card to press, so
    // his canvas is recorded here and left as the scene had it. The panel hides it with SetEventScreenVisible(false)
    // once its cards are up. The default is his screen: when our HUD does not get as far as the panel, the room
    // still has a way to choose and the run cannot stall behind a hidden canvas.
    void HideScreenCanvases(GameObject root)
    {
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            if (canvas.GetComponentInParent<EventView>(true) != null)
            {
                Remember(canvas);
                if (!_eventCanvases.Contains(canvas))
                {
                    _eventCanvases.Add(canvas);
                }

                continue;
            }

            HideCanvas(canvas);
        }
    }

    // False hides his event canvas and its raycasters, true brings them back to the state Hide found them in.
    // Does nothing for a canvas Hide has not seen.
    public void SetEventScreenVisible(bool visible)
    {
        foreach (Canvas canvas in _eventCanvases)
        {
            if (canvas == null)
            {
                continue;
            }

            bool wasEnabled;
            canvas.enabled = visible && _canvases.TryGetValue(canvas, out wasEnabled) && wasEnabled;
            foreach (GraphicRaycaster raycaster in canvas.GetComponents<GraphicRaycaster>())
            {
                bool raycasterWasEnabled;
                raycaster.enabled = visible && _raycasters.TryGetValue(raycaster, out raycasterWasEnabled)
                    && raycasterWasEnabled;
            }
        }
    }

    void Remember(Canvas canvas)
    {
        if (!_canvases.ContainsKey(canvas))
        {
            _canvases.Add(canvas, canvas.enabled);
        }

        foreach (GraphicRaycaster raycaster in canvas.GetComponents<GraphicRaycaster>())
        {
            if (!_raycasters.ContainsKey(raycaster))
            {
                _raycasters.Add(raycaster, raycaster.enabled);
            }
        }
    }

    void HideCanvas(Canvas canvas)
    {
        Remember(canvas);
        canvas.enabled = false;
        foreach (GraphicRaycaster raycaster in canvas.GetComponents<GraphicRaycaster>())
        {
            raycaster.enabled = false;
        }
    }
}

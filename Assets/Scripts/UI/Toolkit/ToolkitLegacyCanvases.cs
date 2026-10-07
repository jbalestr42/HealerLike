using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Hides the legacy screen canvases and their raycasters, and restores their previous state
public class ToolkitLegacyCanvases
{
    Dictionary<Canvas, bool> _canvases = new Dictionary<Canvas, bool>();
    Dictionary<GraphicRaycaster, bool> _raycasters = new Dictionary<GraphicRaycaster, bool>();

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
    }

    // The game's event and rest choice screen has no Toolkit counterpart yet, so it keeps its own canvas:
    // hiding it would leave those rooms with no way to choose and the run stuck
    void HideScreenCanvases(GameObject root)
    {
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.renderMode != RenderMode.WorldSpace && canvas.GetComponentInParent<EventView>(true) == null)
            {
                HideCanvas(canvas);
            }
        }
    }

    void HideCanvas(Canvas canvas)
    {
        if (!_canvases.ContainsKey(canvas))
        {
            _canvases.Add(canvas, canvas.enabled);
        }

        canvas.enabled = false;
        foreach (GraphicRaycaster raycaster in canvas.GetComponents<GraphicRaycaster>())
        {
            if (!_raycasters.ContainsKey(raycaster))
            {
                _raycasters.Add(raycaster, raycaster.enabled);
            }

            raycaster.enabled = false;
        }
    }
}

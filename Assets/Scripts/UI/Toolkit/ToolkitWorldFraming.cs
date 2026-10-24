using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// Keeps the board in the part of the screen the HUD leaves free: the scene's camera composers aim at their authored
// screen point inside that area instead of the whole screen
public class ToolkitWorldFraming
{
#pragma warning disable CS0618 // The scene's FreeLook rigs still use the legacy composer
    readonly Dictionary<CinemachineComposer, Vector2> _authored = new Dictionary<CinemachineComposer, Vector2>();
    Rect _viewport;

    public void Update(Rect viewport)
    {
        if (viewport.width < 0.1f || viewport.height < 0.1f || viewport == _viewport)
        {
            return;
        }

        _viewport = viewport;
        foreach (CinemachineComposer composer in Object.FindObjectsByType<CinemachineComposer>())
        {
            if (!_authored.TryGetValue(composer, out Vector2 authored))
            {
                authored = new Vector2(composer.m_ScreenX, composer.m_ScreenY);
                _authored.Add(composer, authored);
            }

            Vector2 point = ScreenPoint(authored, viewport);
            composer.m_ScreenX = point.x;
            composer.m_ScreenY = point.y;
        }
    }

    // Puts the composers back on their authored points, the HUD no longer covers the screen
    public void Restore()
    {
        foreach (KeyValuePair<CinemachineComposer, Vector2> pair in _authored)
        {
            if (pair.Key != null)
            {
                pair.Key.m_ScreenX = pair.Value.x;
                pair.Key.m_ScreenY = pair.Value.y;
            }
        }

        _authored.Clear();
        _viewport = default;
    }
#pragma warning restore CS0618

    // authored: a composer point, from the top left of the screen; viewport: the free area, normalized from the
    // bottom left as a camera rect. The same point, from the top left of the screen, placed in the free area
    public static Vector2 ScreenPoint(Vector2 authored, Rect viewport)
    {
        float top = 1f - viewport.yMax;
        return new Vector2(viewport.xMin + authored.x * viewport.width, top + authored.y * viewport.height);
    }
}

using UnityEditor;
using UnityEngine;

// The board drawn as a grid seen from above, the column 0 on the left and the row 0 at the bottom: pick a brush (None,
// Player or Enemy) then click or drag over the cells to paint them. The size changes keep each cell at its place
[CustomEditor(typeof(BoardLayout))]
public class BoardLayoutEditor : Editor
{
    const float CellPixels = 40f;
    const float CellGap = 2f;
    const string BrushPrefsKey = "HealerLike.BoardLayoutEditor.Brush";

    static readonly Color NoneColor = new Color(0f, 0f, 0f, 0.25f);
    static readonly Color PlayerColor = new Color(0.25f, 0.5f, 0.95f, 0.85f);
    static readonly Color EnemyColor = new Color(0.9f, 0.3f, 0.25f, 0.85f);
    static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.25f);
    static readonly string[] BrushNames = { "None", "Player", "Enemy" };

    // The cell last painted during a drag, so a cell is painted once per pass
    Vector2Int _lastPainted = new Vector2Int(-1, -1);
    // The cell under the mouse at the last move, (-1, -1) outside the grid
    Vector2Int _hovered = new Vector2Int(-1, -1);

    static BoardCellType brush
    {
        get { return (BoardCellType)EditorPrefs.GetInt(BrushPrefsKey, (int)BoardCellType.Player); }
        set { EditorPrefs.SetInt(BrushPrefsKey, (int)value); }
    }

    public override void OnInspectorGUI()
    {
        BoardLayout layout = (BoardLayout)target;

        EditorGUI.BeginChangeCheck();
        int width = EditorGUILayout.DelayedIntField("Width", layout.width);
        int height = EditorGUILayout.DelayedIntField("Height", layout.height);
        float cellSize = EditorGUILayout.FloatField(new GUIContent("Cell Size", "Size of a cell in the world"), layout.cellSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(layout, "Change Board Size");
            if (width != layout.width || height != layout.height)
            {
                layout.Resize(width, height);
            }
            layout.cellSize = cellSize;
            EditorUtility.SetDirty(layout);
        }

        EditorGUILayout.Space();
        brush = (BoardCellType)GUILayout.Toolbar((int)brush, BrushNames);
        EditorGUILayout.HelpBox("Click or drag over the cells to paint them with the brush. Column 0 on the left, row 0 at the bottom.", MessageType.None);

        DrawGrid(layout);

        EditorGUILayout.LabelField($"Player cells: {layout.GetCells(BoardCellType.Player).Count}    Enemy cells: {layout.GetCells(BoardCellType.Enemy).Count}");
    }

    void DrawGrid(BoardLayout layout)
    {
        float step = CellPixels + CellGap;
        Rect area = GUILayoutUtility.GetRect(layout.width * step, layout.height * step, GUILayout.ExpandWidth(false));
        Event current = Event.current;
        Vector2Int hovered = GetCoord(layout, area, current.mousePosition, step);

        if (current.type == EventType.Repaint)
        {
            for (int x = 0; x < layout.width; x++)
            {
                for (int y = 0; y < layout.height; y++)
                {
                    Rect cell = GetCellRect(layout, area, x, y, step);
                    EditorGUI.DrawRect(cell, GetColor(layout.GetCell(x, y)));
                    if (hovered == new Vector2Int(x, y))
                    {
                        EditorGUI.DrawRect(cell, HoverColor);
                    }
                }
            }
        }

        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        switch (current.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (current.button == 0 && layout.IsValid(hovered))
                {
                    GUIUtility.hotControl = controlId;
                    _lastPainted = new Vector2Int(-1, -1);
                    Paint(layout, hovered);
                    current.Use();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId)
                {
                    if (layout.IsValid(hovered))
                    {
                        Paint(layout, hovered);
                    }
                    current.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                }
                break;
        }
        RepaintOnHover(area, hovered);
    }

    // The inspector repaints when the mouse goes from a cell to another, for the hovered cell to light up at once
    // rather than at the next repaint: it is asked the mouse moves when the mouse is over the grid
    void RepaintOnHover(Rect area, Vector2Int hovered)
    {
        Event current = Event.current;
        EditorWindow window = EditorWindow.mouseOverWindow;
        if (window != null && area.Contains(current.mousePosition) && !window.wantsMouseMove)
        {
            window.wantsMouseMove = true;
        }
        if (current.type == EventType.MouseMove && hovered != _hovered)
        {
            _hovered = hovered;
            Repaint();
        }
    }

    void Paint(BoardLayout layout, Vector2Int coord)
    {
        if (coord == _lastPainted)
        {
            return;
        }
        _lastPainted = coord;
        if (layout.GetCell(coord) == brush)
        {
            return;
        }

        Undo.RecordObject(layout, "Paint Board Cell");
        layout.SetCell(coord.x, coord.y, brush);
        EditorUtility.SetDirty(layout);
        Repaint();
    }

    // The row 0 at the bottom of the area
    static Rect GetCellRect(BoardLayout layout, Rect area, int x, int y, float step)
    {
        return new Rect(area.x + x * step, area.y + (layout.height - 1 - y) * step, CellPixels, CellPixels);
    }

    // (-1, -1) outside the cells
    static Vector2Int GetCoord(BoardLayout layout, Rect area, Vector2 mouse, float step)
    {
        if (!area.Contains(mouse))
        {
            return new Vector2Int(-1, -1);
        }
        int x = Mathf.FloorToInt((mouse.x - area.x) / step);
        int y = layout.height - 1 - Mathf.FloorToInt((mouse.y - area.y) / step);
        return layout.IsValid(x, y) ? new Vector2Int(x, y) : new Vector2Int(-1, -1);
    }

    static Color GetColor(BoardCellType type)
    {
        switch (type)
        {
            case BoardCellType.Player:
                return PlayerColor;
            case BoardCellType.Enemy:
                return EnemyColor;
            default:
                return NoneColor;
        }
    }
}

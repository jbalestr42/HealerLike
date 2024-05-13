using UnityEngine;

public static class UIUtility
{
    public static Vector2 GetWorldPositionOfCanvasElement(Camera camera, RectTransform element)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(element, element.position, camera, out Vector3 position);
        return position;
    }
}
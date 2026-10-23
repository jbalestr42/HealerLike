using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // 0 pauses the game, 1 is the normal speed
    public static void ChangeTimeScale(float scale)
    {
        Time.timeScale = scale;
    }
}

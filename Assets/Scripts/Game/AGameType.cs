using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AGameType : MonoBehaviour
{
    public abstract void StartGame();
    public abstract bool IsOver();
}
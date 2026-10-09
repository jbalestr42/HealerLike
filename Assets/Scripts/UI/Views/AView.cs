using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AView : Sirenix.OdinInspector.SerializedMonoBehaviour
{
    public abstract void Show();
    public abstract void Hide();
}
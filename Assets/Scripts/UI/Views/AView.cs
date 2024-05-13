using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class AView : SerializedMonoBehaviour
{
    public abstract void Show();
    public abstract void Hide();
}
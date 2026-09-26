using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace UI.Toolkit
{

public class ToolkitTestPanel : IDisposable
{
    readonly EditorWindow _window;
    public VisualElement root
    {
        get { return _window.rootVisualElement; }
    }

    public ToolkitTestPanel()
    {
        _window = ScriptableObject.CreateInstance<EditorWindow>();
        _window.Show();
    }

    public static void Submit(Button button)
    {
        using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
        {
            button.SendEvent(submit);
        }
    }

    public void Dispose()
    {
        _window.Close();
    }
}
}

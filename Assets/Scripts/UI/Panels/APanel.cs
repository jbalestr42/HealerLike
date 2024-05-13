using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class APanel : MonoBehaviour
{
    [SerializeField]
    private GameObject _panel;

    public void ShowUI(GameObject selectedObject)
    {
        if (!IsActive())
        {
            _panel.SetActive(true);
        }
        OnShowUI(selectedObject);
        UpdateUI(selectedObject);
    }

    public virtual void OnShowUI(GameObject selectedObject) {}
    public abstract void UpdateUI(GameObject selectedObject);
    public virtual void OnHideUI(GameObject selectedObject) {}

    public void HideUI(GameObject selectedObject)
    {
        OnHideUI(selectedObject);
        _panel.SetActive(false);
    }

    public bool IsActive()
    {
        return _panel.activeInHierarchy;
    }
}
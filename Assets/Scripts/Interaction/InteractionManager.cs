using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionManager : Singleton<InteractionManager>
{
    AInteraction _interaction = null;
    ISelectable _selectable = null;
    bool _isOver = false;
    Ray ray;
    RaycastHit hit;

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (_interaction != null) 
            {
                UpdateInteraction();
            } 
            else 
            {
                if (Input.GetMouseButtonDown(0))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                    {
                        var interactable = hit.collider.gameObject.GetComponentInParent<ISelectable>();

                        if (_selectable != interactable)
                        {
                            CancelSelection();
                            Select(interactable);
                        }
                    }
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelSelection();
                }
            }
        }
    }

    public void UpdateInteraction()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _interaction.GetLayerMask()))
        {
            if (!_isOver)
            {
                _isOver = true;
                _interaction.OnMouseEnter(hit);
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _interaction.OnMouseClick(hit);
                }
                else
                {
                    _interaction.OnMouseOver(hit);
                }
            }
        }
        else if (_isOver)
        {
            _isOver = false;
            _interaction.OnMouseExit();
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelInteraction();
        }
    }

    public AInteraction GetInteraction()
    {
        return _interaction;
    }

    public void SetInteraction(AInteraction interaction)
    {
        CancelSelection();
        CancelInteraction();
        _interaction = interaction;
    }

    public void CancelInteraction()
    {
        if (_interaction != null)
        {
            _interaction.Cancel();
            _interaction = null;
        }
    }

    public void EndInteraction()
    {
        if (_interaction != null)
        {
            _interaction.End();
            _interaction = null;
        }
    }

    public void Select(ISelectable selectable)
    {
        _selectable = selectable;
        if (_selectable != null)
        {
            _selectable.Select();
        }
    }

    public void CancelSelection()
    {
        if (_selectable != null)
        {
            _selectable.UnSelect();
            _selectable = null;
        }
    }
}
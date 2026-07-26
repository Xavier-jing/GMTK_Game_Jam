using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class ScreenBase : MonoBehaviour
{
    [SerializeField]
    private ScreenId screenId;

    [SerializeField]
    private Selectable firstSelected;

    public ScreenId Id => screenId;

    public UIService Owner { get; private set; }

    public bool IsVisible { get; private set; }

    public void SetScreenId(ScreenId value)
    {
        screenId = value;
    }

    public void Show()
    {
        if (IsVisible)
        {
            UISfxFeedback.EnsureInChildren(this);
            SelectDefaultControl();
            return;
        }

        gameObject.SetActive(true);
        IsVisible = true;
        OnShow();
        UISfxFeedback.EnsureInChildren(this);
        SelectDefaultControl();
    }

    public void Hide()
    {
        if (!IsVisible && !gameObject.activeSelf)
        {
            return;
        }

        ClearSelection();
        IsVisible = false;
        OnHide();
        gameObject.SetActive(false);
    }

    internal void Bind(UIService owner)
    {
        Owner = owner;
    }

    protected virtual void OnShow()
    {
    }

    protected virtual void OnHide()
    {
    }

    private void SelectDefaultControl()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        Selectable target = firstSelected;
        if (target == null || !target.gameObject.activeInHierarchy || !target.IsInteractable())
        {
            Selectable[] candidates = GetComponentsInChildren<Selectable>(true);
            foreach (Selectable candidate in candidates)
            {
                if (candidate.gameObject.activeInHierarchy && candidate.IsInteractable())
                {
                    target = candidate;
                    break;
                }
            }
        }

        if (target == null)
        {
            return;
        }

        eventSystem.SetSelectedGameObject(null);
        target.Select();
    }

    private void ClearSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        if (selected != null && selected.transform.IsChildOf(transform))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}

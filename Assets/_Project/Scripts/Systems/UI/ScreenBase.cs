using UnityEngine;

public abstract class ScreenBase : MonoBehaviour
{
    [SerializeField]
    private ScreenId screenId;

    public ScreenId Id => screenId;

    public UIService Owner { get; private set; }

    public bool IsVisible { get; private set; }

    public void SetScreenId(ScreenId value)
    {
        screenId = value;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        IsVisible = true;
        OnShow();
    }

    public void Hide()
    {
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
}

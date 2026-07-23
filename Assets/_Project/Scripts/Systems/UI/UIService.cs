using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class UIService : MonoBehaviour
{
    private readonly Dictionary<ScreenId, ScreenBase> screens = new Dictionary<ScreenId, ScreenBase>();

    public static UIService Current { get; private set; }

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Destroy(gameObject);
            return;
        }

        Current = this;
        RegisterScreensInChildren();

        if (AppContext.HasInstance)
        {
            AppContext.Instance.Services.Register(this);
        }
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }

        if (AppContext.HasInstance)
        {
            AppContext.Instance.Services.Unregister(this);
        }
    }

    public void Register(ScreenBase screen)
    {
        if (screen == null)
        {
            return;
        }

        if (screens.TryGetValue(screen.Id, out ScreenBase existingScreen) && existingScreen != screen)
        {
            Debug.LogWarning(
                $"UIService replaced duplicate screen id '{screen.Id}' on '{existingScreen.name}' with '{screen.name}'.");
        }

        screens[screen.Id] = screen;
        screen.Bind(this);
    }

    public bool TryGet(ScreenId screenId, out ScreenBase screen)
    {
        return screens.TryGetValue(screenId, out screen);
    }

    public TScreen Get<TScreen>(ScreenId screenId) where TScreen : ScreenBase
    {
        if (!TryGet(screenId, out ScreenBase screen))
        {
            throw new InvalidOperationException($"Screen '{screenId}' has not been registered.");
        }

        TScreen typedScreen = screen as TScreen;
        if (typedScreen == null)
        {
            throw new InvalidOperationException(
                $"Screen '{screenId}' is '{screen.GetType().Name}', not '{typeof(TScreen).Name}'.");
        }

        return typedScreen;
    }

    public void Show(ScreenId screenId, bool hideOthers = true)
    {
        if (hideOthers)
        {
            HideAllExcept(screenId);
        }

        Get<ScreenBase>(screenId).Show();
    }

    public void Hide(ScreenId screenId)
    {
        if (TryGet(screenId, out ScreenBase screen))
        {
            screen.Hide();
        }
    }

    public void HideAll()
    {
        foreach (ScreenBase screen in screens.Values)
        {
            screen.Hide();
        }
    }

    private void HideAllExcept(ScreenId visibleScreenId)
    {
        foreach (KeyValuePair<ScreenId, ScreenBase> pair in screens)
        {
            if (pair.Key != visibleScreenId)
            {
                pair.Value.Hide();
            }
        }
    }

    private void RegisterScreensInChildren()
    {
        ScreenBase[] childScreens = GetComponentsInChildren<ScreenBase>(true);
        foreach (ScreenBase screen in childScreens)
        {
            Register(screen);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public sealed class UISfxFeedback :
    MonoBehaviour,
    IPointerEnterHandler,
    ISelectHandler,
    IPointerClickHandler,
    ISubmitHandler
{
    private const float RepeatGuardSeconds = 0.05f;

    private Selectable selectable;
    private Button button;
    private float lastSelectionTime = float.NegativeInfinity;
    private float lastClickTime = float.NegativeInfinity;

    public static void Ensure(Selectable target)
    {
        if (target == null || target.GetComponent<UISfxFeedback>() != null)
        {
            return;
        }

        target.gameObject.AddComponent<UISfxFeedback>();
    }

    public static void EnsureInChildren(Component root)
    {
        if (root == null)
        {
            return;
        }

        Selectable[] controls = root.GetComponentsInChildren<Selectable>(true);
        for (int index = 0; index < controls.Length; index++)
        {
            Ensure(controls[index]);
        }
    }

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        button = selectable as Button;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData == null || !eventData.dragging)
        {
            PlaySelectionFeedback();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Pointer hover has its own callback. Ignoring pointer-originated selection
        // prevents a hover followed by a click from playing UI_Select twice.
        if (eventData is PointerEventData)
        {
            return;
        }

        PlaySelectionFeedback();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null &&
            eventData.button == PointerEventData.InputButton.Left)
        {
            PlayClickFeedback();
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickFeedback();
    }

    private void PlaySelectionFeedback()
    {
        if (!CanPlayFeedback() ||
            Time.unscaledTime - lastSelectionTime < RepeatGuardSeconds)
        {
            return;
        }

        lastSelectionTime = Time.unscaledTime;
        UISfxPlayer.Play(UISfxPlayer.SelectSfxId, this);
    }

    private void PlayClickFeedback()
    {
        if (button == null ||
            !CanPlayFeedback() ||
            Time.unscaledTime - lastClickTime < RepeatGuardSeconds)
        {
            return;
        }

        lastClickTime = Time.unscaledTime;
        UISfxPlayer.Play(UISfxPlayer.ClickSfxId, this);
    }

    private bool CanPlayFeedback()
    {
        return isActiveAndEnabled &&
               selectable != null &&
               selectable.gameObject.activeInHierarchy &&
               selectable.IsInteractable();
    }
}

internal static class UISfxPlayer
{
    public const string SelectSfxId = "UI_Select";
    public const string ClickSfxId = "UI_Click";

    private static readonly HashSet<string> LoggedErrors =
        new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLoggedErrors()
    {
        LoggedErrors.Clear();
    }

    public static void Play(string audioId, Object context)
    {
        if (!AppContext.HasInstance)
        {
            return;
        }

        AudioService audio = AppContext.Instance.Audio;
        if (audio == null)
        {
            return;
        }

        if (audio.TryPlayUiSfxById(audioId, 1f, out string error))
        {
            LoggedErrors.Remove(audioId);
            return;
        }

        if (LoggedErrors.Add(audioId))
        {
            Debug.LogWarning(
                $"[UISfx] Could not play '{audioId}': {error}",
                context);
        }
    }
}

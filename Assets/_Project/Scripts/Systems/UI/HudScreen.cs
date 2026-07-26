using UnityEngine;
using UnityEngine.UI;

public sealed class HudScreen : ScreenBase
{
    private const string RemainingIncreaseSfxId = "UI_Remaining_Increase";
    private const string RemainingReduceSfxId = "UI_Remaining_Reduce";
    private const string RemainingWarningSfxId = "UI_Warning";

    [SerializeField]
    private Image turnImage;

    [SerializeField]
    [Tooltip("Index 0 displays 1 remaining turn, index 1 displays 2, and so on.")]
    private Sprite[] turnSpritesByValue = new Sprite[5];

    private TurnManager turnManager;
    private int previousRemainingTurns;
    private bool hasRemainingTurnsSnapshot;

    protected override void OnShow()
    {
        UnsubscribeTurnManager();
        turnManager = AppContext.Instance.TurnManager;
        if (turnManager != null)
        {
            previousRemainingTurns = turnManager.RemainingTurns;
            hasRemainingTurnsSnapshot = true;
            turnManager.OnTurnsChanged += HandleTurnsChanged;
        }

        RefreshTurnDisplay();
    }

    protected override void OnHide()
    {
        UnsubscribeTurnManager();
    }

    private void OnDestroy()
    {
        UnsubscribeTurnManager();
    }

    private void UnsubscribeTurnManager()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnsChanged -= HandleTurnsChanged;
            turnManager = null;
        }

        hasRemainingTurnsSnapshot = false;
    }

    private void HandleTurnsChanged(int remaining, int max)
    {
        PlayRemainingTurnsFeedback(remaining);
        RefreshTurnDisplay();
    }

    private void PlayRemainingTurnsFeedback(int remaining)
    {
        if (!hasRemainingTurnsSnapshot)
        {
            previousRemainingTurns = remaining;
            hasRemainingTurnsSnapshot = true;
            return;
        }

        string audioId = string.Empty;
        if (remaining > previousRemainingTurns)
        {
            audioId = RemainingIncreaseSfxId;
        }
        else if (remaining == 1 && previousRemainingTurns > 1)
        {
            audioId = RemainingWarningSfxId;
        }
        else if (remaining > 1 && remaining < previousRemainingTurns)
        {
            audioId = RemainingReduceSfxId;
        }

        previousRemainingTurns = remaining;
        if (!string.IsNullOrEmpty(audioId))
        {
            UISfxPlayer.Play(audioId, this);
        }
    }

    private void RefreshTurnDisplay()
    {
        if (turnImage == null || turnManager == null)
        {
            return;
        }

        int remainingTurns = turnManager.RemainingTurns;
        if (remainingTurns <= 0)
        {
            turnImage.enabled = false;
            return;
        }

        int spriteIndex = remainingTurns - 1;
        if (turnSpritesByValue == null ||
            spriteIndex < 0 ||
            spriteIndex >= turnSpritesByValue.Length)
        {
            turnImage.enabled = false;
            Debug.LogWarning(
                $"HudScreen on '{name}' has no turn sprite configured for value " +
                $"{remainingTurns}.");
            return;
        }

        Sprite turnSprite = turnSpritesByValue[spriteIndex];
        turnImage.sprite = turnSprite;
        turnImage.enabled = turnSprite != null;

        if (turnSprite == null)
        {
            Debug.LogWarning(
                $"HudScreen on '{name}' is missing the turn sprite at index {spriteIndex} " +
                $"for value {remainingTurns}.");
        }
    }
}

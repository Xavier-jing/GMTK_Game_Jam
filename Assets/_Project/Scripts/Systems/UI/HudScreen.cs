using TMPro;
using UnityEngine;

public sealed class HudScreen : ScreenBase
{

    [SerializeField]
    private TMP_Text turnText;

    [SerializeField]
    private string turnDisplayFormat = "00";

    private TurnManager turnManager;

    protected override void OnShow()
    {
        UnsubscribeTurnManager();
        turnManager = AppContext.Instance.TurnManager;
        if (turnManager != null)
        {
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
    }

    private void HandleTurnsChanged(int remaining, int max)
    {
        RefreshTurnDisplay();
    }

    private void RefreshTurnDisplay()
    {
        if (turnText == null || turnManager == null)
        {
            return;
        }

        turnText.text = string.Format(turnDisplayFormat, turnManager.RemainingTurns);
    }
}

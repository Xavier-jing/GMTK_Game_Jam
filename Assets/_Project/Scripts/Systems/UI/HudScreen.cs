using UnityEngine;
using TMPro;

public sealed class HudScreen : ScreenBase
{
    [SerializeField]
    [TextArea]
    private string defaultHint = "Press Escape to pause.";

    [SerializeField]
    private TMP_Text hintText;

    private string currentHint;

    public void SetHint(string value)
    {
        currentHint = value;
        RefreshHint();
    }

    public void ClearHint()
    {
        currentHint = null;
        RefreshHint();
    }

    protected override void OnShow()
    {
        RefreshHint();
    }

    private void RefreshHint()
    {
        if (hintText == null)
        {
            return;
        }

        hintText.text = !string.IsNullOrWhiteSpace(currentHint)
            ? currentHint
            : defaultHint;
    }
}

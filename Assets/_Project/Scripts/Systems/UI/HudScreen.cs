using UnityEngine;
using TMPro;

public sealed class HudScreen : ScreenBase
{
    [SerializeField]
    [TextArea]
    private string defaultHint = "Press Escape to pause.";

    [SerializeField]
    private TMP_Text hintText;

    public void SetHint(string value)
    {
        if (hintText != null)
        {
            hintText.text = value;
        }
    }

    protected override void OnShow()
    {
        if (!string.IsNullOrWhiteSpace(defaultHint))
        {
            SetHint(defaultHint);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsScreen : ScreenBase
{
    [SerializeField]
    private Slider bgmSlider;

    [SerializeField]
    private Slider sfxSlider;

    [SerializeField]
    private Button backButton;

    private ScreenId returnScreenId = ScreenId.MainMenu;

    private void Awake()
    {
        AutoBindMissingControls();
        BindControls();
    }

    private void OnDestroy()
    {
        UnbindControls();
    }

    protected override void OnShow()
    {
        SyncSlidersFromAudio();
    }

    public void GoBack()
    {
        if (Owner == null)
        {
            return;
        }

        if (Owner.TryGet(returnScreenId, out _))
        {
            Owner.Show(returnScreenId);
            return;
        }

        if (Owner.TryGet(ScreenId.Pause, out _))
        {
            Owner.Show(ScreenId.Pause);
            return;
        }

        if (Owner.TryGet(ScreenId.MainMenu, out _))
        {
            Owner.Show(ScreenId.MainMenu);
            return;
        }

        Debug.LogWarning($"SettingsScreen on '{name}' has no registered screen to return to.");
    }

    public void OpenFromMainMenu()
    {
        OpenFrom(ScreenId.MainMenu);
    }

    public void OpenFromPause()
    {
        OpenFrom(ScreenId.Pause);
    }

    public void OpenFrom(ScreenId sourceScreenId)
    {
        returnScreenId = sourceScreenId;

        if (Owner == null)
        {
            Debug.LogWarning($"SettingsScreen on '{name}' cannot open because it is not registered.");
            return;
        }

        if (Owner.TryGet(ScreenId.Settings, out _))
        {
            Owner.Show(ScreenId.Settings);
            return;
        }

        Debug.LogWarning($"SettingsScreen on '{name}' could not find '{ScreenId.Settings}' screen.");
    }

    public void OnBgmVolumeChanged(float value)
    {
        if (!TryGetAudio(out AudioService audio))
        {
            return;
        }

        audio.BgmVolume = value;
        SaveAudioSettings(audio);
    }

    public void OnSfxVolumeChanged(float value)
    {
        if (!TryGetAudio(out AudioService audio))
        {
            return;
        }

        audio.SfxVolume = value;
        SaveAudioSettings(audio);
    }

    private void SyncSlidersFromAudio()
    {
        if (!TryGetAudio(out AudioService audio))
        {
            return;
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(audio.BgmVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(audio.SfxVolume);
        }
    }

    private static bool TryGetAudio(out AudioService audio)
    {
        audio = null;
        if (!AppContext.HasInstance)
        {
            return false;
        }

        audio = AppContext.Instance.Audio;
        return audio != null;
    }

    private static void SaveAudioSettings(AudioService audio)
    {
        if (audio == null || !AppContext.HasInstance)
        {
            return;
        }

        if (!AppContext.Instance.Services.TryGet(out SettingsService settings))
        {
            return;
        }

        settings.BgmVolume = audio.BgmVolume;
        settings.SfxVolume = audio.SfxVolume;
    }

    private void AutoBindMissingControls()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        if (bgmSlider == null && sliders.Length > 0)
        {
            bgmSlider = sliders[0];
        }

        if (sfxSlider == null && sliders.Length > 1)
        {
            sfxSlider = sliders[1];
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && backButton == null && IsBackButtonName(button.name))
            {
                backButton = button;
            }
        }
    }

    private static bool IsBackButtonName(string buttonName)
    {
        return buttonName == "Back"
            || buttonName == "BackButton"
            || buttonName == "Return"
            || buttonName == "ReturnButton"
            || buttonName == "CloseButton";
    }

    private void BindControls()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBack);
            backButton.onClick.AddListener(GoBack);
        }
    }

    private void UnbindControls()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBack);
        }
    }
}

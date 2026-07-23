using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsScreen : ScreenBase
{
    [SerializeField]
    private Slider masterSlider;

    [SerializeField]
    private Slider bgmSlider;

    [SerializeField]
    private Slider sfxSlider;

    [SerializeField]
    private Button backButton;

    private ScreenId previousScreenId;

    protected override void OnShow()
    {
        AudioService audio = AppContext.Instance.Audio;
        if (audio == null)
        {
            return;
        }

        masterSlider.SetValueWithoutNotify(audio.MasterVolume);
        bgmSlider.SetValueWithoutNotify(audio.BgmVolume);
        sfxSlider.SetValueWithoutNotify(audio.SfxVolume);
    }

    public void OpenFrom(ScreenId fromScreen)
    {
        previousScreenId = fromScreen;
        Owner.Show(ScreenId.Settings);
    }

    public void OnMasterVolumeChanged(float value)
    {
        AppContext.Instance.Audio.MasterVolume = value;
        SyncToSettings();
    }

    public void OnBgmVolumeChanged(float value)
    {
        AppContext.Instance.Audio.BgmVolume = value;
        SyncToSettings();
    }

    public void OnSfxVolumeChanged(float value)
    {
        AppContext.Instance.Audio.SfxVolume = value;
        SyncToSettings();
    }

    private void SyncToSettings()
    {
        if (!AppContext.HasInstance)
        {
            return;
        }

        if (!AppContext.Instance.Services.TryGet(out SettingsService settings))
        {
            return;
        }

        AudioService audio = AppContext.Instance.Audio;
        settings.MasterVolume = audio.MasterVolume;
        settings.BgmVolume = audio.BgmVolume;
        settings.SfxVolume = audio.SfxVolume;
    }

    private void HandleBackClicked()
    {
        Owner.Show(previousScreenId);
    }

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBackClicked);
        }
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
        }
    }
}

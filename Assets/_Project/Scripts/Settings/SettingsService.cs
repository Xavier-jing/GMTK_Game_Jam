using UnityEngine;

public sealed class SettingsService
{
    private const string MasterVolumeKey = "Audio_MasterVolume";
    private const string BgmVolumeKey = "Audio_BgmVolume";
    private const string SfxVolumeKey = "Audio_SfxVolume";

    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            Save();
        }
    }

    public float BgmVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            Save();
        }
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            Save();
        }
    }

    public void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }
}

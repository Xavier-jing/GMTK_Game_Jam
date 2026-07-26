using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public sealed class AudioService
{
    public const string SfxResourceFolder = "Audio/SFX";
    public const string BgmResourceFolder = "Audio/BGM";

    private const int SfxPoolSize = 8;
    private const string MixerResourcePath = "Audio/MasterMixer";
    private const string BgmGroupName = "BGM";
    private const string SfxGroupName = "SFX";
    private const string MasterVolumeParam = "MasterVolume";
    private const string BgmVolumeParam = "BgmVolume";
    private const string SfxVolumeParam = "SfxVolume";
    private const float MinDb = -80f;
    private const float MaxDb = 0f;

    private AudioMixer mixer;
    private GameObject root;
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private Tween bgmTweenA;
    private Tween bgmTweenB;
    private AudioSource[] sfxPool;
    private int sfxNextIndex;
    private AudioClip currentBgmClip;
    private readonly Dictionary<string, AudioClip> resourceClipCache =
        new Dictionary<string, AudioClip>(StringComparer.Ordinal);

    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            SetMixerVolume(MasterVolumeParam, masterVolume);
        }
    }

    public float BgmVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            SetMixerVolume(BgmVolumeParam, bgmVolume);
        }
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            SetMixerVolume(SfxVolumeParam, sfxVolume);
        }
    }

    public void Initialize()
    {
        mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (mixer == null)
        {
            Debug.LogError(
                $"[AudioService] Failed to load AudioMixer at 'Resources/{MixerResourcePath}'. " +
                "Create an AudioMixer at Assets/_Project/Resources/Audio/MasterMixer.mixer");
            return;
        }

        AudioMixerGroup[] bgmGroups = mixer.FindMatchingGroups(BgmGroupName);
        AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups(SfxGroupName);

        if (bgmGroups.Length == 0)
        {
            Debug.LogError(
                $"[AudioService] No mixer group named '{BgmGroupName}' found. " +
                "Add a child group named 'BGM' under the Master group in the AudioMixer.");
            return;
        }

        if (sfxGroups.Length == 0)
        {
            Debug.LogError(
                $"[AudioService] No mixer group named '{SfxGroupName}' found. " +
                "Add a child group named 'SFX' under the Master group in the AudioMixer.");
            return;
        }

        root = new GameObject("[AudioService]");
        Object.DontDestroyOnLoad(root);

        AudioMixerGroup bgmGroup = bgmGroups[0];
        AudioMixerGroup sfxGroup = sfxGroups[0];

        bgmSourceA = CreateAudioSource("BGM-A", bgmGroup, loop: true);
        bgmSourceB = CreateAudioSource("BGM-B", bgmGroup, loop: true);

        sfxPool = new AudioSource[SfxPoolSize];
        for (int i = 0; i < SfxPoolSize; i++)
        {
            sfxPool[i] = CreateAudioSource($"SFX-{i}", sfxGroup, loop: false);
        }
    }

    public void Dispose()
    {
        bgmTweenA?.Kill();
        bgmTweenB?.Kill();
        resourceClipCache.Clear();

        if (root != null)
        {
            Object.Destroy(root);
        }
    }

    public bool TryPlaySfxById(
        string audioId,
        float volume,
        out string error)
    {
        if (!TryValidateAudioId(audioId, out error))
        {
            return false;
        }

        if (sfxPool == null)
        {
            error = "The SFX audio sources are not initialized.";
            return false;
        }

        string resourcePath = GetSfxResourcePath(audioId);
        if (!TryLoadResourceClip(resourcePath, out AudioClip clip, out error))
        {
            return false;
        }

        PlaySfx(clip, volume);
        error = string.Empty;
        return true;
    }

    public bool TrySwitchBgmById(
        string audioId,
        float fadeDuration,
        out string error)
    {
        if (!TryValidateAudioId(audioId, out error))
        {
            return false;
        }

        if (bgmSourceA == null || bgmSourceB == null)
        {
            error = "The BGM audio sources are not initialized.";
            return false;
        }

        string resourcePath = GetBgmResourcePath(audioId);
        if (!TryLoadResourceClip(resourcePath, out AudioClip clip, out error))
        {
            return false;
        }

        PlayBgm(clip, fadeDuration);
        error = string.Empty;
        return true;
    }

    public static bool IsValidAudioId(string audioId)
    {
        if (string.IsNullOrEmpty(audioId))
        {
            return false;
        }

        for (int index = 0; index < audioId.Length; index++)
        {
            char character = audioId[index];
            bool valid =
                character >= 'a' && character <= 'z' ||
                character >= 'A' && character <= 'Z' ||
                character >= '0' && character <= '9' ||
                character == '_' ||
                character == '-';

            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    public static string GetSfxResourcePath(string audioId)
    {
        return $"{SfxResourceFolder}/{audioId}";
    }

    public static string GetBgmResourcePath(string audioId)
    {
        return $"{BgmResourceFolder}/{audioId}";
    }

    public void PlayBgm(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioService] PlayBgm called with null clip.");
            return;
        }

        if (clip == currentBgmClip)
        {
            return;
        }

        if (bgmSourceA == null || bgmSourceB == null)
        {
            Debug.LogWarning("[AudioService] PlayBgm called but AudioService is not initialized.");
            return;
        }

        currentBgmClip = clip;

        AudioSource activeSource = bgmSourceA.isPlaying ? bgmSourceA : bgmSourceB;
        AudioSource inactiveSource = activeSource == bgmSourceA ? bgmSourceB : bgmSourceA;

        KillBgmTweens();

        inactiveSource.clip = clip;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float effectiveDuration = Mathf.Max(0.01f, fadeDuration);
        bgmTweenA = activeSource.DOFade(0f, effectiveDuration).OnComplete(() => activeSource.Stop());
        bgmTweenB = inactiveSource.DOFade(1f, effectiveDuration);
    }

    public void StopBgm(float fadeDuration = 1f)
    {
        currentBgmClip = null;

        if (bgmSourceA == null || bgmSourceB == null)
        {
            return;
        }

        KillBgmTweens();

        float effectiveDuration = Mathf.Max(0.01f, fadeDuration);
        if (bgmSourceA.isPlaying)
        {
            bgmTweenA = bgmSourceA.DOFade(0f, effectiveDuration).OnComplete(() => bgmSourceA.Stop());
        }

        if (bgmSourceB.isPlaying)
        {
            bgmTweenB = bgmSourceB.DOFade(0f, effectiveDuration).OnComplete(() => bgmSourceB.Stop());
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        PlaySfx(clip, 1f, 1f, 1f);
    }

    public void PlaySfx(AudioClip clip, float volume)
    {
        PlaySfx(clip, volume, 1f, 1f);
    }

    public void PlaySfx(AudioClip clip, float volume, float pitchMin, float pitchMax)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioService] PlaySfx called with null clip.");
            return;
        }

        if (sfxPool == null)
        {
            Debug.LogWarning("[AudioService] PlaySfx called but AudioService is not initialized.");
            return;
        }

        AudioSource source = sfxPool[sfxNextIndex];
        sfxNextIndex = (sfxNextIndex + 1) % SfxPoolSize;

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.Play();
    }

    private AudioSource CreateAudioSource(string name, AudioMixerGroup group, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root.transform);

        AudioSource source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = group;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private bool TryLoadResourceClip(
        string resourcePath,
        out AudioClip clip,
        out string error)
    {
        if (resourceClipCache.TryGetValue(resourcePath, out clip) && clip != null)
        {
            error = string.Empty;
            return true;
        }

        clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            error =
                $"AudioClip resource 'Resources/{resourcePath}' was not found.";
            return false;
        }

        resourceClipCache[resourcePath] = clip;
        error = string.Empty;
        return true;
    }

    private static bool TryValidateAudioId(string audioId, out string error)
    {
        if (IsValidAudioId(audioId))
        {
            error = string.Empty;
            return true;
        }

        error =
            $"Audio id '{audioId}' must contain only letters, numbers, underscores, or hyphens.";
        return false;
    }

    private void KillBgmTweens()
    {
        bgmTweenA?.Kill();
        bgmTweenB?.Kill();
        bgmTweenA = null;
        bgmTweenB = null;
    }

    private void SetMixerVolume(string parameter, float linear)
    {
        if (mixer == null)
        {
            return;
        }

        float db = linear <= 0.0001f ? MinDb : Mathf.Log10(linear) * 20f;
        db = Mathf.Clamp(db, MinDb, MaxDb);
        mixer.SetFloat(parameter, db);
    }
}

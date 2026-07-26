using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class EndingSequencePresenter : MonoBehaviour
{
    private enum PresentationState
    {
        Idle,
        Typing,
        WaitingForAdvance,
        PreparingVideo,
        PlayingVideo,
        Completed
    }

    [Header("Presentation")]
    [SerializeField]
    private GameObject presentationRoot;

    [SerializeField]
    private Image cgImage;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    [Min(0f)]
    private float charactersPerSecond = 40f;

    [Header("Ending CG")]
    [SerializeField]
    private Sprite endingOneCg;

    [SerializeField]
    private Sprite endingTwoCg;

    [SerializeField]
    private Sprite endingThreeCg;

    [Header("Credits Video")]
    [SerializeField]
    private RawImage creditsVideoImage;

    [SerializeField]
    private AspectRatioFitter creditsAspectRatio;

    [SerializeField]
    private VideoPlayer videoPlayer;

    [SerializeField]
    private AudioSource videoAudioSource;

    [SerializeField]
    private VideoClip creditsVideoClip;

    [SerializeField]
    [Min(0f)]
    private float bgmFadeDuration = 0.5f;

    private EndingSequenceCatalog catalog;
    private IReadOnlyList<string> activeSequenceIds;
    private EndingSequenceData activeSequence;
    private AudioService audioService;
    private Action completion;
    private PresentationState state;
    private RunEndReason activeReason;
    private int sequenceIndex;
    private int lineIndex;
    private int totalVisibleCharacters;
    private float visibleCharacterProgress;
    private bool videoEventsSubscribed;

    public bool IsConfigured =>
        presentationRoot != null &&
        cgImage != null &&
        dialogueText != null;

    public bool IsPresenting =>
        state != PresentationState.Idle &&
        state != PresentationState.Completed;

    private void Awake()
    {
        ConfigureVideoComponents();
        ResetVisuals();

        if (presentationRoot != null)
        {
            presentationRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (state == PresentationState.Typing)
        {
            UpdateTypewriter();
            return;
        }

        if (state == PresentationState.PlayingVideo &&
            creditsVideoImage != null &&
            creditsVideoImage.texture == null &&
            videoPlayer != null &&
            videoPlayer.texture != null)
        {
            creditsVideoImage.texture = videoPlayer.texture;
        }
    }

    private void OnDisable()
    {
        AbortPresentation();
    }

    private void OnDestroy()
    {
        AbortPresentation();
    }

    public bool TryPresent(
        RunEndReason reason,
        AudioService endingAudioService,
        Action onCompleted,
        out string error)
    {
        if (IsPresenting)
        {
            error =
                $"EndingSequencePresenter on '{name}' is already presenting " +
                $"'{activeReason}'.";
            return false;
        }

        if (!IsConfigured)
        {
            error =
                $"EndingSequencePresenter on '{name}' is missing its " +
                "Presentation Root, CG Image, or Dialogue Text reference.";
            return false;
        }

        if (catalog == null &&
            !EndingSequenceCatalog.TryLoad(out catalog, out error))
        {
            return false;
        }

        if (!EndingSequenceFlow.TryGetSequenceIds(
                reason,
                out activeSequenceIds))
        {
            error = $"Run end reason '{reason}' has no ending sequence flow.";
            return false;
        }

        CleanupVideo();
        ResetVisuals();

        activeReason = reason;
        audioService = endingAudioService;
        completion = onCompleted;
        sequenceIndex = 0;
        lineIndex = 0;
        state = PresentationState.WaitingForAdvance;
        presentationRoot.SetActive(true);

        if (!TryShowSequence(sequenceIndex, out error))
        {
            AbortPresentation();
            return false;
        }

        Debug.Log(
            $"[EndingSequence] Started '{reason}' with " +
            $"{activeSequenceIds.Count} sequence(s) on '{name}'.",
            this);
        error = string.Empty;
        return true;
    }

    public void Advance()
    {
        if (!IsPresenting)
        {
            return;
        }

        if (state == PresentationState.Typing)
        {
            CompleteCurrentLine();
            return;
        }

        if (state != PresentationState.WaitingForAdvance)
        {
            return;
        }

        lineIndex++;
        if (lineIndex < activeSequence.Lines.Length)
        {
            ShowLine(activeSequence.Lines[lineIndex]);
            return;
        }

        sequenceIndex++;
        if (sequenceIndex < activeSequenceIds.Count)
        {
            if (!TryShowSequence(sequenceIndex, out string error))
            {
                Debug.LogError(
                    $"[EndingSequence] Could not continue '{activeReason}' " +
                    $"on '{name}': {error}",
                    this);
                CompletePresentation();
            }

            return;
        }

        if (EndingSequenceFlow.RequiresCredits(activeReason))
        {
            BeginCreditsVideo();
            return;
        }

        CompletePresentation();
    }

    private bool TryShowSequence(int targetIndex, out string error)
    {
        string sequenceId = activeSequenceIds[targetIndex];
        if (!catalog.TryGet(sequenceId, out activeSequence) ||
            activeSequence == null)
        {
            error =
                $"Ending sequence '{sequenceId}' was not found in the " +
                "validated catalog.";
            return false;
        }

        ApplyCg(sequenceId);
        lineIndex = 0;
        ShowLine(activeSequence.Lines[lineIndex]);
        error = string.Empty;
        return true;
    }

    private void ApplyCg(string sequenceId)
    {
        Sprite sprite;
        switch (sequenceId)
        {
            case EndingSequenceId.Truth:
                sprite = null;
                break;

            case EndingSequenceId.EndingOne:
                sprite = endingOneCg;
                break;

            case EndingSequenceId.EndingTwo:
                sprite = endingTwoCg;
                break;

            case EndingSequenceId.EndingThree:
                sprite = endingThreeCg;
                break;

            default:
                sprite = null;
                break;
        }

        cgImage.sprite = sprite;
        cgImage.enabled = sprite != null;
        cgImage.gameObject.SetActive(sprite != null);

        if (sequenceId != EndingSequenceId.Truth && sprite == null)
        {
            Debug.LogError(
                $"[EndingSequence] Sequence '{sequenceId}' has no CG Sprite " +
                $"assigned on '{name}'. The text will continue on black.",
                this);
        }
    }

    private void ShowLine(string line)
    {
        dialogueText.gameObject.SetActive(true);
        dialogueText.text = line;
        dialogueText.ForceMeshUpdate();

        totalVisibleCharacters = dialogueText.textInfo.characterCount;
        visibleCharacterProgress = 0f;

        if (charactersPerSecond <= 0f || totalVisibleCharacters == 0)
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
            state = PresentationState.WaitingForAdvance;
            return;
        }

        dialogueText.maxVisibleCharacters = 0;
        state = PresentationState.Typing;
    }

    private void UpdateTypewriter()
    {
        visibleCharacterProgress +=
            charactersPerSecond * Time.unscaledDeltaTime;
        int visibleCharacters = Mathf.Min(
            totalVisibleCharacters,
            Mathf.FloorToInt(visibleCharacterProgress));
        dialogueText.maxVisibleCharacters = visibleCharacters;

        if (visibleCharacters >= totalVisibleCharacters)
        {
            state = PresentationState.WaitingForAdvance;
        }
    }

    private void CompleteCurrentLine()
    {
        visibleCharacterProgress = totalVisibleCharacters;
        dialogueText.maxVisibleCharacters = int.MaxValue;
        state = PresentationState.WaitingForAdvance;
    }

    private void BeginCreditsVideo()
    {
        dialogueText.gameObject.SetActive(false);
        cgImage.gameObject.SetActive(false);

        if (videoPlayer == null ||
            videoAudioSource == null ||
            creditsVideoImage == null ||
            creditsVideoClip == null)
        {
            Debug.LogError(
                $"[EndingSequence] Credits video setup is incomplete on " +
                $"'{name}' for '{activeReason}'. Returning to the main menu " +
                "without playing credits.",
                this);
            CompletePresentation();
            return;
        }

        if (audioService == null)
        {
            Debug.LogError(
                $"[EndingSequence] AudioService is unavailable on '{name}'. " +
                "The credits video will use its AudioSource without the game mixer.",
                this);
        }
        else
        {
            audioService.StopBgm(bgmFadeDuration);
            if (!audioService.TryRouteToBgmMixer(
                    videoAudioSource,
                    out string routeError))
            {
                Debug.LogError(
                    $"[EndingSequence] Could not route credits audio through " +
                    $"the BGM mixer on '{name}': {routeError}",
                    this);
            }
        }

        try
        {
            ConfigureVideoComponents();
            videoPlayer.Stop();
            videoPlayer.clip = creditsVideoClip;
            SubscribeVideoEvents();
            state = PresentationState.PreparingVideo;
            videoPlayer.Prepare();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[EndingSequence] Failed to prepare credits video " +
                $"'{creditsVideoClip.name}' on '{name}': {exception.Message}",
                this);
            CompletePresentation();
        }
    }

    private void ConfigureVideoComponents()
    {
        if (videoAudioSource != null)
        {
            videoAudioSource.playOnAwake = false;
            videoAudioSource.loop = false;
            videoAudioSource.spatialBlend = 0f;
        }

        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;

        if (videoAudioSource != null)
        {
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        }
    }

    private void HandleVideoPrepared(VideoPlayer source)
    {
        if (state != PresentationState.PreparingVideo ||
            source != videoPlayer)
        {
            return;
        }

        if (source.width > 0 && source.height > 0 &&
            creditsAspectRatio != null)
        {
            creditsAspectRatio.aspectRatio =
                (float)source.width / source.height;
        }

        if (source.audioTrackCount == 0)
        {
            Debug.LogError(
                $"[EndingSequence] Credits video '{creditsVideoClip.name}' " +
                "contains no audio track.",
                this);
        }

        creditsVideoImage.texture = source.texture;
        creditsVideoImage.gameObject.SetActive(true);
        state = PresentationState.PlayingVideo;
        source.Play();

        Debug.Log(
            $"[EndingSequence] Credits video '{creditsVideoClip.name}' " +
            $"started for '{activeReason}' on '{name}'.",
            this);
    }

    private void HandleVideoCompleted(VideoPlayer source)
    {
        if (state != PresentationState.PlayingVideo ||
            source != videoPlayer)
        {
            return;
        }

        Debug.Log(
            $"[EndingSequence] Credits video '{creditsVideoClip.name}' " +
            $"completed for '{activeReason}' on '{name}'.",
            this);
        CompletePresentation();
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        if (source != videoPlayer ||
            (state != PresentationState.PreparingVideo &&
             state != PresentationState.PlayingVideo))
        {
            return;
        }

        Debug.LogError(
            $"[EndingSequence] Credits video failed for '{activeReason}' " +
            $"on '{name}': {message}",
            this);
        CompletePresentation();
    }

    private void CompletePresentation()
    {
        if (!IsPresenting)
        {
            return;
        }

        RunEndReason completedReason = activeReason;
        Action completed = completion;
        state = PresentationState.Completed;
        completion = null;
        activeSequenceIds = null;
        activeSequence = null;
        audioService = null;

        CleanupVideo();
        ResetVisuals();
        presentationRoot.SetActive(false);

        Debug.Log(
            $"[EndingSequence] Completed '{completedReason}' on '{name}'.",
            this);
        completed?.Invoke();
    }

    private void AbortPresentation()
    {
        completion = null;
        activeSequenceIds = null;
        activeSequence = null;
        audioService = null;
        state = PresentationState.Idle;
        CleanupVideo();
        ResetVisuals();

        if (presentationRoot != null)
        {
            presentationRoot.SetActive(false);
        }
    }

    private void ResetVisuals()
    {
        if (cgImage != null)
        {
            cgImage.sprite = null;
            cgImage.enabled = false;
            cgImage.gameObject.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
            dialogueText.gameObject.SetActive(false);
        }

        if (creditsVideoImage != null)
        {
            creditsVideoImage.texture = null;
            creditsVideoImage.gameObject.SetActive(false);
        }
    }

    private void SubscribeVideoEvents()
    {
        if (videoPlayer == null || videoEventsSubscribed)
        {
            return;
        }

        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.loopPointReached += HandleVideoCompleted;
        videoPlayer.errorReceived += HandleVideoError;
        videoEventsSubscribed = true;
    }

    private void CleanupVideo()
    {
        if (videoPlayer != null)
        {
            if (videoEventsSubscribed)
            {
                videoPlayer.prepareCompleted -= HandleVideoPrepared;
                videoPlayer.loopPointReached -= HandleVideoCompleted;
                videoPlayer.errorReceived -= HandleVideoError;
            }

            videoPlayer.Stop();
        }

        videoEventsSubscribed = false;

        if (creditsVideoImage != null)
        {
            creditsVideoImage.texture = null;
            creditsVideoImage.gameObject.SetActive(false);
        }
    }
}

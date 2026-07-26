using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public sealed class BootVideoLoading : MonoBehaviour
{
    [SerializeField]
    private VideoPlayer videoPlayer;

    private TaskCompletionSource<bool> playbackCompletion;

    public bool CanPlay =>
        videoPlayer != null &&
        (videoPlayer.clip != null || !string.IsNullOrWhiteSpace(videoPlayer.url));

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        playbackCompletion?.TrySetResult(false);
        playbackCompletion = null;
    }

    public Task PlayAsync()
    {
        if (!CanPlay)
        {
            Debug.LogWarning(
                $"BootVideoLoading on '{name}' has no VideoClip or URL. Boot will use the normal loading screen.");
            return Task.CompletedTask;
        }

        if (playbackCompletion != null)
        {
            return playbackCompletion.Task;
        }

        playbackCompletion = new TaskCompletionSource<bool>();
        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.loopPointReached += HandlePlaybackCompleted;
        videoPlayer.errorReceived += HandlePlaybackError;
        videoPlayer.Stop();
        videoPlayer.Prepare();
        return playbackCompletion.Task;
    }

    private void HandlePrepared(VideoPlayer source)
    {
        Debug.Log(
            $"Boot video started on '{name}'. Clip length: {source.length:0.00}s.");
        source.Play();
    }

    private void HandlePlaybackCompleted(VideoPlayer source)
    {
        Debug.Log(
            $"Boot video completed on '{name}' at {source.time:0.00}s / {source.length:0.00}s.");
        CompletePlayback(true);
    }

    private void HandlePlaybackError(VideoPlayer source, string message)
    {
        Debug.LogError($"Boot video failed on '{name}': {message}");
        CompletePlayback(false);
    }

    private void CompletePlayback(bool playedSuccessfully)
    {
        Unsubscribe();
        playbackCompletion?.TrySetResult(playedSuccessfully);
    }

    private void Unsubscribe()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= HandlePrepared;
        videoPlayer.loopPointReached -= HandlePlaybackCompleted;
        videoPlayer.errorReceived -= HandlePlaybackError;
    }
}

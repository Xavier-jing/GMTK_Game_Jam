using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class LoadingScreen : MonoBehaviour, ISceneTransition
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private float fadeInDuration = 0.2f;

    [SerializeField]
    private float fadeOutDuration = 0.25f;

    [SerializeField]
    [Min(0f)]
    private float minimumVisibleDuration = 0.8f;

    [SerializeField]
    private Ease fadeEase = Ease.OutQuad;

    [SerializeField]
    private Slider progressSlider;

    [SerializeField]
    private TMP_Text progressText;

    [SerializeField]
    [Min(0.1f)]
    private float progressUnitsPerSecond = 2.5f;

    [SerializeField]
    private bool startOpaque = true;

    private float displayedProgress;
    private TaskCompletionSource<bool> fadeCompletion;
    private Tween fadeTween;
    private float targetProgress;
    private int transitionVersion;
    private float visibleSinceRealtime = float.NegativeInfinity;

    public static LoadingScreen Current { get; private set; }

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Destroy(gameObject);
            return;
        }

        Current = this;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (transform.parent != null)
        {
            Debug.LogWarning(
                $"LoadingScreen on '{name}' is not a root GameObject. Un-parenting to ensure DontDestroyOnLoad works.");
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        canvasGroup.alpha = startOpaque ? 1f : 0f;
        SetInputBlocked(startOpaque);
        ResetProgress();

        if (startOpaque)
        {
            visibleSinceRealtime = Time.realtimeSinceStartup;
        }

        AppContext.EnsureExists().SceneLoader.RegisterTransition(this);
    }

    private void Update()
    {
        if (Mathf.Approximately(displayedProgress, targetProgress))
        {
            return;
        }

        displayedProgress = Mathf.MoveTowards(
            displayedProgress,
            targetProgress,
            progressUnitsPerSecond * Time.unscaledDeltaTime);

        ApplyProgress(displayedProgress);
    }

    // Start() 中不自动隐藏 LoadingScreen。
    // 自动隐藏逻辑已移至 Bootstrap.HandleInitialSceneLoaded：
    // - Boot 场景：Bootstrap 直接 LoadSceneAsync，加载完后 HideAsync() 淡出
    // - 非 Boot 场景（直接运行调试）：Bootstrap 调用 HideAsync() 淡出黑屏

    private void OnDestroy()
    {
        transitionVersion++;
        CompleteAndKillFade();

        if (Current != this)
        {
            return;
        }

        if (AppContext.HasInstance)
        {
            AppContext.Instance.SceneLoader.UnregisterTransition(this);
        }

        Current = null;
    }

    public async Task ShowAsync()
    {
        int version = ++transitionVersion;
        ResetProgress();
        SetInputBlocked(true);
        await FadeToAsync(1f, fadeInDuration, keepInputBlocked: true);

        if (version != transitionVersion)
        {
            return;
        }

        visibleSinceRealtime = Time.realtimeSinceStartup;
    }

    public async Task HideAsync()
    {
        int version = ++transitionVersion;
        SetProgress(1f);
        await WaitForMinimumVisibleDurationAsync();

        if (version != transitionVersion)
        {
            return;
        }

        targetProgress = 1f;
        ApplyProgress(1f);
        await FadeToAsync(0f, fadeOutDuration, keepInputBlocked: false);
    }

    public void HideImmediately()
    {
        transitionVersion++;
        CompleteAndKillFade();
        visibleSinceRealtime = float.NegativeInfinity;

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        SetInputBlocked(false);
    }

    public void SetProgress(float normalizedProgress)
    {
        targetProgress = Mathf.Clamp01(normalizedProgress);

        if (targetProgress < displayedProgress)
        {
            displayedProgress = targetProgress;
            ApplyProgress(displayedProgress);
        }
    }

    private Task FadeToAsync(float targetAlpha, float duration, bool keepInputBlocked)
    {
        CompleteAndKillFade();

        if (canvasGroup == null)
        {
            return Task.CompletedTask;
        }

        if (duration <= 0f || Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            canvasGroup.alpha = targetAlpha;
            SetInputBlocked(keepInputBlocked);
            return Task.CompletedTask;
        }

        TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        fadeCompletion = completion;

        fadeTween = canvasGroup
            .DOFade(targetAlpha, duration)
            .SetEase(fadeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                canvasGroup.alpha = targetAlpha;
                SetInputBlocked(keepInputBlocked);
                completion.TrySetResult(true);
            })
            .OnKill(() => completion.TrySetResult(true));

        return completion.Task;
    }

    private void CompleteAndKillFade()
    {
        fadeCompletion?.TrySetResult(true);
        fadeCompletion = null;

        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }

        fadeTween = null;
    }

    private async Task WaitForMinimumVisibleDurationAsync()
    {
        if (minimumVisibleDuration <= 0f || float.IsNegativeInfinity(visibleSinceRealtime))
        {
            return;
        }

        float elapsed = Time.realtimeSinceStartup - visibleSinceRealtime;
        float remaining = minimumVisibleDuration - elapsed;
        if (remaining <= 0f)
        {
            return;
        }

        await Task.Delay(Mathf.CeilToInt(remaining * 1000f));
    }

    private void ResetProgress()
    {
        targetProgress = 0f;
        displayedProgress = 0f;
        ApplyProgress(0f);
    }

    private void ApplyProgress(float normalizedProgress)
    {
        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(normalizedProgress);
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(normalizedProgress * 100f)}%";
        }
    }

    private void SetInputBlocked(bool isBlocked)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.blocksRaycasts = isBlocked;
        canvasGroup.interactable = false;
    }
}

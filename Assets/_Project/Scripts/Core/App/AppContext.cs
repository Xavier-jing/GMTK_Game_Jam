using UnityEngine;

public sealed class AppContext : MonoBehaviour
{
    private const int InitialTurns = 5;
    private const string GlobalBgmAudioId = "Music";
    private const float GlobalBgmFadeDuration = 1f;

    private static AppContext instance;
    private bool hasLoggedGlobalBgmError;

    public static AppContext Instance => instance != null ? instance : EnsureExists();

    public static bool HasInstance => instance != null;

    public ServiceRegistry Services { get; private set; }

    public SceneLoader SceneLoader { get; private set; }

    public GamePause GamePause { get; private set; }

    public AudioService Audio { get; private set; }

    public TurnManager TurnManager { get; private set; }

    public ActionResolver ActionResolver { get; private set; }

    public LoopManager LoopManager { get; private set; }

    public LoopProgress LoopProgress { get; private set; }

    public RunState RunState { get; private set; }

    public Inventory Inventory { get; private set; }

    public StoryProgress StoryProgress { get; private set; }

    public bool IsInitialized { get; private set; }

    public static AppContext EnsureExists()
    {
        if (instance != null)
        {
            return instance;
        }

        AppContext existingContext = FindObjectOfType<AppContext>();
        if (existingContext != null)
        {
            existingContext.Initialize();
            return existingContext;
        }

        GameObject contextRoot = new GameObject("[AppContext]");
        instance = contextRoot.AddComponent<AppContext>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameObject.name = "[AppContext]";
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        Services = new ServiceRegistry();
        SceneLoader = new SceneLoader();
        GamePause = new GamePause();

        Services.Register(Services);
        Services.Register(SceneLoader);
        Services.Register(GamePause);

        SettingsService settings = new SettingsService();
        settings.Load();
        Services.Register(settings);

        TurnManager = new TurnManager(initialTurns: InitialTurns);
        Services.Register(TurnManager);

        Inventory = new Inventory();
        Services.Register(Inventory);

        LoopProgress = new LoopProgress();
        Services.Register(LoopProgress);

        RunState = new RunState();
        Services.Register(RunState);

        LoopManager = new LoopManager(
            TurnManager,
            Inventory,
            SceneLoader,
            LoopProgress,
            RunState);
        Services.Register(LoopManager);

        ActionResolver = new ActionResolver(TurnManager, LoopManager);
        Services.Register(ActionResolver);

        StoryProgress = new StoryProgress();
        Services.Register(StoryProgress);

        Audio = new AudioService();
        Audio.Initialize();
        Services.Register(Audio);

        Audio.MasterVolume = settings.MasterVolume;
        Audio.BgmVolume = settings.BgmVolume;
        Audio.SfxVolume = settings.SfxVolume;

        SceneLoader.SceneLoaded += HandleSceneLoaded;
        IsInitialized = true;
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        if (SceneLoader != null)
        {
            SceneLoader.SceneLoaded -= HandleSceneLoaded;
        }

        SceneLoader?.Dispose();
        GamePause?.Resume();
        LoopManager?.Dispose();
        Audio?.Dispose();
        IsInitialized = false;
        instance = null;
    }

    private void HandleSceneLoaded(SceneId sceneId)
    {
        if (sceneId == SceneId.Boot)
        {
            return;
        }

        if (Audio.TrySwitchBgmById(
                GlobalBgmAudioId,
                GlobalBgmFadeDuration,
                out string error))
        {
            hasLoggedGlobalBgmError = false;
            return;
        }

        if (hasLoggedGlobalBgmError)
        {
            return;
        }

        hasLoggedGlobalBgmError = true;
        Debug.LogError(
            $"[AppContext] Could not start global BGM " +
            $"'{GlobalBgmAudioId}': {error}",
            this);
    }
}

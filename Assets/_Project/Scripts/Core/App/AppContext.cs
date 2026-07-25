using UnityEngine;

public sealed class AppContext : MonoBehaviour
{
    private static AppContext instance;

    public static AppContext Instance => instance != null ? instance : EnsureExists();

    public static bool HasInstance => instance != null;

    public ServiceRegistry Services { get; private set; }

    public SceneLoader SceneLoader { get; private set; }

    public GamePause GamePause { get; private set; }

    public AudioService Audio { get; private set; }

    public TurnManager TurnManager { get; private set; }

    public Inventory Inventory { get; private set; }

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

        TurnManager = new TurnManager(maxTurns: 30);
        Services.Register(TurnManager);

        Inventory = new Inventory();
        Services.Register(Inventory);

        Audio = new AudioService();
        Audio.Initialize();
        Services.Register(Audio);

        Audio.MasterVolume = settings.MasterVolume;
        Audio.BgmVolume = settings.BgmVolume;
        Audio.SfxVolume = settings.SfxVolume;

        IsInitialized = true;
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneLoader?.Dispose();
        GamePause?.Resume();
        Audio?.Dispose();
        IsInitialized = false;
        instance = null;
    }
}

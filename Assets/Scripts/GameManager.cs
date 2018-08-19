using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum UIOverlayState
{
    Record,
    Rewind,
    Play,
    Stop
}

public enum LevelEndState
{
    FailFuel,
    FailTimeline,
    Success
}

public class GameManager : MonoBehaviour
{
    //Singleton
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            return _instance;
        }
    }

    // Globals
    [HideInInspector]
    public TimeManager TimeManager;

    [HideInInspector]
    public CarController ActiveCar;

    public bool RecordSnapshots;
    public bool Rewinding;

    public bool AllowTimeRewind;
    public float SpawnEnergy;
    public float AccelerationEnergyConsumption;
    public float IdleEnergyConsumption;
    public float EnergyPerCollectible;

    public int RewindCount;

    public bool LevelEnded;
    private float _levelEndTimer;
    [HideInInspector]
    public LevelEndState LevelEndState;

    // Self references
    public Camera Camera;
    public CarPool CarPool;
    public ParticleSystemPool ExplosionPSPool;

    // Other
    public GameObject GameUI;
    public GameObject FadeUI;

    public bool DrawGameUI;

    public GameObject CarSpawn;

    // History of cars in order
    private List<CarController> _carHistory;

    [HideInInspector]
    public Vector3 SceneCameraOffset;

    [HideInInspector]
    public GameUIController GameUIController;

    private FadeUIController _fadeUIController;
    private float _fadeAmount;
    private float _fadeTarget;
    private float _fadeTimer;
    private float _fadeTime;
    private float _fadeStart;

    public bool LoadingScene;
    public bool ExitingLevel;
    public int CollectiblesLeft;
    public int LevelEndInput;

    void Awake()
    {
        var currentInstance = _instance != null ? _instance : this;

        var inMenu = SceneManager.GetActiveScene().name == "menu";

        if (currentInstance != this)
        {
            GameManager.Instance.Camera = Camera;
            Camera.transform.parent = GameManager.Instance.transform;
        }

        currentInstance.AllowTimeRewind = AllowTimeRewind;
        currentInstance.RewindCount = RewindCount;

        currentInstance.CollectiblesLeft = 0;

        // Create new UI
        if (!inMenu && DrawGameUI)
        {
            var ui = Instantiate(GameUI);
            currentInstance.GameUIController = ui.GetComponent<GameUIController>();
        }

        // Setup singleton
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        _instance = this;

        // Self references
        TimeManager = GetComponent<TimeManager>();

        var fadeUI = Instantiate(FadeUI);
        fadeUI.transform.parent = transform;
        _fadeUIController = fadeUI.GetComponent<FadeUIController>();
        _fadeUIController.SetFade(0f);

        _carHistory = new List<CarController>();

        // Start the level

        ResetState();
    }

    void Update()
    {
        var carExists = ActiveCar != null;
        // Rewinding input
        if (Input.GetKeyDown(KeyCode.Space) && carExists && AllowTimeRewind && !Rewinding && RewindCount > 0 && !LevelEnded)
        {
            GameUIController.SetUIState(UIOverlayState.Rewind);
            Rewinding = true;
            ActiveCar.PlayerControlled = false;
        }

        if (Input.GetKeyUp(KeyCode.Space) && carExists && AllowTimeRewind && Rewinding && !LevelEnded)
        {
            GameUIController.SetUIState(UIOverlayState.Record);
            Rewinding = false;
            RewindCount -= 1;
            TimeManager.CutTimeline(ActiveCar);

            var oldCar = ActiveCar;

            var newCar = CarPool.GetPooledObject();
            newCar.gameObject.SetActive(true);
            SetNewCar(newCar, ActiveCar.transform);
            newCar.component.Energy = ActiveCar.Energy;

            newCar.component.ApplySnapshot(oldCar.GetTimeSnapshot());
        }

        // Update time states - record only if car exists and not rewinding time
        RecordSnapshots = carExists && !Rewinding && !LevelEnded;


        // Update level end state
        if (ActiveCar == null && !LevelEnded)
        {
            _levelEndTimer += Time.deltaTime;
            if (_levelEndTimer > 2f)
            {
                LevelEndState = LevelEndState.FailTimeline;
                EndLevel(SceneManager.GetActiveScene().name);
            }
        }

        // Check for out of fuel
        if (ActiveCar != null && RewindCount == 0 && !LevelEnded && ActiveCar.Velocity.magnitude < 0.1f && ActiveCar.Energy <= 0f)
        {
            LevelEndState = LevelEndState.FailFuel;
            EndLevel(SceneManager.GetActiveScene().name);
        }

        // Fading
        if (_fadeTimer > 0f)
        {
            _fadeTimer -= Time.deltaTime;

            var fadeT = 1f - _fadeTimer / _fadeTime;
            _fadeAmount = Mathf.Lerp(_fadeStart, _fadeTarget, fadeT);

            _fadeUIController.SetFade(_fadeAmount);

            if (_fadeTimer <= 0f)
            {
                _fadeTimer = 0f;
                _fadeAmount = _fadeTarget;
            }
        }
    }

    private void ResetState()
    {
        LevelEndInput = 0;
        LevelEnded = false;
        ActiveCar = null;
        ExitingLevel = false;
        _levelEndTimer = 0f;
        _carHistory.Clear();
        CarPool.Clear();
    }

    public void SpawnCar(GameObject spawn)
    {
        // Spawn a new car at car spawn
        SceneCameraOffset = spawn.transform.position - Camera.transform.position;

        var newCar = CarPool.GetPooledObject();
        newCar.gameObject.SetActive(true);

        SetNewCar(newCar, spawn.transform);
        newCar.component.Energy = SpawnEnergy;
    }

    public void SetNewCar(Component<CarController> newCar, Transform spawnTransform)
    {
        newCar.gameObject.transform.position = spawnTransform.position;
        newCar.gameObject.transform.rotation = spawnTransform.rotation;

        newCar.component.Reset();

        newCar.component.PlayerControlled = true;
        ActiveCar = newCar.component;

        // Set camera to follow the car
        Camera.transform.parent = newCar.gameObject.transform;

        _carHistory.Add(newCar.component);
    }

    public void ActiveCarDestroyed()
    {
        Camera.transform.parent = transform;
        // Find last active car, control that

        ActiveCar = null;

        for (var i = _carHistory.Count - 1; i >= 0; i--)
        {
            if (_carHistory[i] != null && !_carHistory[i].Destroyed && _carHistory[i].gameObject.activeInHierarchy)
            {
                ActiveCar = _carHistory[i];
                ActiveCar.PlayerControlled = true;
                Camera.transform.parent = ActiveCar.transform;

                break;
            }
        }
    }

    public void EndLevel(string targetScene = "")
    {
        if (ExitingLevel) return;

        // Show UI
        LevelEnded = true;

        StartCoroutine(StartLevelEndFade(targetScene));
    }

    public IEnumerator StartLevelEndFade(string nextScene)
    {
        while (LevelEndInput == 0) yield return new WaitForEndOfFrame();

        if (LevelEndInput == 2) nextScene = "menu";

        ExitingLevel = true;
        Fade(1.25f, 1f);
        yield return new WaitForSeconds(1.25f);
        ChangeLevel(nextScene);
    }

    public void ChangeLevel(string sceneName)
    {
        if (ActiveCar)
        {
            ActiveCar.gameObject.SetActive(false);
        }

        Destroy(Camera);

        LoadingScene = true;
        SceneManager.LoadScene(sceneName);
        LoadingScene = false;

        Fade(1.25f, 0f);

        ExitingLevel = false;
        ResetState();
    }

    public void Fade(float time, float target)
    {
        _fadeTime = time;
        _fadeTimer = time;
        _fadeTarget = target;
        _fadeStart = _fadeAmount;
    }
}